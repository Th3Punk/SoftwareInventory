using AppInventory.Core.Interfaces;

namespace AppInventory.Infrastructure.Docs;

internal sealed class NullDocumentStore : IDocumentStore
{
    public bool IsAvailable => false;
}
