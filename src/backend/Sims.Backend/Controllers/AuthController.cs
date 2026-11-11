using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sims.Backend.Data;
using Sims.Backend.DTOs;
using Sims.Backend.Models;
using Sims.Backend.Services;

namespace Sims.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController(SimsDbContext db, IRedisLogService logger) : ControllerBase
{
    // ── POST /api/auth/register ───────────────────────────────────────────────

    /// <summary>
    /// Registers a new user account. Returns 409 Conflict if the username is already taken.
    /// </summary>
    // Neuen Benutzer registrieren; 409 wenn Username bereits vergeben
    [HttpPost("register")]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserDto dto,
        CancellationToken ct = default)
    {
        var exists = await db.Users
            .AnyAsync(u => u.Username == dto.Username, ct);

        if (exists)
            return Conflict(new { Message = $"Username '{dto.Username}' is already taken." });

        var user = new User
        {
            Username     = dto.Username,
            Email        = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            Role         = dto.Role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        await logger.LogAsync("INFO",
            $"User '{user.Username}' registered.",
            $"Role: {user.Role} | Email: {user.Email}");

        var response = UserResponseDto.FromEntity(user);
        return CreatedAtAction(nameof(GetUsers), response);
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────

    /// <summary>
    /// Validates credentials and returns a safe user projection on success.
    /// </summary>
    // Login mit Credentials; 401 bei falschem Passwort oder inaktivem Account
    [HttpPost("login")]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == dto.Username, ct);

        if (user is null || !user.IsActive)
        {
            await logger.LogAsync("WARN",
                $"Failed login attempt for username '{dto.Username}'.",
                user is null ? "User not found." : "Account is deactivated.");

            return Unauthorized(new { Message = "Invalid credentials or account is inactive." });
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            await logger.LogAsync("WARN",
                $"Failed login attempt for username '{dto.Username}'.",
                "Password mismatch.");

            return Unauthorized(new { Message = "Invalid credentials or account is inactive." });
        }

        await logger.LogAsync("INFO",
            $"User '{user.Username}' logged in successfully.",
            $"Role: {user.Role}");

        return Ok(UserResponseDto.FromEntity(user));
    }

    // ── GET /api/auth/users ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a list of all registered users (admin use only).
    /// </summary>
    // Alle registrierten Benutzer auflisten (nur für Admins gedacht)
    [HttpGet("users")]
    [ProducesResponseType<IEnumerable<UserResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken ct = default)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => UserResponseDto.FromEntity(u))
            .ToListAsync(ct);

        return Ok(users);
    }

    // ── PUT /api/auth/users/{id}/status ──────────────────────────────────────

    /// <summary>
    /// Toggles a user account between active and inactive.
    /// </summary>
    // Account aktivieren oder deaktivieren
    [HttpPut("users/{id:int}/status")]
    [ProducesResponseType<UserResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(
        int id,
        [FromQuery] bool isActive,
        CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct);

        if (user is null)
            return NotFound(new { Message = $"User with id {id} was not found." });

        var previousState = user.IsActive;
        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        await logger.LogAsync("INFO",
            $"User '{user.Username}' (id={id}) status changed: {previousState} → {isActive}.");

        return Ok(UserResponseDto.FromEntity(user));
    }
}
