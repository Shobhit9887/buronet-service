using Buronet.JobService.Models;
using System.Threading.Tasks;

namespace Buronet.JobService.Services.Interfaces
{
    public interface IUpdateFetcherService
    {
        Task FetchAndStoreUpdatesAsync(string query);
        Task<List<ExternalUpdate>> GetLatestUpdatesAsync(UpdateCategory? category, int limit);
    }
}