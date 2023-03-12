using Microsoft.Extensions.Logging;

namespace RabbitLink.Logging
{
    internal class LoggerFactory : ILinkLoggerFactory
    {
        private readonly ILoggerFactory _factory;
        private readonly string _categoryPrefix;

        public LoggerFactory(ILoggerFactory factory, string categoryPrefix)
        {
            _factory = factory;
            _categoryPrefix = categoryPrefix;
        }

        public ILinkLogger CreateLogger(string name, string identifier)
            => new Logger(_factory.CreateLogger($"{_categoryPrefix}{name}{identifier}"));
    }

    internal class IdentityIgnoringLoggerFactory : ILinkLoggerFactory
    {
        private readonly ILoggerFactory _factory;
        private readonly string _categoryPrefix;

        public IdentityIgnoringLoggerFactory(ILoggerFactory factory, string categoryPrefix)
        {
            _factory = factory;
            _categoryPrefix = categoryPrefix;
        }

        public ILinkLogger CreateLogger(string name, string _)
            => new Logger(_factory.CreateLogger($"{_categoryPrefix}{name}"));
    }
}
