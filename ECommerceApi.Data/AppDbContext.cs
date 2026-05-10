using ECommerceApi.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── ApplicationUser ──────────────────────────────────────
        builder.Entity<ApplicationUser>(e =>
        {
            e.HasMany(u => u.Addresses)
             .WithOne(a => a.User)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(u => u.Orders)
             .WithOne(o => o.User)
             .HasForeignKey(o => o.UserId)
             .OnDelete(DeleteBehavior.Restrict);   // لا نحذف أوردرات لو حذفنا يوزر

            e.HasOne(u => u.Cart)
             .WithOne(c => c.User)
             .HasForeignKey<Cart>(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(u => u.Reviews)
             .WithOne(r => r.User)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(u => u.Wishlists)
             .WithOne(w => w.User)
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // Seller → Products
            e.HasMany(u => u.Products)
             .WithOne(p => p.Seller)
             .HasForeignKey(p => p.SellerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Category (self-referencing) ───────────────────────────
        builder.Entity<Category>(e =>
        {
            e.HasOne(c => c.Parent)
             .WithMany(c => c.SubCategories)
             .HasForeignKey(c => c.ParentId)
             .OnDelete(DeleteBehavior.Restrict);   // لو حذفنا parent لا تحذف children
        });

        // ── Product ───────────────────────────────────────────────
        builder.Entity<Product>(e =>
        {
            e.Property(p => p.Price)
             .HasColumnType("decimal(18,2)");

            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Order ─────────────────────────────────────────────────
        builder.Entity<Order>(e =>
        {
            e.Property(o => o.Total)
             .HasColumnType("decimal(18,2)");

            e.HasOne(o => o.Address)
             .WithMany(a => a.Orders)
             .HasForeignKey(o => o.AddressId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(o => o.Items)
             .WithOne(i => i.Order)
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(o => o.Payment)
             .WithOne(p => p.Order)
             .HasForeignKey<Payment>(p => p.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── OrderItem ─────────────────────────────────────────────
        builder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.UnitPrice)
             .HasColumnType("decimal(18,2)");

            e.HasOne(i => i.Product)
             .WithMany(p => p.OrderItems)
             .HasForeignKey(i => i.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Cart ──────────────────────────────────────────────────
        builder.Entity<Cart>(e =>
        {
            e.HasMany(c => c.Items)
             .WithOne(i => i.Cart)
             .HasForeignKey(i => i.CartId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CartItem ──────────────────────────────────────────────
        builder.Entity<CartItem>(e =>
        {
            e.HasOne(i => i.Product)
             .WithMany(p => p.CartItems)
             .HasForeignKey(i => i.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Review ────────────────────────────────────────────────
        builder.Entity<Review>(e =>
        {
            // كل user يقدر يعمل review واحدة بس على نفس المنتج
            e.HasIndex(r => new { r.UserId, r.ProductId }).IsUnique();

            e.HasOne(r => r.Product)
             .WithMany(p => p.Reviews)
             .HasForeignKey(r => r.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Wishlist ──────────────────────────────────────────────
        builder.Entity<Wishlist>(e =>
        {
            // منتج واحد مرة واحدة بس في wishlist نفس الـ user
            e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();

            e.HasOne(w => w.Product)
             .WithMany(p => p.Wishlists)
             .HasForeignKey(w => w.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Payment ───────────────────────────────────────────────
        builder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount)
             .HasColumnType("decimal(18,2)");
        });
    }
}