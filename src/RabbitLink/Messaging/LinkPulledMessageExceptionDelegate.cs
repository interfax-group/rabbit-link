#region Usings

using System;

#endregion

namespace RabbitLink.Messaging
{
    /// <summary>
    ///     Delegate for <see cref="ILinkPulledMessage{TBody}.Exception" />
    /// </summary>
    public delegate void LinkPulledMessageExceptionDelegate(Exception ex);
}
