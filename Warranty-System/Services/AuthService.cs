using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Warranty_System.Auth;
using Warranty_System.Models;
using Warranty_System.Repositories;

namespace Warranty_System.Services;

public interface IAuthService
{
    Task<(AuthResponse, string)> Authenticate(AuthRequest model);

    /// <summary>
    /// Generates a signed JWT access token containing the user ID.
    /// </summary>
    string GenerateAccessToken(Users user);

    /// <summary>
    /// Generates a cryptographically secure refresh token (base64-encoded).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT and extracts the user Id (sub claim).
    /// Returns null if invalid.
    /// </summary>
    int? GetUserIdFromToken(string token);
}

public class AuthService : IAuthService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IGenericRepo _genericRepo;

    public AuthService(IOptions<JwtSettings> JwtSettings, IGenericRepo genericRepo)
    {
        _jwtSettings = JwtSettings.Value;
        _genericRepo = genericRepo;
    }

    public async Task<(AuthResponse, string)> Authenticate(AuthRequest model)
    {
        var user = await _genericRepo.FindModel<Users>
            (u => u.Username == model.Username && u.Password == model.Password)
            ?? throw new NullReferenceException("User không tồn tại");
        string token = GenerateAccessToken(user);
        string refreshToken = GenerateRefreshToken();
        AuthResponse res = new(user, token);
        return (res, refreshToken);
    }

    public string GenerateAccessToken(Users user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.FullName!),
            new Claim(ClaimTypes.Role, user.RoleId == 0 ? "Managers" : "Users"),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public int? GetUserIdFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                throw new Exception("id đâu??");
            return Convert.ToInt32(userId);
        }
        catch
        {
            return null; // Invalid token
        }
    }
}