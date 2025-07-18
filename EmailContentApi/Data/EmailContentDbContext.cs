using Microsoft.EntityFrameworkCore;
using EmailContentApi.Models;

namespace EmailContentApi.Data
{
    public class EmailContentDbContext : DbContext
    {
        public EmailContentDbContext(DbContextOptions<EmailContentDbContext> options)
            : base(options)
        {
        }

        public DbSet<EmailContent> EmailContents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmailContent>(entity =>
            {
                entity.ToTable("EmailContent");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
} 