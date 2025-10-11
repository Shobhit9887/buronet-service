using Buronet.Bytes.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Define CORS policy
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


// Configure MongoDB settings and service
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddScoped<BytePostService>();

builder.Services.AddControllers();

// TODO: Add your Authentication service here (e.g., builder.Services.AddAuthentication(...))

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins); // Apply CORS policy

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
