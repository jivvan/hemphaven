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

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 2,
                    Description = "Everyday-wear flat-front shorts made with lightweight organic cotton/hemp fabric for cool-wearing comfort in hot weather. Inseam is 8''; also available in 6'' inseam. Made in a Fair Trade Certified™ factory.",
                    ListPrice = 1500,
                    Price = 1250,
                    Size = "Medium",
                    Title = "Men's Lightweight All-Wear Hemp Shorts",
                    CategoryId = 1,
                    ImageUrl = "/images/product/329c7372-1537-4ea1-ab32-88dfb7bd9dea.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 3,
                    Description = "A sturdy and comfortable pocket tee for hard work, built with a breathable blend of industrial hemp and organic cotton. Made in a Fair Trade Certified™ factory.",
                    ListPrice = 700,
                    Price = 650,
                    Size = "Large",
                    Title = "Men's Work Pocket T-Shirt",
                    CategoryId = 2,
                    ImageUrl = "/images/product/2a8ecc1a-bd7a-4ff6-ba5f-5a87d9384ad5.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 4,
                    Description = "A cool-wearing, short-sleeved work shirt made from a lightweight, durable and highly breathable 53% industrial hemp/44% recycled polyester/3% spandex blend. Made in a Fair Trade Certified™ factory.",
                    ListPrice = 2000,
                    Price = 1750,
                    Size = "Medium",
                    Title = "Men's Western Snap Shirt",
                    CategoryId = 2,
                    ImageUrl = "/images/product/5eb3c3f0-8803-40bc-baf3-6253ad79d5dc.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 5,
                    Description = "Grab it at dawn and leave it on—the new Full-Zip Work Hoody Sweatshirt is warm, comfortable and durable—whether you wear it as a midlayer or outer layer. Hemp-tough on the outside, fleece-warm on the inside, it’s your go-to for getting it done on chilly jobsites. Made in a Fair Trade Certified™ factory.",
                    ListPrice = 3000,
                    Price = 2500,
                    Size = "Small",
                    Title = "Men's Full-Zip Work Hoody Sweatshirt",
                    CategoryId = 2,
                    ImageUrl = "/images/product/543a2b69-1f7b-48ce-9efe-499ed403e974.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 6,
                    Description = "Discover the essence of bohemian style with our handcrafted Gheri Cotton Patched Pure Hemp Hat. Made from eco-friendly hemp fibers, this hat combines comfort and sustainability. With its unique design and vibrant colors, it adds a touch of free-spirited charm to any outfit. Whether you’re strolling through festivals, lounging on the beach, or exploring nature, our Bohemian Hippie Hemp Hat is the perfect accessory to express your individuality and embrace the boho lifestyle. Shop now and elevate your fashion game with this timeless and earth-friendly accessory.",
                    ListPrice = 1900,
                    Price = 1750,
                    Size = null,
                    Title = "Forest green tone Sustainable gheri hemp backpack",
                    CategoryId = 3,
                    ImageUrl = "/images/product/d2cd8c4a-0495-4672-8166-42a9fcb2f285.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 7,
                    Description = "Discover the essence of bohemian style with our handcrafted Gheri Cotton Patched Pure Hemp Hat. Made from eco-friendly hemp fibers, this hat combines comfort and sustainability. With its unique design and vibrant colors, it adds a touch of free-spirited charm to any outfit. Whether you’re strolling through festivals, lounging on the beach, or exploring nature, our Bohemian Hippie Hemp Hat is the perfect accessory to express your individuality and embrace the boho lifestyle. Shop now and elevate your fashion game with this timeless and earth-friendly accessory.",
                    ListPrice = 500,
                    Price = 500,
                    Size = null,
                    Title = "Gheri Cotton Patched Pure Hemp Hat",
                    CategoryId = 3,
                    ImageUrl = "/images/product/1179cf12-e985-4907-b1cf-b42ff55b3ee4.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 8,
                    Description = "Hemp in Nepal, a leading hippie hemp product manufacturer, offers the Gheri Colorful Mushroom Printed Hemp Bag. Likewise, crafted from the finest Himalayan hemp, our Colorful Mushroom Printed Hemp Bag includes the essence of eco-friendly fashion. Furthermore, this bag is designed with functionality in mind. With multiple pockets, it provides ample space for organizing your daily essentials. Additionally, from your smartphone to your wallet and keys, everything has a dedicated place. Likewise, the vibrant colors and intricate mushroom pattern make this hemp bag a statement piece that reflects your personality and love for nature.",
                    ListPrice = 1000,
                    Price = 899,
                    Size = null,
                    Title = "Colorful Mushroom Printed Hemp Bag",
                    CategoryId = 1,
                    ImageUrl = "/images/product/60c30210-b728-4fae-adbb-206801b4565d.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 9,
                    Description = "Enhance your musical style with our Blue Tone Buddha Eyes Printed Gheri Guitar bag. Crafted with love and expertise, this eco-friendly bag is designed to protect your precious guitar while adding a touch of bohemian charm. Made from natural hemp fibers, it offers superior durability and breathability, ensuring your instrument stays safe and in optimal condition. With its vibrant colors and unique hippie-inspired patterns, our guitar bag is a true statement piece that reflects your free-spirited personality. Whether you’re a seasoned musician or a passionate beginner, our Handmade Hippie Hemp Guitar Bag is the perfect blend of style and functionality. Elevate your guitar carrying experience and embrace the sustainable lifestyle today.",
                    ListPrice = 3000,
                    Price = 2499,
                    Size = null,
                    Title = "Blue Tone Buddha Eyes Printed Gheri Guitar Bag",
                    CategoryId = 3,
                    ImageUrl = "/images/product/d72091a2-3476-4273-b9fe-242f3871e6e1.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 10,
                    Description = "Made with a blend of ultralightweight 95% organic cotton/5% hemp, this crepe-woven shirt has a slightly raised texture to keep fabric lifted from skin.",
                    ListPrice = 1200,
                    Price = 1000,
                    Size = "Large",
                    Title = "Women's Lightweight A/C® Shirt",
                    CategoryId = 1,
                    ImageUrl = "/images/product/132f9538-bb46-4940-b0f0-b94d0d9ad06b.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 11,
                    Description = "Airy, everyday shirt made with lightweight hemp/organic cotton fabric for cool-wearing comfort in hot weather. Made in a Fair Trade Certified™ factory.",
                    ListPrice = 2500,
                    Price = 1500,
                    Size = "Small",
                    Title = "Men's Back Step Shirt",
                    CategoryId = 1,
                    ImageUrl = "/images/product/dbd182fa-2628-4996-b0fc-689915ac01ec.jpg"
                }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 12,
                    Description = "Designed for hot and humid weather, this tee is made from a cool blend of hemp and organic cotton. Made in a Fair Trade Certified™ factory.",
                    ListPrice = 1500,
                    Price = 1399,
                    Size = "Medium",
                    Title = "Women's Trail Harbor T-Shirt",
                    CategoryId = 1,
                    ImageUrl = "/images/product/73523345-6857-4c5b-81bd-b572a5642e2d.jpg"
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