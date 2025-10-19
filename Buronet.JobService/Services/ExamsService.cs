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
            var indexKeysDefinition = Builders<Exam>.IndexKeys
            .Text(j => j.ExamTitle)
            .Text(j => j.ConductingBody)
            .Text(j => j.ExamSummary)
            .Text(j => j.EligibilityCriteria.EducationalQualification)
            .Text(j => j.ExamPattern.Preliminary.Summary)
            .Text(j => j.ExamPattern.Main.Summary);            

            var indexModel = new CreateIndexModel<Exam>(indexKeysDefinition);
            // The driver will check if the index exists and only create it if it doesn't.
            _examsCollection.Indexes.CreateOne(indexModel);
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

        public async Task<List<Exam>> SearchAsync(string keyword)
        {
            // Use the $text filter to perform a case-insensitive text search on the indexed fields.
            var filter = Builders<Exam>.Filter.Text(keyword, new TextSearchOptions { CaseSensitive = false });
            var jobs = await _examsCollection.Find(filter).ToListAsync();
            return jobs;
        }
    }
}
