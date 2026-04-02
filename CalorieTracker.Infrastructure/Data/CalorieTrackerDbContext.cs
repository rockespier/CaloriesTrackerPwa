using CalorieTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CalorieTracker.Infrastructure.Data
{
    public class CalorieTrackerDbContext : DbContext
    {
        public CalorieTrackerDbContext(DbContextOptions<CalorieTrackerDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<FoodLog> FoodLogs { get; set; }

        public DbSet<UserProfileHistory> UserProfileHistory { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Mapeo Fluent API para Database First
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(512);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });
        }
    }
}