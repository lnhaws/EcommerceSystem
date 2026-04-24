namespace EcommerceAPI.DTOs
{
    // DTO khách hàng gửi lên để Đánh giá
    public class ReviewCreateDto
    {
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        public Guid OrderId { get; set; } // Bắt buộc phải có mã đơn hàng để chứng minh đã mua
        public int Rating { get; set; } // Từ 1 đến 5 sao
        public string? Comment { get; set; }
        public string? MediaUrls { get; set; }
    }

    // DTO trả về danh sách đánh giá cho người xem
    public class ReviewResponseDto
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? MediaUrls { get; set; }
        public string? VendorReply { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin người đánh giá (Chỉ lấy Tên, không lấy Email/Pass)
        public string ReviewerName { get; set; } = string.Empty;
    }

    // DTO cho Admin/Vendor trả lời đánh giá
    public class ReviewReplyDto
    {
        public required string ReplyMessage { get; set; }
    }
}