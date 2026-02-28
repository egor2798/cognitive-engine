using Microsoft.EntityFrameworkCore;

namespace CognitiveEngine.API.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<TrainingResult> TrainingResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId);

        modelBuilder.Entity<Patient>()
            .HasOne(p => p.Organization)
            .WithMany(o => o.Patients)
            .HasForeignKey(p => p.OrganizationId);

        modelBuilder.Entity<TrainingResult>()
            .HasOne(t => t.Patient)
            .WithMany(p => p.TrainingResults)
            .HasForeignKey(t => t.PatientId);

        modelBuilder.Entity<TrainingResult>()
            .HasOne(t => t.Exercise)
            .WithMany()
            .HasForeignKey(t => t.ExerciseId);
    }
}