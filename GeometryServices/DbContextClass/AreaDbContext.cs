
using Microsoft.EntityFrameworkCore;
using TrackLibrary;

namespace GeometryServices.DbContextClass
{
    public class AreaDbContext : DbContext // Db context for Area  => In area / Not in area
    {
        public AreaDbContext(DbContextOptions options) : base(options) { }
        
        public DbSet<Area> Areas { get; set; }

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
