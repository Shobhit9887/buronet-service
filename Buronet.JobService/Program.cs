// File: JobService/Program.cs
using Buronet.JobService.Data;
using Buronet.JobService.Services;
using Buronet.JobService.Services.Interfaces;
using Buronet.JobService.Settings;
using JobService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        // TODO: Replace with your frontend's actual URL in production
        policy.WithOrigins("http://ec2-13-48-45-225.eu-north-1.compute.amazonaws.com")
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

builder.Services.AddAuthentication(options =>
{
    // 🚨 THIS IS THE CRITICAL LINE 🚨
    // Tells the framework to use the JWT Bearer handler for retrieving tokens 
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddHttpClient("NotificationService", client =>
{
    // Ensure you have the ServiceUrls:NotificationService in your appsettings.json
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:NotificationService"]!);
});

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