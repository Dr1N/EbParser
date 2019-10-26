using Microsoft.EntityFrameworkCore;

namespace EbParser.Context
{
    class Site : DbContext
    {
        public DbSet<Post> Posts { get; set; }

        public DbSet<Comment> Comments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=eb.db");
        }
    }
}
