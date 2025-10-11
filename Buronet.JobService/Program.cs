// File: JobService/Program.cs
using Buronet.JobService.Data;
using Buronet.JobService.Services;
using Buronet.JobService.Services.Interfaces;
using Buronet.JobService.Settings;
using JobService.Services;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        // TODO: Replace with your frontend's actual URL in production
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 1. Configure settings from appsettings.json
builder.Services.Configure<JobDBSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// --- MySQL Configuration for Bookmarks ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseMySql(connectionString, Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(connectionString)));
builder.Services.AddScoped<IBookmarkService, BookmarkService>();

// 2. Add services to the container.
// This registers your JobsService for dependency injection.
builder.Services.AddScoped<IJobsService, JobsService>();
builder.Services.AddScoped<IExamsService, ExamsService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins); // Apply CORS policy
app.UseAuthorization();
app.MapControllers();
app.Run();