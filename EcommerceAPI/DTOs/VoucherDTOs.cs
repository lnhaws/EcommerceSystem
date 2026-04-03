namespace EcommerceAPI.DTOs
{
    // DTO trả về danh sách cho Admin xem
    public class VoucherResponseDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal MaxDiscount { get; set; }
        public int MaxUses { get; set; }
        public int UsedCount { get; set; }
        public DateTime ExpiryDate { get; set; }
        // Thuộc tính IsActive giả định bạn đã thêm vào Model hoặc AuditableEntity
        public bool IsActive { get; set; }
    }

    // DTO để Admin tạo mã mới
    public class VoucherCreateDto
    {
        public required string Code { get; set; }
        public string DiscountType { get; set; } = "Percentage"; // "Percentage" hoặc "FixedAmount"
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal MaxDiscount { get; set; }
        public int MaxUses { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // DTO dùng khi khách hàng ấn nút "Áp dụng mã" trong giỏ hàng
    public class VoucherValidateRequestDto
    {
        public required string Code { get; set; }
        public Guid UserId { get; set; }
        public decimal OrderTotal { get; set; }
    }
}