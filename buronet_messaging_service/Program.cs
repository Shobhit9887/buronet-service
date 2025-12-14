// buronet_messaging_service/Program.cs
using AutoMapper; // For AutoMapper
using buronet_messaging_service.Data;
using buronet_messaging_service.Hubs; // Your SignalR Hub
using buronet_messaging_service.Profiles; // Your AutoMapper Profiles
using buronet_messaging_service.Services; // Your Messaging Services
using buronet_messaging_service.Services.Interfaces; // Your Messaging Service Interfaces
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var configuration = builder.Configuration;

// --- Add Services to the container ---

// 1. Add DbContext for Messaging
builder.Services.AddDbContext<MessagingDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("MessagingConnection");
    // Use the appropriate provider for your database (e.g., UseMySql, UseSqlServer)
    // For MySQL with Pomelo:
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    // For SQL Server:
    // options.UseSqlServer(connectionString);
});

// 2. Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"], // Ensure this matches your buronet_service's JWT Issuer
            ValidAudience = configuration["Jwt:Audience"], // Ensure this matches your buronet_service's JWT Audience
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])) // Ensure this matches your buronet_service's JWT Key
        };
        // SignalR specific: Read token from query string for WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/chatHub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(); // Enable authorization

// 3. Add SignalR
builder.Services.AddSignalR();

// 4. Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program)); // Scans assembly for profiles (e.g., MessagingProfile)

// 5. Add Messaging Services
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
// Add other services as needed

// 6. Add Controllers and OpenAPI (Swagger)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Buronet API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 7. Configure CORS (Crucial for frontend communication)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins(
                                configuration["FrontendAppUrl"] ?? "http://localhost:3000" // Your Next.js frontend URL
                            )
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()); // Allow credentials for SignalR and HttpOnly cookies
});


var app = builder.Build();

// --- Configure the HTTP request pipeline ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Use HTTPS redirection (if configured)

app.UseCors("AllowSpecificOrigin"); // Use CORS policy

app.UseAuthentication(); // MUST be before UseAuthorization and UseRouting
app.UseAuthorization();

// Map SignalR Hub
app.MapHub<ChatHub>("/chatHub"); // Endpoint for SignalR clients to connect to

app.MapControllers(); // Map API controllers

app.MapGet("/__debug/controllers", (IEnumerable<EndpointDataSource> endpoints) =>
{
    return endpoints
        .SelectMany(e => e.Endpoints)
        .OfType<RouteEndpoint>()
        .Select(e => new
        {
            Route = e.RoutePattern.RawText,
            Order = e.Order,
            DisplayName = e.DisplayName
        });
});


app.Run();
