namespace EcommerceAPI.DTOs
{
    // Mô tả món hàng khách chọn mua
    public class OrderItemRequestDto
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
    }

    // Gói dữ liệu tổng thể khi khách bấm Đặt hàng
    public class OrderCreateDto
    {
        public Guid UserId { get; set; }
        public Guid ShippingAddressId { get; set; } // Liên kết bảng UserAddress
        public string PaymentMethod { get; set; } = "COD"; // Liên kết tạo PaymentTransaction
        public string? VoucherCode { get; set; }

        public required List<OrderItemRequestDto> Items { get; set; }
    }
}