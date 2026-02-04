namespace buronet_service.Models.DTOs.User
{
    public class LoginResultDto
    {
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
    }
}