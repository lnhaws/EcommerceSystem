namespace EcommerceAPI.DTOs;

// 1. DTO dùng để trả dữ liệu cho client xem (Giấu các trường nhạy cảm)
public class ProductResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public double AverageRating { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}

// 2. DTO dùng để client gửi dữ liệu Thêm mới (Không cho phép gửi Id, Rating...)
public class ProductCreateDto
{
    public Guid VendorId { get; set; }
    public int CategoryId { get; set; }
    public required string Name { get; set; }
    public decimal BasePrice { get; set; }
}

// 3. DTO dùng để client Cập nhật (Chỉ cho phép sửa tên, giá, trạng thái)
public class ProductUpdateDto
{
    public int CategoryId { get; set; }
    public required string Name { get; set; }
    public decimal BasePrice { get; set; }
    public required string Status { get; set; }
}