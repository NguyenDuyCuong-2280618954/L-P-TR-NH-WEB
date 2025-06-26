using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Drawing;
using System.Text.Json;
using WebThoiTrang.Models.Entity;


public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    
    public  ApplicationDbContext (DbContextOptions<ApplicationDbContext> options) :
        base(options)
    { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<EmailConfirmation> EmailConfirmations { get; set; }
    public DbSet<GeneratedImage> GeneratedImages { get; set; }
    public DbSet<Collection> Collections { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var sizeConverter = new ValueConverter<List<string>, string>(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

        builder.Entity<Product>()
            .Property(p => p.Sizes)
            .HasConversion(sizeConverter)
            .HasColumnType("nvarchar(max)");

        builder.Entity<OrderDetail>()
        .HasOne(od => od.Order)
        .WithMany(o => o.OrderDetails)
        .HasForeignKey(od => od.OrderId)
        .OnDelete(DeleteBehavior.Cascade);


        builder.Entity<Product>()
         .HasOne(p => p.Collection) // Một Product có một Collection
         .WithMany(c => c.Products) // <--- CHỈNH SỬA DÒNG NÀY!
                                    // Khai báo rõ ràng navigation property ở phía Collection
         .HasForeignKey(p => p.CollectionId)
         .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>().OwnsOne(o => o.ShippingInfo);

        /* 1️⃣  DANH MỤC ---------------------------------------------------- */
        builder.Entity<Category>().HasData(
            
            // Categories
        
            new Category { Id = 1, Name = "Dresses", },
            new Category { Id = 2, Name = "Men's Tops" },
            new Category { Id = 3, Name = "Mini Dressess"},
            new Category { Id = 4, Name = "Midi Dresses" },
            new Category { Id = 5, Name = "Polos" },
            new Category { Id = 6, Name = "T-Shirts" }
       

        );
        /*2   COLLECTION ---------------------------------------------------- */
        builder.Entity<Collection>().HasData(
            new Collection { Id = 1, Name = "Man Collection"},
            new Collection { Id = 2, Name = "Womman Collection"}
            );

        /*3   SẢN PHẨM ---------------------------------------------------- */
        builder.Entity<Product>().HasData(
            new
            {
                Id = 1,
                Name = "Elegant Button-Front Midi Dress",
                Price = 1290000m,
                MainImage = "/images/products/black-button-midi-dress.jpg",
                Color = "Black",
                CategoryId = 1,
                CollectionId = 2,
                Description = "A sophisticated sleeveless black midi dress featuring decorative front buttons and a subtle flare at the hem. Perfect for both office settings and evening occasions with its timeless silhouette.",
                Sizes = new List<string> { "XS", "S", "M", "L" },
                CreatedDate = new DateTime(2025, 5, 15),
                IsAvailable = true,
                Rating = 3
            },

            new
            {
                Id = 2,
                Name = "Nautical Stripe Mini Dress",
                Price = 790000m,
                MainImage = "/images/products/striped-mini-dress.jpg",
                Color = "Navy/White",
                CategoryId = 1,
                CollectionId = 2,
                Description = "A playful navy and white striped mini dress with wide straps and a flattering square neckline. Made from breathable cotton blend fabric, perfect for beach outings and casual summer days.",
                Sizes = new List<string> { "XS", "S", "M", "L" },
                CreatedDate = new DateTime(2025, 5, 20),
                IsAvailable = true,
                Rating = 4
            },

            new
            {
                Id = 3,
                Name = "Smocked Bodice Midi Sundress",
                Price = 1150000m,
                MainImage = "/images/products/white-midi-sundress.jpg",
                Color = "White",
                CategoryId = 1,
                CollectionId = 2,
                Description = "An elegant white sundress featuring a smocked bodice, thin shoulder straps, and a flowing A-line skirt that falls to mid-calf. The lightweight cotton fabric offers comfort during warm weather while maintaining a sophisticated look.",
                Sizes = new List<string> { "XS", "S", "M", "L" },
                CreatedDate = new DateTime(2025, 5, 25),
                IsAvailable = true,
                Rating = 5
            },

            new
            {
                Id = 4,
                Name = "Python Print Halter Mini Dress",
                Price = 1350000m,
                MainImage = "/images/products/python-print-dress.jpg",
                Color = "Beige/Brown",
                CategoryId = 1,
                CollectionId = 2,
                Description = "A statement mini dress featuring a bold python print pattern in neutral tones. The halter neckline and relaxed silhouette create a modern, sophisticated look perfect for summer evenings and special occasions.",
                Sizes = new List<string> { "XS", "S", "M", "L" },
                CreatedDate = new DateTime(2025, 6, 1),
                IsAvailable = true,
                Rating = 4
            },

            new
            {
                Id = 5,
                Name = "Smocked Waist Utility Mini Dress",
                Price = 950000m,
                MainImage = "/images/products/olive-utility-dress.jpg",
                Color = "Olive",
                CategoryId = 1,
                CollectionId = 2,
                Description = "A versatile olive green mini dress with cap sleeves and a square neckline. Features smocked detailing at the waist for a flattering fit, with a slightly flared skirt. Pairs perfectly with both casual footwear and dressier options.",
                Sizes = new List<string> { "XS", "S", "M", "L" },
                CreatedDate = new DateTime(2025, 6, 10),
                IsAvailable = true,
                Rating = 5
            },
            new
            {
                Id = 6,
                Name = "Nike Dri-FIT Essential Polo",
                Price = 850000m,
                MainImage = "/images/products/nike-light-pink-polo.jpg",
                Color = "Light Pink",
                CategoryId = 2,
                CollectionId = 1,
                Description = "A premium light pink Nike polo shirt featuring moisture-wicking Dri-FIT technology. The minimalist design includes a classic three-button placket and embroidered Nike logo, perfect for both athletic activities and casual wear.",
                Sizes = new List<string> { "S", "M", "L", "XL" },
                CreatedDate = new DateTime(2025, 5, 15),
                IsAvailable = true,
                Rating = 4
            },

            new
            {
                Id = 7,
                Name = "Adidas Originals 3-Stripe Tee",
                Price = 650000m,
                MainImage = "/images/products/adidas-white-tshirt.jpg",
                Color = "White",
                CategoryId = 2,
                CollectionId = 1,
                Description = "A classic white Adidas t-shirt featuring the iconic three stripes along the shoulders. Made from soft cotton jersey with a relaxed fit and small gold Adidas trefoil logo on the chest for a subtle branded touch.",
                Sizes = new List<string> { "S", "M", "L", "XL" },
                CreatedDate = new DateTime(2025, 5, 18),
                IsAvailable = true,
                Rating = 3
            },

            new
            {
                Id = 8,
                Name = "Nike Court Tennis Polo",
                Price = 890000m,
                MainImage = "/images/products/nike-white-tennis-polo.jpg",
                Color = "White",
                CategoryId = 2,
                CollectionId = 1,
                Description = "A performance-oriented white tennis polo from Nike's Court collection. Features a clean, minimal design with a small court logo on the chest. Made with lightweight, breathable fabric ideal for athletic activities.",
                Sizes = new List<string> { "S", "M", "L", "XL" },
                CreatedDate = new DateTime(2025, 5, 22),
                IsAvailable = true,
                Rating = 4
            },

            new
            {
                Id = 9,
                Name = "Retro '90 Graphic Tee",
                Price = 590000m,
                MainImage = "/images/products/red-90-tshirt.jpg",
                Color = "Red",
                CategoryId = 2,
                CollectionId = 1,
                Description = "A vibrant red t-shirt featuring a bold '90' graphic in white circle print. Made from soft cotton jersey with a comfortable regular fit, perfect for casual everyday wear with a touch of retro style.",
                Sizes = new List<string> { "S", "M", "L", "XL" },
                CreatedDate = new DateTime(2025, 6, 1),
                IsAvailable = true,
                Rating = 4
            },

            new
            {
                Id = 10,
                Name = "Premium Heathered T-Shirt",
                Price = 550000m,
                MainImage = "/images/products/heather-grey-tshirt.jpg",
                Color = "Heather Grey",
                CategoryId = 2,
                CollectionId = 1,
                Description = "A versatile heather grey t-shirt made from premium cotton blend fabric. Features a classic crew neck and relaxed fit with subtle texture throughout. The perfect essential piece for everyday casual styling.",
                Sizes = new List<string> { "S", "M", "L", "XL" },
                CreatedDate = new DateTime(2025, 6, 5),
                IsAvailable = true,
                Rating = 4
            }


        );


        /* 3️⃣  ẢNH PHỤ ---------------------------------------------------- */
        builder.Entity<ProductImage>().HasData(
            
        );
    }


}