using BlindMatchAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace BlindMatchAPI.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ResearchArea> ResearchAreas { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<SupervisorPreference> SupervisorPreferences { get; set; }
       

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Project → Student relationship
            builder.Entity<Project>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match → Project relationship
            builder.Entity<Match>()
                .HasOne(m => m.Project)
                .WithOne(p => p.Match)
                .HasForeignKey<Match>(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Match → Supervisor relationship
            builder.Entity<Match>()
                .HasOne(m => m.Supervisor)
                .WithMany()
                .HasForeignKey(m => m.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // SupervisorPreference relationships
            builder.Entity<SupervisorPreference>()
                .HasOne(sp => sp.Supervisor)
                .WithMany()
                .HasForeignKey(sp => sp.SupervisorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SupervisorPreference>()
                .HasOne(sp => sp.ResearchArea)
                .WithMany(r => r.SupervisorPreferences)
                .HasForeignKey(sp => sp.ResearchAreaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}