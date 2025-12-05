using Warranty_System.Models;

namespace Warranty_System.Auth
{
    public class AuthResponse(Users user, string token)
    {
        public int UserId { get; set; } = user.Id;
        public string Username { get; set; } = user.Username!;
        public string Fullname { get; set; } = user.FullName!;
        public string AccessToken { get; set; } = token;
        public string Redirect { get; set; } = user.RoleId == 0 ? "/managers" : "/users";
        //public string? RefreshToken { get; set; } = refreshToken;
    }
}