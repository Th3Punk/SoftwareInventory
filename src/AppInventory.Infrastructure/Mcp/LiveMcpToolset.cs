using AppInventory.Core.Interfaces;

namespace AppInventory.Infrastructure.Mcp;

internal sealed class LiveMcpToolset : IMcpToolset
{
    public bool IsAvailable => true;
}
