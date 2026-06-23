using AppInventory.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppInventory.Infrastructure.Data;

public class AppInventoryDbContext : DbContext
{
    public AppInventoryDbContext(DbContextOptions<AppInventoryDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();
    public DbSet<LocalCredential> LocalCredentials => Set<LocalCredential>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<GroupRoleMapping> GroupRoleMappings => Set<GroupRoleMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<ExternalIdentity>(entity =>
        {
            entity.ToTable("ExternalIdentities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProviderType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ExternalId).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => new { e.ProviderType, e.ExternalId }).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.ExternalIdentities)
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LocalCredential>(entity =>
        {
            entity.ToTable("LocalCredentials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User).WithMany()
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasData(
                new Role { Id = 1, Name = "Admin", Description = "Full system access", IsSystemRole = true },
                new Role { Id = 2, Name = "Developer", Description = "Developer access to applications and documentation", IsSystemRole = true },
                new Role { Id = 3, Name = "ApplicationOwner", Description = "Manage owned applications", IsSystemRole = true },
                new Role { Id = 4, Name = "ReadOnly", Description = "Read-only access", IsSystemRole = true }
            );
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasOne(e => e.Role).WithMany(r => r.Permissions)
                .HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission).WithMany(p => p.Roles)
                .HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.Property(e => e.Source).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.GrantedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role).WithMany(r => r.UserAssignments)
                .HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupRoleMapping>(entity =>
        {
            entity.ToTable("GroupRoleMappings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProviderType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ExternalGroupRef).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasOne(e => e.Role).WithMany(r => r.GroupMappings)
                .HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
