namespace AlmacenCloud.Domain.Entities;

public sealed class PasswordResetToken
{
    private PasswordResetToken() { }

    private PasswordResetToken(Guid usuarioId, Guid empresaId, string tokenHash, DateTime expiraEn)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        TokenHash = tokenHash;
        ExpiraEn = expiraEn;
        CreadoEn = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid EmpresaId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiraEn { get; private set; }
    public DateTime? UsadoEn { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public Usuario Usuario { get; private set; } = null!;

    public bool IsValid(DateTime now) => UsadoEn is null && ExpiraEn > now;

    public void MarkUsed(DateTime now)
    {
        if (UsadoEn is null) UsadoEn = now;
    }

    public static PasswordResetToken Create(Guid usuarioId, Guid empresaId, string tokenHash, DateTime expiraEn) =>
        new(usuarioId, empresaId, tokenHash, expiraEn);
}
