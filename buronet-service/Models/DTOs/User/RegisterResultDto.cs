namespace buronet_service.Models.DTOs.User
{
    public class RegisterResultDto
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
    }
}