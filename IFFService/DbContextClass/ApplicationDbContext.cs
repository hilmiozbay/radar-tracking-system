using Microsoft.EntityFrameworkCore;
using TrackLibrary;

namespace IFFService.DbContextClass
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Track> Tracks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMongoDB("mongodb://localhost:27017", "radar");
            Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        }

    }
}
