namespace AlmacenCloud.Application.Abstractions;

public interface IProductImageStorage
{
    Task<string> SaveAsync(Guid empresaId, Guid productoId, Stream content, string extension, CancellationToken ct);
    Task DeleteAsync(string? relativePath, CancellationToken ct);
}
