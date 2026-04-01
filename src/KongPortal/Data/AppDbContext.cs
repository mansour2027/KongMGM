using KongPortal.Models.Domain;
using KongPortal.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KongPortal.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ConsumerProfile> ConsumerProfiles { get; set; }
    public DbSet<RotationRecord> RotationRecords { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ConsumerProfile>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.KongConsumerUsername).IsUnique();
            e.Property(x => x.KongConsumerUsername).IsRequired().HasMaxLength(200);
            e.Property(x => x.ServiceName).HasMaxLength(200);
            e.Property(x => x.TeamName).HasMaxLength(200);
            e.Property(x => x.ContactEmail).HasMaxLength(200);
        });

        builder.Entity<RotationRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.KongConsumerUsername, x.RotatedAt });
            e.Property(x => x.KongConsumerUsername).IsRequired().HasMaxLength(200);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.At);
            e.HasIndex(x => x.PerformedBy);
            e.Property(x => x.Action).IsRequired().HasMaxLength(100);
            e.Property(x => x.Resource).HasMaxLength(300);
            e.Property(x => x.PerformedBy).HasMaxLength(200);
        });
    }
}

public static class DbSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
        string[] roles = [Roles.Admin, Roles.Operator, Roles.Viewer];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));
        }
    }
}
