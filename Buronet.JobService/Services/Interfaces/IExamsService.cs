using Buronet.JobService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Buronet.JobService.Services.Interfaces
{
    public interface IExamsService
    {
        Task<List<Exam>> GetAsync();
        Task<Exam?> GetAsync(string id);
        Task CreateAsync(Exam newExam);
        Task<bool> UpdateAsync(string id, Exam updatedExam);
        Task<List<Exam>> SearchAsync(string keyword);
    }
}
