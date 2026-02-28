using CognitiveEngine.API.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Добавляем InMemory базу данных
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("CognitiveEngineDB"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Проверяем, есть ли уже данные
    if (!db.Organizations.Any())
    {
        var org = new Organization { Name = "Test Clinic" };
        db.Organizations.Add(org);

        db.Users.Add(new User
        {
            Email = "admin@test.com",
            Name = "Admin",
            Role = "Admin",
            Organization = org
        });

        db.SaveChanges();
    }
}

app.Run();