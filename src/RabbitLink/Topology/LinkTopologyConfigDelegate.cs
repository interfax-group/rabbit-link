#region Usings

using System.Threading.Tasks;

#endregion

namespace RabbitLink.Topology
{
    /// <summary>
    ///     Config delegate for <see cref="ILinkTopology" />
    /// </summary>
    public delegate Task LinkTopologyConfigDelegate(ILinkTopologyConfig config);
}
