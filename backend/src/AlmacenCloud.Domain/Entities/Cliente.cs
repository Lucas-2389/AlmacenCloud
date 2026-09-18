using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Domain.Entities;

public sealed class Cliente
{
    private Cliente() { }
    private Cliente(Guid empresaId, TipoDocumento tipoDocumento, string numeroDocumento, string nombreRazonSocial,
        string? direccion, string? telefono, string? email)
    {
        Id = Guid.NewGuid(); EmpresaId = empresaId; CreadoEn = DateTime.UtcNow; ActualizadoEn = CreadoEn; Activo = true;
        Apply(tipoDocumento, numeroDocumento, nombreRazonSocial, direccion, telefono, email);
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public TipoDocumento TipoDocumento { get; private set; }
    public string NumeroDocumento { get; private set; } = null!;
    public string NombreRazonSocial { get; private set; } = null!;
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public bool Activo { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }

    public static Cliente Create(Guid empresaId, TipoDocumento tipoDocumento, string numeroDocumento, string nombreRazonSocial,
        string? direccion, string? telefono, string? email) => new(empresaId, tipoDocumento, numeroDocumento, nombreRazonSocial, direccion, telefono, email);
    public void Update(TipoDocumento tipoDocumento, string numeroDocumento, string nombreRazonSocial, string? direccion, string? telefono, string? email)
    { Apply(tipoDocumento, numeroDocumento, nombreRazonSocial, direccion, telefono, email); ActualizadoEn = DateTime.UtcNow; }
    public void Deactivate() { Activo = false; ActualizadoEn = DateTime.UtcNow; }

    private void Apply(TipoDocumento tipo, string document, string name, string? address, string? phone, string? email)
    {
        document = document?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(document)) throw new ArgumentException("El documento es obligatorio.", nameof(document));
        if (tipo == TipoDocumento.Dni && (document.Length != 8 || !document.All(char.IsDigit))) throw new ArgumentException("El DNI debe tener 8 dígitos.");
        if (tipo == TipoDocumento.Ruc && (document.Length != 11 || !document.All(char.IsDigit))) throw new ArgumentException("El RUC debe tener 11 dígitos.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre o razón social es obligatorio.", nameof(name));
        TipoDocumento = tipo; NumeroDocumento = document.ToUpperInvariant(); NombreRazonSocial = name.Trim();
        Direccion = Clean(address); Telefono = Clean(phone); Email = Clean(email)?.ToLowerInvariant();
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
