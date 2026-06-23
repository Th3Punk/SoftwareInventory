using AppInventory.Core.Interfaces;

namespace AppInventory.Infrastructure.Mcp;

internal sealed class NullMcpToolset : IMcpToolset
{
    public bool IsAvailable => false;
}
