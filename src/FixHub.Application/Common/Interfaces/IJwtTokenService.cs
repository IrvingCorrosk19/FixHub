using FixHub.Domain.Entities;

namespace FixHub.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
