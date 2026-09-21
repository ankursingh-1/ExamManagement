using ExamManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Exam>()
                .Property(x => x.ApplicationFee)
                .HasPrecision(18, 2);

            builder.Entity<ExamEligibility>()
                .Property(x => x.MinimumPercentage)
                .HasPrecision(5, 2);
        }

        public DbSet<InstituteSetting> InstituteSettings { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamEligibility> ExamEligibilities { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
    }
}