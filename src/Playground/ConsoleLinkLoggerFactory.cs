#region Usings

using RabbitLink.Logging;

#endregion

namespace Playground
{
    internal class ConsoleLinkLoggerFactory : ILinkLoggerFactory
    {
        /// <inheritdoc />
        public ILinkLogger CreateLogger(string name, string identifier)
        {
            return new ConsoleLinkLogger($"{name}({identifier})");
        }
    }
}
