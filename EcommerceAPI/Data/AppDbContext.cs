using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Models;

namespace EcommerceAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Khai báo DbSet cho tất cả các bảng
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserAddress> UserAddresses { get; set; }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<UserVoucher> UserVouchers { get; set; }

    public DbSet<UserTier> UserTiers { get; set; }
    public DbSet<PointHistory> PointHistories { get; set; }

    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Cấu hình mặc định cho tất cả các cột kiểu decimal là (18,2)
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
    // Ghi đè phương thức Lưu dữ liệu mặc định của Entity Framework
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Yêu cầu EF Core tìm tất cả các bảng có kế thừa từ AuditableEntity
        // và đang trong trạng thái được Thêm mới (Added) hoặc Cập nhật (Modified)
        var entries = ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Nếu là Thêm mới -> Tự động chèn giờ hiện tại vào CreatedAt
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            // Nếu là Cập nhật -> Tự động chèn giờ hiện tại vào UpdatedAt
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Sau khi cấu hình xong thời gian, mới gọi lệnh lưu thực sự vào SQL Server
        return base.SaveChangesAsync(cancellationToken);
    }
}