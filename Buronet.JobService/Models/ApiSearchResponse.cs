using Buronet.JobService.Models;

namespace Buronet.JobService.Models;

    public class ApiSearchResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
