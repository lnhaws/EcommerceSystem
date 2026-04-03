namespace EcommerceAPI.DTOs
{
    // 1. DTO cho khách hàng Đăng ký
    public class UserRegisterDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
    }

    // 2. DTO cho khách hàng Đăng nhập
    public class UserLoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    // 3. DTO trả về sau khi Đăng nhập thành công (Chứa Token)
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }

    // 4. DTO trả về thông tin User (ẩn Password)
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    // DTO dành riêng cho Admin tạo tài khoản nhân viên / người dùng mới
    public class AdminCreateUserDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
        public required string RoleName { get; set; } // Admin được quyền chỉ định Role ngay lúc tạo
    }
}