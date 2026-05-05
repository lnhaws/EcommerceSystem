namespace EcommerceAPI.DTOs
{
    // DTO Khách hàng gửi lên để thêm vào giỏ
    public class CartItemAddDto
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
    }

    // DTO Trả về chi tiết 1 món hàng trong giỏ
    public class CartItemResponseDto
    {
        public Guid VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU_Code { get; set; } = string.Empty;
        public string? Color { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity; // Tự động tính tiền món này
    }

    // DTO Trả về toàn bộ Giỏ hàng
    public class CartResponseDto
    {
        public Guid CartId { get; set; }
        public List<CartItemResponseDto> Items { get; set; } = new List<CartItemResponseDto>();
        public decimal CartTotal => Items.Sum(i => i.TotalPrice); // Tự động tính Tổng tiền cả giỏ
    }
}