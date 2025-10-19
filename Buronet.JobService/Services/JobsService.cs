using Buronet.JobService.Data;
using Buronet.JobService.Models;
using Buronet.JobService.Services.Interfaces;
using Buronet.JobService.Settings;
using JobService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace JobService.Services;

public class JobsService : IJobsService
{
    private readonly IMongoCollection<Job> _jobsCollection;
    private readonly IMongoCollection<Exam> _examsCollection;
    private readonly JobDbContext _dbContext;

    public JobsService(IOptions<JobDBSettings> mongoDbSettings, JobDbContext dbContext)
    {
        var mongoClient = new MongoClient(mongoDbSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
        _jobsCollection = mongoDatabase.GetCollection<Job>(mongoDbSettings.Value.JobsCollectionName);
        _examsCollection = mongoDatabase.GetCollection<Exam>(mongoDbSettings.Value.ExamsCollectionName);
        _dbContext = dbContext;
        // --- THIS IS THE FIX ---
        // Create a text index on multiple fields to enable text search.
        // This is an idempotent operation, so it's safe to run on every startup.
        var indexKeysDefinition = Builders<Job>.IndexKeys
            .Text(j => j.JobTitle)
            .Text(j => j.JobDescription)
            .Text(j => j.CompanyName)
            .Text(j => j.OrganizationName)
            .Text(j => j.Sector)
            .Text(j => j.Location);

        var indexModel = new CreateIndexModel<Job>(indexKeysDefinition);
        // The driver will check if the index exists and only create it if it doesn't.
        _jobsCollection.Indexes.CreateOne(indexModel);
    }

    public async Task<List<Job>> GetAsync() =>
        await _jobsCollection.Find(_ => true).ToListAsync();

    public async Task<List<Job>> GetJobsForJobHomeAsync() =>
    await _jobsCollection.Find(job => job.Status == "Active")
                         .SortByDescending(job => job.CreatedDate)
                         .Limit(6)
                         .ToListAsync();

    public async Task<Job?> GetAsync(string id) =>
        await _jobsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Job newJob) =>
        await _jobsCollection.InsertOneAsync(newJob);

    public async Task<JobDashboardStatsDto> GetJobDashboardStatsAsync(string userId)
    {
        var today = DateTime.UtcNow.Date.ToString();

        // Using FilterDefinitionBuilder for more complex queries
        var activeJobsFilter = Builders<Job>.Filter.Eq(j => j.Status, "active");
        var newJobsTodayFilter = Builders<Job>.Filter.Gte(j => j.CreatedDate, today);

        // Run counts in parallel for efficiency
        var totalActiveJobsTask = _jobsCollection.CountDocumentsAsync(activeJobsFilter);
        var newJobsTodayTask = _jobsCollection.CountDocumentsAsync(newJobsTodayFilter);

        // We also need the bookmark count, which requires the DbContext for MySQL
        var totalBookmarkedJobsTask = _dbContext.UserJobBookmarks.CountAsync(b => b.UserId == userId);

        await Task.WhenAll(totalActiveJobsTask, newJobsTodayTask, totalBookmarkedJobsTask);

        return new JobDashboardStatsDto
        {
            TotalActiveJobs = await totalActiveJobsTask,
            NewJobsToday = await newJobsTodayTask,
            TotalBookmarkedJobs = await totalBookmarkedJobsTask
        };
    }

    public async Task<ExamDashboardStatsDto> GetExamDashboardStatsAsync(string userId)
    {
        var today = DateTime.UtcNow.Date.ToString();

        // Using FilterDefinitionBuilder for more complex queries
        var activeExamsFilter = Builders<Exam>.Filter.Eq(j => j.Status, "active");
        var newExamsTodayFilter = Builders<Exam>.Filter.Gte(j => j.CreatedDate, today);

        // Run counts in parallel for efficiency
        var totalActiveExamsTask = _examsCollection.CountDocumentsAsync(activeExamsFilter);
        var newExamsTodayTask = _examsCollection.CountDocumentsAsync(newExamsTodayFilter);

        // We also need the bookmark count, which requires the DbContext for MySQL
        var totalBookmarkedExamsTask = _dbContext.UserExamBookmarks.CountAsync(b => b.UserId == userId);

        await Task.WhenAll(totalActiveExamsTask, newExamsTodayTask, totalBookmarkedExamsTask);

        return new ExamDashboardStatsDto
        {
            TotalActiveExams = await totalActiveExamsTask,
            NewExamsToday = await newExamsTodayTask,
            TotalBookmarkedExams = await totalBookmarkedExamsTask
        };
    }

    public async Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync()
    {
        // This aggregation pipeline efficiently groups and counts jobs directly in the database.
        var aggregation = await _jobsCollection.Aggregate()
            // 1. Find all documents where the status is "Active".
            .Match(job => job.Status == "active")
            // 2. Group the active jobs by the value in their "Sector" field.
            .Group(
                job => job.Sector, // This is the key to group by.
                group => new
                {
                    // The 'Key' from the grouping becomes our department name.
                    DepartmentName = group.Key,
                    // For each item in the group, add 1 to the sum to get the total count.
                    JobCount = group.Sum(j => 1)
                })
            // 3. Sort the results so the departments with the most jobs appear first.
            .SortByDescending(g => g.JobCount)
            // 4. Limit the result to only the top 6 departments.
            .Limit(6)
            // 5. Shape the result to match our DepartmentStatsDto.
            .Project(g => new DepartmentStatsDto
            {
                DepartmentName = g.DepartmentName,
                JobCount = g.JobCount
            })
            .ToListAsync();

        return aggregation;
    }

    public async Task<bool> UpdateAsync(string id, Job updatedJob)
    {
        // It's a good practice to ensure the ID from the route matches the ID in the body.
        updatedJob.Id = id;

        // Set the updated date to the current UTC time.
        updatedJob.UpdatedDate = DateTime.UtcNow.ToString();

        // The ReplaceOneAsync method finds a document matching the filter 
        // and replaces it with the new object.
        var result = await _jobsCollection.ReplaceOneAsync(
            job => job.Id == id,
            updatedJob);

        // The operation is considered successful if the server acknowledged the write
        // and at least one document was actually modified.
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<List<Job>> SearchAsync(string keyword)
    {
        // Use the $text filter to perform a case-insensitive text search on the indexed fields.
        var filter = Builders<Job>.Filter.Text(keyword, new TextSearchOptions { CaseSensitive = false });
        var jobs = await _jobsCollection.Find(filter).ToListAsync();
        return jobs;
    }

}