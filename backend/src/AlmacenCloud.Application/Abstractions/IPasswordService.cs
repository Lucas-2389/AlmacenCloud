using AlmacenCloud.Domain.Entities;

namespace AlmacenCloud.Application.Abstractions;

public interface IPasswordService
{
    string Hash(Usuario usuario, string password);
    bool Verify(Usuario usuario, string password);
}
