using Microsoft.EntityFrameworkCore;

namespace productApi.Context
{

    public class productDb : DbContext
    {
        public productDb(DbContextOptions<productDb> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    }
}