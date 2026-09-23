namespace AlmacenCloud.Domain.Entities;

public sealed class SecuenciaCompra
{
    private SecuenciaCompra() { }
    private SecuenciaCompra(Guid empresaId) { EmpresaId = empresaId; UltimoNumero = 1; Version = 1; }
    public Guid EmpresaId { get; private set; }
    public long UltimoNumero { get; private set; }
    public long Version { get; private set; }
    public static SecuenciaCompra Create(Guid empresaId) => new(empresaId);
    public void Advance(long expectedVersion) { UltimoNumero++; Version = expectedVersion + 1; }
}
