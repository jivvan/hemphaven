using BulkyWeb.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Product>().HasOne(p => p.Category);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Trending", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Clothing", DisplayOrder = 1 },
                new Category { Id = 3, Name = "Accessories", DisplayOrder = 1 }
            );
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Description = "This men's kurta is crafted from the finest organic hemp, offering both unparalleled comfort and a commitment to sustainability. Hemp is naturally breathable and soft, making it perfect for year-round wear. It also boasts natural moisture-wicking properties to keep you cool and dry.",
                    ListPrice = 799,
                    Price = 759,
                    Size = "Medium",
                    Title = "Men's Organic Hemp Kurta",
                    CategoryId = 2,
                    ImageUrl = "https://www.hempinnepal.com/wp-content/uploads/2017/08/hemp-shirt.jpg"
                }
            );

            modelBuilder.Entity<Company>().HasData(
              new Company { Id = 1, Name = "Himal Fancy", StreetAddress = "Lalitpur, 5", City = "Kathmandu", PhoneNumber = "9876543210", PostalCode = "1231", State = "Bagmati" },
              new Company { Id = 2, Name = "Enigma Clothing Store", StreetAddress = "Lalitpur, 5", City = "Kathmandu", PhoneNumber = "9876543210", PostalCode = "1231", State = "Bagmati" },
              new Company { Id = 3, Name = "Ecloth Nepal", StreetAddress = "Lalitpur, 5", City = "Kathmandu", PhoneNumber = "9876543210", PostalCode = "1231", State = "Bagmati" }
          );
        }
    }
}