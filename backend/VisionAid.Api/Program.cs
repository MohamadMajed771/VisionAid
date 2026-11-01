using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Data;
using VisionAid.Api.Services;
using VisionAid.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString));
builder.Services.AddHttpClient<IAnalysisService, AnalysisService>(client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:8000/");
    client.Timeout = TimeSpan.FromMinutes(5);
});
    builder.Services.AddScoped<IFrameService, FrameService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();