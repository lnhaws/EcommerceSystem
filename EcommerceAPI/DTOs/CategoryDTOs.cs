namespace EcommerceAPI.DTOs;

// 1. Trả về cho client xem
public class CategoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; } // Null nếu là danh mục gốc
    public bool IsActive { get; set; }
}

// 2. Client gửi lên để Thêm mới
public class CategoryCreateDto
{
    public required string Name { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
}

// 3. Client gửi lên để Cập nhật
public class CategoryUpdateDto
{
    public required string Name { get; set; }
    public int? ParentId { get; set; }
    public bool IsActive { get; set; }
}

public class CategoryStatusDto
{
    public bool IsActive { get; set; }
}