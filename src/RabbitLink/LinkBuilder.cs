using RabbitLink.Builders;

namespace RabbitLink
{
    /// <summary>
    /// Builder for <see cref="ILink"/>
    /// </summary>
    public static class LinkBuilder
    {
        /// <summary>
        /// Gets new <see cref="ILinkBuilder"/>
        /// </summary>
        public static ILinkBuilder Configure
            => new Builders.LinkBuilder();
    }
}
