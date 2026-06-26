using AppInventory.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Infrastructure.Data;

public class AppInventoryDbContext : DbContext
{
    public AppInventoryDbContext(DbContextOptions<AppInventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();
    public DbSet<LocalCredential> LocalCredentials => Set<LocalCredential>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<GroupRoleMapping> GroupRoleMappings => Set<GroupRoleMapping>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationEnvironment> ApplicationEnvironments => Set<ApplicationEnvironment>();
    public DbSet<ApplicationContact> ApplicationContacts => Set<ApplicationContact>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ApplicationTag> ApplicationTags => Set<ApplicationTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureExternalIdentity(modelBuilder);
        ConfigureLocalCredential(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigurePermission(modelBuilder);
        ConfigureRolePermission(modelBuilder);
        ConfigureUserRole(modelBuilder);
        ConfigureGroupRoleMapping(modelBuilder);
        ConfigureApplication(modelBuilder);
        ConfigureApplicationEnvironment(modelBuilder);
        ConfigureApplicationContact(modelBuilder);
        ConfigureTag(modelBuilder);
        ConfigureApplicationTag(modelBuilder);

        SeedSystemRoles(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private static void ConfigureExternalIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExternalIdentity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ProviderType)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => new { e.ProviderType, e.ExternalId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.ExternalIdentities)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureLocalCredential(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LocalCredential>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).IsRequired();

            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<LocalCredential>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private static void ConfigurePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private static void ConfigureRolePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(p => p.Roles)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUserRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.Property(e => e.Source)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserAssignments)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureGroupRoleMapping(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupRoleMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalGroupRef).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ProviderType)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => new { e.ProviderType, e.ExternalGroupRef, e.RoleId }).IsUnique();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.GroupMappings)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureApplication(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ShortDescription).HasMaxLength(500).IsRequired();
            entity.Property(e => e.DetailedDescription).HasMaxLength(4000);
            entity.Property(e => e.OwnerTeam).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RepositoryUrl).HasMaxLength(2000);
            entity.Property(e => e.WikiUrl).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.SourceControl).HasConversion<string>().HasMaxLength(50);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.OwnerTeam);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsDeleted);

            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    private static void ConfigureApplicationEnvironment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationEnvironment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);

            entity.HasIndex(e => e.ApplicationId);

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Environments)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureApplicationContact(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationContact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Label).HasMaxLength(200);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);

            entity.HasIndex(e => e.ApplicationId);

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Contacts)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Color).HasMaxLength(7);

            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private static void ConfigureApplicationTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationTag>(entity =>
        {
            entity.HasKey(e => new { e.ApplicationId, e.TagId });

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Tags)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.Applications)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void SeedSystemRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", Description = "Full access; user management, group mappings, all CRUD", IsSystemRole = true },
            new Role { Id = 2, Name = "Developer", Description = "Application CRUD, developer/ops docs read and write", IsSystemRole = true },
            new Role { Id = 3, Name = "ApplicationOwner", Description = "Edit own applications, write user docs", IsSystemRole = true },
            new Role { Id = 4, Name = "ReadOnly", Description = "Read all public content (user docs, catalog)", IsSystemRole = true }
        );
    }
}
