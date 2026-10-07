using ExamManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
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

            builder.Entity<ExamCutoff>()
                .Property(x => x.CutoffValue)
                .HasPrecision(5, 2);

            builder.Entity<ExamResult>()
                .Property(x => x.TotalMarks)
                .HasPrecision(10, 2);

            builder.Entity<ExamResult>()
                .Property(x => x.ObtainedMarks)
                .HasPrecision(10, 2);

            builder.Entity<ExamResult>()
                .Property(x => x.Percentage)
                .HasPrecision(5, 2);

            builder.Entity<ExamResultSubject>()
               .Property(x => x.TotalMarks)
               .HasPrecision(10, 2);

            builder.Entity<ExamResultSubject>()
                .Property(x => x.ObtainedMarks)
                .HasPrecision(10, 2);

            builder.Entity<ExamResultSubject>()
                .Property(x => x.Percentage)
                .HasPrecision(5, 2);

            builder.Entity<ExamSubject>()
                .HasOne(x => x.Exam)
                .WithMany(x => x.Subjects)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentApplicationSubject>()
                .HasOne(x => x.StudentApplication)
                .WithMany()
                .HasForeignKey(x => x.StudentApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentApplicationSubject>()
                .HasOne(x => x.ExamSubject)
                .WithMany()
                .HasForeignKey(x => x.ExamSubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<InstituteSetting> InstituteSettings { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamEligibility> ExamEligibilities { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<StudentApplication> StudentApplications { get; set; }
        public DbSet<StudentApplicationDocument> StudentApplicationDocuments { get; set; }
        public DbSet<StudentApplicationPayment> StudentApplicationPayments { get; set; }
        public DbSet<ExamCenter> ExamCenters { get; set; }
        public DbSet<ExamCutoff> ExamCutoffs { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<ExamResultSubject> ExamResultSubjects { get; set; }
        public DbSet<ExamSubject> ExamSubjects { get; set; }
        public DbSet<StudentApplicationSubject> StudentApplicationSubjects { get; set; }
    }
}