using FixHub.Application.Common.Interfaces;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// BCrypt con work factor 12 (balance seguridad/rendimiento para producción 2024+).
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
