using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // 1. TẠO QUYỀN (ROLES)
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "Quản trị viên toàn hệ thống" },
                    new Role { Id = Guid.NewGuid(), Name = "Staff", Description = "Nhân viên quản lý kho và đơn hàng" },
                    new Role { Id = Guid.NewGuid(), Name = "Customer", Description = "Khách hàng mua sắm" }
                );
                context.SaveChanges();
            }

            // 2. TẠO TÀI KHOẢN MẪU (USERS) - TẤT CẢ PASS LÀ 123456
            if (!context.Users.Any(u => u.Email == "admin@gmail.com"))
            {
                var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
                var staffRole = context.Roles.FirstOrDefault(r => r.Name == "Staff");
                var customerRole = context.Roles.FirstOrDefault(r => r.Name == "Customer");

                string sharedPasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

                // Tạo Admin
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@gmail.com",
                    FullName = "Quản Trị Viên (Hào)",
                    PasswordHash = sharedPasswordHash,
                    IsActive = true
                };

                // Tạo Nhân viên
                var staffUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "staff@gmail.com",
                    FullName = "Nhân Viên Bán Hàng",
                    PasswordHash = sharedPasswordHash,
                    IsActive = true
                };

                // Tạo Khách hàng
                var customerUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "khachhang@gmail.com",
                    FullName = "Khách Hàng May Mắn",
                    PasswordHash = sharedPasswordHash,
                    IsActive = true
                };

                context.Users.AddRange(adminUser, staffUser, customerUser);

                // Gán quyền cho các tài khoản
                if (adminRole != null) context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
                if (staffRole != null) context.UserRoles.Add(new UserRole { UserId = staffUser.Id, RoleId = staffRole.Id });
                if (customerRole != null) context.UserRoles.Add(new UserRole { UserId = customerUser.Id, RoleId = customerRole.Id });

                // Khởi tạo điểm thưởng và hạng thành viên
                context.UserTiers.AddRange(
                    new UserTier { UserId = adminUser.Id, CurrentTier = "Diamond", TotalPoints = 9999, LastEvaluated = DateTime.UtcNow },
                    new UserTier { UserId = staffUser.Id, CurrentTier = "Gold", TotalPoints = 1000, LastEvaluated = DateTime.UtcNow },
                    new UserTier { UserId = customerUser.Id, CurrentTier = "Bronze", TotalPoints = 0, LastEvaluated = DateTime.UtcNow }
                );

                context.SaveChanges();
            }

            // 3. TẠO DANH MỤC (CATEGORIES)
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Điện thoại", IsActive = true },
                    new Category { Name = "Laptop", IsActive = true },
                    new Category { Name = "Máy tính bảng", IsActive = true },
                    new Category { Name = "Phụ kiện", IsActive = true }
                );
                context.SaveChanges();
            }

            // 4. TẠO SẢN PHẨM & BIẾN THỂ (PRODUCTS & VARIANTS)
            if (!context.Products.Any())
            {
                var phoneCat = context.Categories.FirstOrDefault(c => c.Name == "Điện thoại");
                var laptopCat = context.Categories.FirstOrDefault(c => c.Name == "Laptop");
                var vendorId = Guid.NewGuid();

                // Sản phẩm 1: iPhone
                var iphone = new Product
                {
                    Id = Guid.NewGuid(),
                    CategoryId = phoneCat?.Id ?? 1,
                    VendorId = vendorId,
                    Name = "iPhone 15 Pro Max",
                    BasePrice = 30000000,
                    Status = "Active",
                    AverageRating = 5.0,
                    TotalReviews = 100
                };

                // Sản phẩm 2: MacBook
                var macbook = new Product
                {
                    Id = Guid.NewGuid(),
                    CategoryId = laptopCat?.Id ?? 2,
                    VendorId = vendorId,
                    Name = "MacBook Air M2",
                    BasePrice = 25000000,
                    Status = "Active",
                    AverageRating = 4.9,
                    TotalReviews = 50
                };

                context.Products.AddRange(iphone, macbook);

                // Thêm biến thể cho sản phẩm
                context.ProductVariants.AddRange(
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = iphone.Id, SKU_Code = "IP15-TITAN-256", Color = "Titan Tự Nhiên", Size = "256GB", Price = 30000000, StockQuantity = 50 },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = iphone.Id, SKU_Code = "IP15-BLUE-512", Color = "Xanh Dương", Size = "512GB", Price = 34000000, StockQuantity = 20 },
                    new ProductVariant { Id = Guid.NewGuid(), ProductId = macbook.Id, SKU_Code = "MBA-M2-SILVER", Color = "Bạc", Size = "8GB/256GB", Price = 25000000, StockQuantity = 30 }
                );

                context.SaveChanges();
            }

            // 5. TẠO MÃ GIẢM GIÁ (VOUCHERS)
            if (!context.Vouchers.Any())
            {
                context.Vouchers.AddRange(
                    new Voucher
                    {
                        Id = Guid.NewGuid(),
                        Code = "GIAM50K",
                        DiscountType = "FixedAmount",
                        DiscountValue = 50000,
                        MinOrderValue = 200000,
                        MaxDiscount = 50000,
                        MaxUses = 100,
                        UsedCount = 0,
                        ExpiryDate = DateTime.UtcNow.AddMonths(1)
                    },
                    new Voucher
                    {
                        Id = Guid.NewGuid(),
                        Code = "SieuSale10",
                        DiscountType = "Percentage",
                        DiscountValue = 10,
                        MinOrderValue = 1000000,
                        MaxDiscount = 500000,
                        MaxUses = 50,
                        UsedCount = 0,
                        ExpiryDate = DateTime.UtcNow.AddDays(7)
                    }
                );
                context.SaveChanges();
            }
        }
    }
}