using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context
{
    public class BulkImportDbContext : IdentityDbContext<IdentityUser>
    {
        public BulkImportDbContext(DbContextOptions<BulkImportDbContext> options)
            : base(options) { }

        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Restaurant>().HasKey(r => r.Id);
            modelBuilder.Entity<Restaurant>()
                .Property(r => r.OwnerEmailAddress)
                .HasMaxLength(256);

            modelBuilder.Entity<MenuItem>().HasKey(m => m.Id);
            modelBuilder.Entity<MenuItem>()
                .HasOne(m => m.Restaurant)
                .WithMany(r => r.MenuItems)
                .HasForeignKey(m => m.RestaurantId);

            modelBuilder.Entity<MenuItem>()
                .Property(m => m.ImagePath)
                .HasMaxLength(512);

            SeedAdmin(modelBuilder);
        }

        private static void SeedAdmin(ModelBuilder modelBuilder)
        {
            var adminRole = new IdentityRole
            {
                Id = "f3e7c755-4aef-4db5-a5d9-165ac5fe2050",
                Name = "Admin",
                NormalizedName = "ADMIN"
            };

            var adminUser = new IdentityUser
            {
                Id = "a11b2fce-24d8-44c0-9b75-30f48fe599f9",
                UserName = "admin@site.com",
                NormalizedUserName = "ADMIN@SITE.COM",
                Email = "admin@site.com",
                NormalizedEmail = "ADMIN@SITE.COM",
                EmailConfirmed = true,
                SecurityStamp = "2b9d9fbc-5e0b-4db8-8720-170c8353d4c3",
                ConcurrencyStamp = "6e3e4d16-c705-4c1f-9727-4e94516ce0a1"
            };

            var hasher = new PasswordHasher<IdentityUser>();
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin123!");

            modelBuilder.Entity<IdentityRole>().HasData(adminRole);
            modelBuilder.Entity<IdentityUser>().HasData(adminUser);
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = adminRole.Id,
                UserId = adminUser.Id
            });
        }
    }
}
