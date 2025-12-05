using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Warranty_System.Auth;
using Warranty_System.Models;
using Warranty_System.Repositories;
using Warranty_System.Services;

namespace Warranty_System.Middlewares;
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtSettings _jwtSettings;

    public JwtMiddleware(RequestDelegate next, IOptions<JwtSettings> JwtSettings)
    {
        _next = next;
        _jwtSettings = JwtSettings.Value;
    }

    public async Task Invoke(HttpContext context, IAuthService userService, IGenericRepo repo)
    {
        var skip = context.GetEndpoint()?.Metadata.GetMetadata<SkipJWTMiddlewareAttribute>() != null;
        if (skip)
        {
            await _next(context);
            return;
        }
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
        if (token != null)
            await AttachUserToContext(context, userService, token, repo);

        await _next(context);
    }

    private async Task AttachUserToContext(HttpContext context, IAuthService userService,
        string token, IGenericRepo repo)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = int.Parse(jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
            //var fullName = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;

            // attach user to context on successful jwt validation
            var user = await repo.GetById<Users>(userId) ?? throw new NullReferenceException();
            context.Items["User"] = user;
        }
        catch (SecurityTokenExpiredException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Access token expired");
            return;
        }
        catch (NullReferenceException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("User not found");
            return;
        }
        catch
        {
            // Do nothing if JWT validation fails
            // User is not attached to context, so request won't have access to secure routes
        }
    }
}