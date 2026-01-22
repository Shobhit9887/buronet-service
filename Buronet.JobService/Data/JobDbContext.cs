using Microsoft.EntityFrameworkCore;
using Buronet.JobService.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

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
    public DbSet<UserExamBookmark> UserExamBookmarks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyUtcDateTimeConverters(modelBuilder);
    }

    private static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
            toDb => DateTime.SpecifyKind(
                (toDb.Kind == DateTimeKind.Utc) ? toDb : toDb.ToUniversalTime(),
                DateTimeKind.Unspecified),
            fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));

        var utcNullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            toDb => toDb.HasValue
                ? DateTime.SpecifyKind(
                    (toDb.Value.Kind == DateTimeKind.Utc) ? toDb.Value : toDb.Value.ToUniversalTime(),
                    DateTimeKind.Unspecified)
                : null,
            fromDb => fromDb.HasValue
                ? DateTime.SpecifyKind(fromDb.Value, DateTimeKind.Utc)
                : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(utcNullableDateTimeConverter);
                }
            }
        }
    }
}
