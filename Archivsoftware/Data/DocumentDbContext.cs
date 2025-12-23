using DocumentManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Data
{
    public class DocumentDbContext : DbContext
    {
        public DbSet<Folder> Folders { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=ArchivSoftwareDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Folder>()
                .HasIndex(f => new { f.Name })
                .IsUnique();

            modelBuilder.Entity<Folder>()
                .HasMany(f => f.SubFolders)
                .WithOne(f => f.ParentFolder)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Folder>()
                .HasMany(f => f.Documents)
                .WithOne(d => d.Folder)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
