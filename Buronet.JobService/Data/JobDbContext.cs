using Microsoft.EntityFrameworkCore;
using Buronet.JobService.Models;

namespace Buronet.JobService.Data;

/// <summary>
/// The EF Core DbContext for the JobService.
/// This class manages the connection to the MySQL database for bookmark data.
/// </summary>
public class JobDbContext : DbContext
{
    public JobDbContext(DbContextOptions<JobDbContext> options) : base(options)
    {
    }

    public DbSet<UserJobBookmark> UserJobBookmarks { get; set; }
}
