using Microsoft.EntityFrameworkCore;

namespace CognitiveEngine.API.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // — справочники / администрирование —
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Consent> Consents => Set<Consent>();

    // — аппаратный слой —
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Tracker> Trackers => Set<Tracker>();
    public DbSet<BodyPoint> BodyPoints => Set<BodyPoint>();
    public DbSet<CalibrationProfile> CalibrationProfiles => Set<CalibrationProfile>();

    // — контент —
    public DbSet<ExerciseTemplate> ExerciseTemplates => Set<ExerciseTemplate>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<FreeTrace> FreeTraces => Set<FreeTrace>();
    public DbSet<DerivedTrack> DerivedTracks => Set<DerivedTrack>();

    // — телеметрия —
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionSample> SessionSamples => Set<SessionSample>();
    public DbSet<SessionEvent> SessionEvents => Set<SessionEvent>();
    public DbSet<Metric> Metrics => Set<Metric>();

    // — отчёты / коммуникация —
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Export> Exports => Set<Export>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // — auth / служебное —
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ------- Organization -------
        b.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.Property(x => x.Type).HasConversion<int>();
            e.HasIndex(x => x.Inn).IsUnique().HasFilter("inn IS NOT NULL");
        });

        // ------- User -------
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.Property(x => x.Role).HasConversion<int>();
            e.HasIndex(x => x.Login).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => new { x.OrganizationId, x.Role });

            e.HasOne(x => x.Organization)
             .WithMany(o => o.Users)
             .HasForeignKey(x => x.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ------- Patient -------
        b.Entity<Patient>(e =>
        {
            e.ToTable("patients");
            e.Property(x => x.Gender).HasConversion<int?>();
            e.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.LastName, x.FirstName });
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.BirthDate);

            e.HasOne(x => x.Organization)
             .WithMany(o => o.Patients)
             .HasForeignKey(x => x.OrganizationId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.User).WithOne()
             .HasForeignKey<Patient>(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Consent -------
        b.Entity<Consent>(e =>
        {
            e.ToTable("consents");
            e.Property(x => x.Type).HasConversion<int>();
            e.HasIndex(x => new { x.PatientId, x.Type, x.CreatedAt });
            e.HasOne(x => x.Patient).WithMany(p => p.Consents)
             .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
        });

        // ------- Device -------
        b.Entity<Device>(e =>
        {
            e.ToTable("devices");
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.OrganizationId, x.Status });
            e.HasIndex(x => x.SerialNumber).IsUnique().HasFilter("serial_number IS NOT NULL");

            e.HasOne(x => x.Organization).WithMany(o => o.Devices)
             .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ActiveCalibrationProfile).WithMany()
             .HasForeignKey(x => x.ActiveCalibrationProfileId).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Tracker -------
        b.Entity<Tracker>(e =>
        {
            e.ToTable("trackers");
            e.Property(x => x.Type).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.DeviceId, x.Status });
            e.HasIndex(x => x.SerialNumber).IsUnique().HasFilter("serial_number IS NOT NULL");

            e.HasOne(x => x.Device).WithMany(d => d.Trackers)
             .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.BodyPoint).WithMany()
             .HasForeignKey(x => x.BodyPointId).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- BodyPoint -------
        b.Entity<BodyPoint>(e =>
        {
            e.ToTable("body_points");
            e.Property(x => x.Side).HasConversion<int>();
            e.Property(x => x.Group).HasConversion<int>();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.OrderIndex).IsUnique();
        });

        // ------- CalibrationProfile -------
        b.Entity<CalibrationProfile>(e =>
        {
            e.ToTable("calibration_profiles");
            e.Property(x => x.Mode).HasConversion<int>();
            e.HasIndex(x => new { x.DeviceId, x.IsActive });
            e.HasIndex(x => x.PatientId);

            e.HasOne(x => x.Device).WithMany(d => d.CalibrationProfiles)
             .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Patient).WithMany()
             .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.CreatedBy).WithMany()
             .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- ExerciseTemplate -------
        b.Entity<ExerciseTemplate>(e =>
        {
            e.ToTable("exercise_templates");
            e.Property(x => x.Scope).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.Scope, x.Status });
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.PatientId);
            e.HasIndex(x => x.ConfigHash);

            e.HasOne(x => x.PreviousVersion).WithMany()
             .HasForeignKey(x => x.PreviousVersionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Organization).WithMany(o => o.Templates)
             .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Patient).WithMany()
             .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedBy).WithMany()
             .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PublishedBy).WithMany()
             .HasForeignKey(x => x.PublishedById).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Exercise -------
        b.Entity<Exercise>(e =>
        {
            e.ToTable("exercises");
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.PatientId, x.Status });
            e.HasIndex(x => x.TemplateId);
            e.HasIndex(x => x.ScheduledAt);

            e.HasOne(x => x.Template).WithMany(t => t.Exercises)
             .HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Patient).WithMany()
             .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AssignedBy).WithMany()
             .HasForeignKey(x => x.AssignedById).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- FreeTrace / DerivedTrack -------
        b.Entity<FreeTrace>(e =>
        {
            e.ToTable("free_traces");
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.PatientId, x.CreatedAt });
            e.HasIndex(x => x.SessionId);

            e.HasOne(x => x.Patient).WithMany(p => p.FreeTraces)
             .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Session).WithMany()
             .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.BodyPoint).WithMany()
             .HasForeignKey(x => x.BodyPointId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CalibrationProfile).WithMany()
             .HasForeignKey(x => x.CalibrationProfileId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Tracker).WithMany()
             .HasForeignKey(x => x.TrackerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedBy).WithMany()
             .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<DerivedTrack>(e =>
        {
            e.ToTable("derived_tracks");
            e.HasIndex(x => x.PatientId);
            e.HasIndex(x => x.SourceFreeTraceId);

            e.HasOne(x => x.Patient).WithMany(p => p.DerivedTracks)
             .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SourceFreeTrace).WithMany()
             .HasForeignKey(x => x.SourceFreeTraceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PatientTemplate).WithMany()
             .HasForeignKey(x => x.PatientTemplateId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedBy).WithMany()
             .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Session -------
        b.Entity<Session>(e =>
        {
            e.ToTable("sessions");
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.PatientId, x.StartedAt });
            e.HasIndex(x => new { x.ExerciseId, x.StartedAt });
            e.HasIndex(x => x.DeviceId);
            e.HasIndex(x => x.StartedAt);
            e.HasIndex(x => x.ConfigHash);

            e.HasOne(x => x.Exercise).WithMany(ex => ex.Sessions)
             .HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Patient).WithMany(p => p.Sessions)
             .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Operator).WithMany()
             .HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Device).WithMany()
             .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CalibrationProfile).WithMany()
             .HasForeignKey(x => x.CalibrationProfileId).OnDelete(DeleteBehavior.Restrict);
        });

        // ------- SessionSample (горячая таблица) -------
        b.Entity<SessionSample>(e =>
        {
            e.ToTable("session_samples");
            e.Property(x => x.Kind).HasConversion<int>();
            e.Property(x => x.SignalStatus).HasConversion<int>();
            // композитный уникум — основа идемпотентности batch-вставки
            e.HasIndex(x => new { x.SessionId, x.Kind, x.SampleIndex }).IsUnique();
            // главный «горячий» поиск: по сессии и времени
            e.HasIndex(x => new { x.SessionId, x.T });
            // фильтр по слоту/курсору
            e.HasIndex(x => new { x.SessionId, x.SlotIndex, x.T });

            e.HasOne(x => x.Session).WithMany(s => s.Samples)
             .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.BodyPoint).WithMany()
             .HasForeignKey(x => x.BodyPointId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Tracker).WithMany()
             .HasForeignKey(x => x.TrackerId).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- SessionEvent -------
        b.Entity<SessionEvent>(e =>
        {
            e.ToTable("session_events");
            e.Property(x => x.Type).HasConversion<int>();
            e.HasIndex(x => new { x.SessionId, x.T });
            e.HasIndex(x => new { x.SessionId, x.Type });

            e.HasOne(x => x.Session).WithMany(s => s.Events)
             .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tracker).WithMany()
             .HasForeignKey(x => x.TrackerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Operator).WithMany()
             .HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Metric -------
        b.Entity<Metric>(e =>
        {
            e.ToTable("metrics");
            e.Property(x => x.Scope).HasConversion<int>();
            e.Property(x => x.Quality).HasConversion<int>();
            e.HasIndex(x => new { x.SessionId, x.Scope, x.Code });

            e.HasOne(x => x.Session).WithMany(s => s.Metrics)
             .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ------- Report -------
        b.Entity<Report>(e =>
        {
            e.ToTable("reports");
            e.Property(x => x.Type).HasConversion<int>();
            e.HasIndex(x => new { x.SessionId, x.Type });

            e.HasOne(x => x.Session).WithMany(s => s.Reports)
             .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GeneratedBy).WithMany()
             .HasForeignKey(x => x.GeneratedById).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Export -------
        b.Entity<Export>(e =>
        {
            e.ToTable("exports");
            e.Property(x => x.Format).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.HasIndex(x => new { x.OwnerEntityType, x.OwnerEntityId });
            e.HasIndex(x => x.Status);

            e.HasOne(x => x.Report).WithMany(r => r.Exports)
             .HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RequestedBy).WithMany()
             .HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Comment -------
        b.Entity<Comment>(e =>
        {
            e.ToTable("comments");
            e.HasIndex(x => new { x.PatientId, x.CreatedAt });
            e.HasIndex(x => x.SessionId);

            e.HasOne(x => x.Patient).WithMany(p => p.Comments)
             .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Session).WithMany(s => s.Comments)
             .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany()
             .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Attachment -------
        b.Entity<Attachment>(e =>
        {
            e.ToTable("attachments");
            e.HasIndex(x => new { x.OwnerEntityType, x.OwnerEntityId });
            e.HasOne(x => x.UploadedBy).WithMany()
             .HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- AuditLog -------
        b.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.Property(x => x.Category).HasConversion<int>();
            e.HasIndex(x => new { x.Category, x.CreatedAt });
            e.HasIndex(x => new { x.ActorUserId, x.CreatedAt });
            e.HasIndex(x => new { x.ObjectType, x.ObjectId });
            e.HasIndex(x => x.SessionId);

            e.HasOne(x => x.ActorUser).WithMany()
             .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Organization).WithMany()
             .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Device).WithMany()
             .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Session).WithMany()
             .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.SetNull);
        });

        // ------- Auth -------
        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.RevokedAt });
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PasswordResetToken>(e =>
        {
            e.ToTable("password_reset_tokens");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Used });
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LoginAttempt>(e =>
        {
            e.ToTable("login_attempts");
            e.HasIndex(x => new { x.Login, x.CreatedAt });
            e.HasIndex(x => new { x.IpAddress, x.CreatedAt });
        });

        b.Entity<IdempotencyKey>(e =>
        {
            e.ToTable("idempotency_keys");
            e.HasIndex(x => new { x.Endpoint, x.Key }).IsUnique();
            e.HasIndex(x => x.ExpiresAt);
            e.HasOne(x => x.Session).WithMany()
             .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ------- timestamps в UTC по всему контексту -------
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties())
                if (prop.ClrType == typeof(DateTime) || prop.ClrType == typeof(DateTime?))
                    prop.SetColumnType("timestamp with time zone");
    }

    public override int SaveChanges()                                 { Touch(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { Touch(); return base.SaveChangesAsync(ct); }
    private void Touch()
    {
        var now = DateTime.UtcNow;
        foreach (var en in ChangeTracker.Entries())
        {
            if (en.State == EntityState.Added)
            {
                if (en.Metadata.FindProperty("CreatedAt") != null) en.Property("CreatedAt").CurrentValue = now;
                if (en.Metadata.FindProperty("UpdatedAt") != null) en.Property("UpdatedAt").CurrentValue = now;
            }
            else if (en.State == EntityState.Modified)
            {
                if (en.Metadata.FindProperty("UpdatedAt") != null) en.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
