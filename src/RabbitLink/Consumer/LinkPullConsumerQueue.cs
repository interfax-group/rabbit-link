#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Async;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkPullConsumerQueue : IDisposable
    {
        #region Fields

        private readonly CancellationToken _disposedCancellation;

        private readonly CancellationTokenSource _disposedCancellationSource = new CancellationTokenSource();
        private readonly LinkedList<QueueItem> _queue = new LinkedList<QueueItem>();
        private readonly SemaphoreSlim _readSem = new SemaphoreSlim(0);
        private readonly AsyncLock _sync = new AsyncLock();

        #endregion

        #region Ctor

        public LinkPullConsumerQueue()
        {
            _disposedCancellation = _disposedCancellationSource.Token;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
            => Dispose(true);

        #endregion


        public Task<LinkConsumerAckStrategy> PutAsync(ILinkConsumedMessage<byte[]> message)
        {
            var completion = new TaskCompletionSource<LinkConsumerAckStrategy>();
            var msg = new LinkPulledMessage<byte[]>(message, completion);

            if (msg.Cancellation.IsCancellationRequested || _disposedCancellation.IsCancellationRequested)
                return Task.FromCanceled<LinkConsumerAckStrategy>(msg.Cancellation);

            try
            {
                using (var compositeCancellation = CancellationTokenSource
                    .CreateLinkedTokenSource(_disposedCancellation, msg.Cancellation))
                {
                    LinkedListNode<QueueItem> node;
                    var qItem = new QueueItem(msg, completion);

                    using (_sync.Lock(compositeCancellation.Token))
                    {
                        node = _queue.AddLast(qItem);
                    }

                    qItem.EnableCancellation(() =>
                    {
                        using (_sync.Lock(CancellationToken.None))
                        {
                            node.List?.Remove(node);
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                return Task.FromCanceled<LinkConsumerAckStrategy>(msg.Cancellation);
            }

            _readSem.Release();
            return completion.Task;
        }

        public async Task<LinkPulledMessage<byte[]>> TakeAsync(CancellationToken cancellation)
        {
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();

                if (_disposedCancellation.IsCancellationRequested)
                    throw new ObjectDisposedException(GetType().Name);

                try
                {
                    using (var compositeCancellation = CancellationTokenSource
                        .CreateLinkedTokenSource(_disposedCancellation, cancellation))
                    {
                        await _readSem.WaitAsync(compositeCancellation.Token)
                            .ConfigureAwait(false);

                        QueueItem item;
                        try
                        {
                            using (await _sync.LockAsync(compositeCancellation.Token).ConfigureAwait(false))
                            {
                                var node = _queue.First;
                                if (node == null)
                                {
                                    continue;
                                }

                                node.List.Remove(node);
                                item = node.Value;
                            }
                        }
                        catch
                        {
                            _readSem.Release();
                            throw;
                        }

                        item.DisableCancellation();
                        return item.Message;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (_disposedCancellation.IsCancellationRequested)
                        throw new ObjectDisposedException(GetType().Name);

                    throw;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            lock (_disposedCancellationSource)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _disposedCancellationSource.Cancel();
                _disposedCancellationSource.Dispose();

                QueueItem[] items;

                using (_sync.Lock(CancellationToken.None))
                {
                    items = _queue.ToArray();
                    _queue.Clear();
                }

                foreach (var item in items)
                {
                    item.Cancel();
                }
            }

            if (disposing)
                GC.SuppressFinalize(this);
        }


        ~LinkPullConsumerQueue()
            => Dispose(false);

        #region Nested types

        #region QueueItem

        /// <summary>
        ///     Class to store queue item
        /// </summary>
        private class QueueItem
        {
            #region Fields

            private readonly object _cancellationSync = new object();
            private CancellationTokenRegistration? _cancellationRegistration;
            private readonly TaskCompletionSource<LinkConsumerAckStrategy> _completion;

            #endregion

            #region Ctor

            public QueueItem(LinkPulledMessage<byte[]> message,
                TaskCompletionSource<LinkConsumerAckStrategy> completion)
            {
                _completion = completion;
                Message = message;
            }

            #endregion

            #region Properties

            public LinkPulledMessage<byte[]> Message { get; }

            #endregion

            public void Cancel()
            {
                DisableCancellation();
                _completion.TrySetCanceled();
            }

            public void EnableCancellation(Action cancelAction)
            {
                if (_cancellationRegistration != null)
                    throw new InvalidOperationException("Cancellation already enabled");

                lock (_cancellationSync)
                {
                    if (_cancellationRegistration != null)
                        throw new InvalidOperationException("Cancellation already enabled");

                    _cancellationRegistration = Message
                        .Cancellation
                        .Register(() =>
                        {
                            _completion.TrySetCanceled();
                            cancelAction?.Invoke();
                        });
                }
            }

            public void DisableCancellation()
            {
                if (_cancellationRegistration == null)
                    return;

                lock (_cancellationSync)
                {
                    if (_cancellationRegistration == null)
                        return;

                    _cancellationRegistration?.Dispose();
                    _cancellationRegistration = null;
                }
            }
        }

        #endregion

        #endregion
    }
}
