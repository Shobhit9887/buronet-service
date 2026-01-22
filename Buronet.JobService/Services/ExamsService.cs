using Buronet.JobService.Models;
using Buronet.JobService.Services.Interfaces;
using Buronet.JobService.Settings;
using JobService.Models;
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

        public async Task<(List<Exam> Exams, long TotalCount)> GetPaginatedAsync(int page, int pageSize)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            if (pageSize > 100)
            {
                pageSize = 100;
            }

            var filter = Builders<Exam>.Filter.Empty;
            var skip = (page - 1) * pageSize;

            var totalCountTask = _examsCollection.CountDocumentsAsync(filter);
            var examsTask = _examsCollection
                .Find(filter)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            await Task.WhenAll(totalCountTask, examsTask);

            return (await examsTask, await totalCountTask);
        }

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
