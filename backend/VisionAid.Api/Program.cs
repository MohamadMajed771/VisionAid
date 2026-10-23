using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string is missing.");

builder.Services.AddDbContext<VisionAidDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();