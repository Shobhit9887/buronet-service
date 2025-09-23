using Buronet.Video.API.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration & Services Setup ---

// Define a CORS policy to allow your Next.js app to communicate with this API
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // Replace with your frontend's actual URL in production
                          policy.WithOrigins("http://localhost:3000")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<VideoPostService>();

builder.Services.AddControllers();

// TODO: Add your Authentication service here (e.g., builder.Services.AddAuthentication(...))

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- HTTP Request Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply the CORS policy
app.UseCors(MyAllowSpecificOrigins);

// Make sure to add UseAuthentication() before UseAuthorization()
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

