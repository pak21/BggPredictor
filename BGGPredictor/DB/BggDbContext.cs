using System.Data.Entity;

namespace BGGPredictor.DB
{
    internal class BggDbContext : DbContext
    {
        public BggDbContext() : base("BggPredictor")
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<CollectionItem> CollectionItems { get; set; }
    }
}
