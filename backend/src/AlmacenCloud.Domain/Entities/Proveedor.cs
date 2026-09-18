namespace AlmacenCloud.Domain.Entities;

public sealed class Proveedor
{
    private Proveedor() { }
    private Proveedor(Guid empresaId, string ruc, string razonSocial, string? nombreComercial, string? direccion, string? telefono, string? email)
    {
        Id = Guid.NewGuid(); EmpresaId = empresaId; CreadoEn = DateTime.UtcNow; ActualizadoEn = CreadoEn; Activo = true;
        Apply(ruc, razonSocial, nombreComercial, direccion, telefono, email);
    }
    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public string Ruc { get; private set; } = null!;
    public string RazonSocial { get; private set; } = null!;
    public string? NombreComercial { get; private set; }
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public bool Activo { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }
    public static Proveedor Create(Guid empresaId, string ruc, string razonSocial, string? nombreComercial, string? direccion, string? telefono, string? email) => new(empresaId, ruc, razonSocial, nombreComercial, direccion, telefono, email);
    public void Update(string ruc, string razonSocial, string? nombreComercial, string? direccion, string? telefono, string? email)
    { Apply(ruc, razonSocial, nombreComercial, direccion, telefono, email); ActualizadoEn = DateTime.UtcNow; }
    public void Deactivate() { Activo = false; ActualizadoEn = DateTime.UtcNow; }
    private void Apply(string ruc, string razon, string? commercial, string? address, string? phone, string? email)
    {
        ruc = ruc?.Trim() ?? string.Empty;
        if (ruc.Length != 11 || !ruc.All(char.IsDigit)) throw new ArgumentException("El RUC debe tener 11 dígitos.");
        if (string.IsNullOrWhiteSpace(razon)) throw new ArgumentException("La razón social es obligatoria.");
        Ruc = ruc; RazonSocial = razon.Trim(); NombreComercial = Clean(commercial); Direccion = Clean(address); Telefono = Clean(phone); Email = Clean(email)?.ToLowerInvariant();
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
