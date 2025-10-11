// File: JobService/Controllers/JobsController.cs
using Buronet.JobService.Models;

namespace JobService.Controllers
{
    internal class ApiResponse<T>
    {
        public bool Success { get; set; }
        public List<DepartmentStatsDto> Data { get; set; }
    }
}