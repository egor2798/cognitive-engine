using CognitiveEngine.API.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- сервисы ---------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Cognitive Engine API", Version = "v1" });
});

var connection = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=cognitive_engine;Username=cogengine;Password=cogengine_dev_pwd";

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connection)
       .UseSnakeCaseNamingConvention());

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod()));

var app = builder.Build();

// ---- pipeline --------------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
// HTTPS redirect is disabled for local demo so http://localhost:5000 works without certificate problems.
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

// ---- быстрая проверка соединения с БД при старте ---------------------------
// Схему создаёт init.sql (через docker-compose) либо вручную через psql.
// Здесь не запускаем Migrate() намеренно — миграции не используем, источник истины — init.sql.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        Console.WriteLine(canConnect
            ? "[startup] PostgreSQL: OK"
            : "[startup] PostgreSQL: НЕТ соединения (проверьте docker compose / connection string)");

        if (canConnect)
        {
            var bpCount = await db.BodyPoints.CountAsync();
            if (bpCount != 11)
                Console.WriteLine($"[startup] WARN: справочник body_points содержит {bpCount} записей из 11. Накатите db/seed.sql.");
            else
                Console.WriteLine("[startup] body_points: 11 ✓");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[startup] DB check failed: {ex.Message}");
    }
}

app.Run();
