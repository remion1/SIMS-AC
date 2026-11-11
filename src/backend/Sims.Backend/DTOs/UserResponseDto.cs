using Sims.Backend.Models;

namespace Sims.Backend.DTOs;

public sealed class UserResponseDto
{
    public int      Id        { get; init; }
    public string   Username  { get; init; } = string.Empty;
    public string   Email     { get; init; } = string.Empty;
    public UserRole Role      { get; init; }
    public bool     IsActive  { get; init; }
    public DateTime CreatedAt { get; init; }

    public static UserResponseDto FromEntity(User user) => new()
    {
        Id        = user.Id,
        Username  = user.Username,
        Email     = user.Email,
        Role      = user.Role,
        IsActive  = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
