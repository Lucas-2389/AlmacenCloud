namespace AlmacenCloud.Application.Abstractions;

public interface ITenantContext
{
    bool HasTenant { get; }
    Guid? EmpresaId { get; }
}
