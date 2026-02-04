namespace buronet_service.Models.DTOs
{
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // If true, issue refresh token for long-lived sessions
        public bool RememberMe { get; set; }
    }
}
