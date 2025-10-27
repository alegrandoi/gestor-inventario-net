using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestorInventario.Application.Authentication.Models;
using GestorInventario.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Identity;

public interface IJwtTokenGenerator
{
    Task<AuthResponseDto> CreateTokenAsync(User user, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        this.options = options.Value;
    }

    public Task<AuthResponseDto> CreateTokenAsync(User user, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(options.Key);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(options.ExpiresInMinutes);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        var tokenValue = handler.WriteToken(token);

        var response = new AuthResponseDto(
            tokenValue,
            expires,
            new UserSummaryDto(user.Id, user.Username, user.Email, roles.FirstOrDefault() ?? string.Empty, user.IsActive),
            false,
            null,
            null);

        return Task.FromResult(response);
    }
}
