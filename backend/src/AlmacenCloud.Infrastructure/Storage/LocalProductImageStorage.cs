using AlmacenCloud.Application.Abstractions;

namespace AlmacenCloud.Infrastructure.Storage;

public sealed class LocalProductImageStorage(string webRootPath) : IProductImageStorage
{
    private readonly string _root = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "productos"));

    public async Task<string> SaveAsync(Guid empresaId, Guid productoId, Stream content, string extension, CancellationToken ct)
    {
        var tenantDirectory = Path.Combine(_root, empresaId.ToString("N"));
        Directory.CreateDirectory(tenantDirectory);
        var fileName = $"{productoId:N}-{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(tenantDirectory, fileName);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(output, ct);
        return $"/uploads/productos/{empresaId:N}/{fileName}";
    }

    public Task DeleteAsync(string? relativePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;
        var normalized = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRootPath, normalized));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La ruta de imagen no pertenece al almacenamiento de productos.");
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
