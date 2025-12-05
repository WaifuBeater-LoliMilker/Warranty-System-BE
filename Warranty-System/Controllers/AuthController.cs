using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warranty_System.Auth;
using Warranty_System.Models;
using Warranty_System.Repositories;
using Warranty_System.Services;

namespace Warranty_System.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IGenericRepo _repo;

        public AuthController(IAuthService authService, IGenericRepo genericRepo)
        {
            _authService = authService;
            _repo = genericRepo;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] AuthRequest req)
        {
            try
            {
                if (HttpContext.Items["User"] != null) return Accepted();
                var (res, refreshToken) = await _authService.Authenticate(req);
                var existedToken = await _repo.FindModel<RefreshTokens>(t => t.UserId == res.UserId);
                var newRefreshToken = new RefreshTokens()
                {
                    UserId = res.UserId,
                    Token = refreshToken,
                    CreatedDate = DateTime.UtcNow,
                    ExpireDate = DateTime.UtcNow.AddDays(15),
                    IsRevoked = false
                };
                if (existedToken != null)
                {
                    newRefreshToken.Id = existedToken.Id;
                    await _repo.Update<RefreshTokens>(newRefreshToken);
                }
                else await _repo.Insert<RefreshTokens>(newRefreshToken);
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(15)
                };
                Response.Cookies.Append("access_token", res.AccessToken, cookieOptions);
                Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);

                return Ok(res);
            }
            catch (NullReferenceException)
            {
                return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refresh_token"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var tokenEntity = await _repo.FindModel<RefreshTokens>(x => x.Token == refreshToken);
                if (tokenEntity != null)
                {
                    await _repo.Delete<RefreshTokens>(tokenEntity);
                }
            }
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(-1)
            };
            Response.Cookies.Append("access_token", "", cookieOptions);
            Response.Cookies.Append("refresh_token", "", cookieOptions);

            return Ok("See ya later, aligator!");
        }

        [HttpPost("role")]
        public IActionResult Role()
        {
            if (HttpContext.Items["User"] is not Users user) return BadRequest();
            return Ok(new { role = user!.RoleId == 0 ? "managers" : "users" });
        }
    }
}