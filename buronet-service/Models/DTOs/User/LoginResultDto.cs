namespace buronet_service.Models.DTOs.User
{
    public class LoginResultDto
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}