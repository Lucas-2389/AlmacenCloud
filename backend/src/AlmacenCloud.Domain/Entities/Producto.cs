using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Domain.Entities;

public sealed class Producto
{
    private Producto() { }

    private Producto(Guid empresaId, Guid categoriaId, string codigo, string nombre, string? descripcion,
        UnidadMedida unidadMedida, decimal precioCompra, decimal precioVenta, decimal stockMinimo, bool afectoIgv)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        CreadoEn = DateTime.UtcNow;
        ActualizadoEn = CreadoEn;
        Activo = true;
        Apply(categoriaId, codigo, nombre, descripcion, unidadMedida, precioCompra, precioVenta, stockMinimo, afectoIgv);
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid CategoriaId { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public UnidadMedida UnidadMedida { get; private set; }
    public decimal PrecioCompra { get; private set; }
    public decimal PrecioVenta { get; private set; }
    public decimal StockMinimo { get; private set; }
    public bool AfectoIgv { get; private set; }
    public string? ImagenUrl { get; private set; }
    public bool Activo { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }
    public Categoria Categoria { get; private set; } = null!;

    public static Producto Create(Guid empresaId, Guid categoriaId, string codigo, string nombre, string? descripcion,
        UnidadMedida unidadMedida, decimal precioCompra, decimal precioVenta, decimal stockMinimo, bool afectoIgv) =>
        new(empresaId, categoriaId, codigo, nombre, descripcion, unidadMedida, precioCompra, precioVenta, stockMinimo, afectoIgv);

    public void Update(Guid categoriaId, string codigo, string nombre, string? descripcion, UnidadMedida unidadMedida,
        decimal precioCompra, decimal precioVenta, decimal stockMinimo, bool afectoIgv)
    {
        Apply(categoriaId, codigo, nombre, descripcion, unidadMedida, precioCompra, precioVenta, stockMinimo, afectoIgv);
        ActualizadoEn = DateTime.UtcNow;
    }

    public void Deactivate() { Activo = false; ActualizadoEn = DateTime.UtcNow; }

    public void SetImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) throw new ArgumentException("La URL de imagen es obligatoria.", nameof(imageUrl));
        ImagenUrl = imageUrl.Trim();
        ActualizadoEn = DateTime.UtcNow;
    }

    public void RemoveImage() { ImagenUrl = null; ActualizadoEn = DateTime.UtcNow; }

    private void Apply(Guid categoriaId, string codigo, string nombre, string? descripcion, UnidadMedida unidadMedida,
        decimal precioCompra, decimal precioVenta, decimal stockMinimo, bool afectoIgv)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("El código es obligatorio.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (precioCompra < 0 || precioVenta < 0 || stockMinimo < 0) throw new ArgumentOutOfRangeException(nameof(precioCompra), "Los importes y el stock mínimo no pueden ser negativos.");
        CategoriaId = categoriaId;
        Codigo = codigo.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        UnidadMedida = unidadMedida;
        PrecioCompra = precioCompra;
        PrecioVenta = precioVenta;
        StockMinimo = stockMinimo;
        AfectoIgv = afectoIgv;
    }
}
