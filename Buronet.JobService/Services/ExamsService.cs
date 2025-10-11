using Buronet.JobService.Models;
using Buronet.JobService.Services.Interfaces;
using Buronet.JobService.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Buronet.JobService.Services
{
    public class ExamsService : IExamsService
    {
        private readonly IMongoCollection<Exam> _examsCollection;

        public ExamsService(IOptions<JobDBSettings> settings)
        {
            var mongoClient = new MongoClient(settings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _examsCollection = mongoDatabase.GetCollection<Exam>(settings.Value.ExamsCollectionName);
        }

        public async Task<List<Exam>> GetAsync() =>
            await _examsCollection.Find(_ => true).ToListAsync();

        public async Task<Exam?> GetAsync(string id) =>
            await _examsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Exam newExam) =>
            await _examsCollection.InsertOneAsync(newExam);

        public async Task<bool> UpdateAsync(string id, Exam updatedExam)
        {
            updatedExam.Id = id;
            var result = await _examsCollection.ReplaceOneAsync(exam => exam.Id == id, updatedExam);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
    }
}
