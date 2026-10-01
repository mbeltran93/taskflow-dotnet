using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
