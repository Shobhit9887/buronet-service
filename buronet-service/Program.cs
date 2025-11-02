using AutoMapper;
using buronet_service.Data;
using buronet_service.Helpers;
using buronet_service.Mappings;
using buronet_service.Services;
using buronet_service.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddHttpClient("JobService", client =>
{
    // *** IMPORTANT: Replace this with the actual URL of your JobService microservice ***
    // If you are using Docker/K8s/API Gateway, this URL will be the internal service name or gateway route.
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:JobService"] ?? "https://localhost:44318");

    // Add any default headers needed for microservice communication, e.g., an internal API key
    // client.DefaultRequestHeaders.Add("X-Internal-API-Key", "..."); 
});

builder.Services.AddHttpClient("NotificationsService", client =>
{
    // *** IMPORTANT: Replace this with the actual URL of your JobService microservice ***
    // If you are using Docker/K8s/API Gateway, this URL will be the internal service name or gateway route.
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:NotificationsService"]!);

    // Add any default headers needed for microservice communication, e.g., an internal API key
    // client.DefaultRequestHeaders.Add("X-Internal-API-Key", "..."); 
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

//*********************** Add services to the container.***********************
builder.Services.AddSingleton<IHeroService, HeroService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>(); // <--- NEW: Register PostService with its interface
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<INotificationsService, NotificationsService>();
//*********************** Add services to the container end.***********************

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program)); // Or the assembly containing your profiles
builder.Services.AddAutoMapper(typeof(UserProfileMappingProfile).Assembly);

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}"; // Your Auth0 Domain
        //options.Audience = builder.Configuration["Auth0:Audience"];             // Your API Identifier
        //// Optional: for stricter validation (e.g., against specific audiences in token)
        //// options.TokenValidationParameters = new TokenValidationParameters
        //// {
        ////     ValidAudience = builder.Configuration["Auth0:Audience"],
        ////     ValidIssuer = $"https://{builder.Configuration["Auth0:Domain"]}/" // Note the trailing slash
        //// };


        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenAll", policy =>
    {
        policy.WithOrigins("http://ec2-13-48-45-225.eu-north-1.compute.amazonaws.com")  // or your frontend URL
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("OpenAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
