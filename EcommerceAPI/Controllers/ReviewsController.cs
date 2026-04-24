using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.DTOs;

namespace EcommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Reviews/product/{productId}
        // Cho phép xem đánh giá công khai (Không cần Token)
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetProductReviews(Guid productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Join(_context.Users,
                      review => review.UserId,
                      user => user.Id,
                      (review, user) => new ReviewResponseDto
                      {
                          Id = review.Id,
                          Rating = review.Rating,
                          Comment = review.Comment,
                          MediaUrls = review.MediaUrls,
                          VendorReply = review.VendorReply,
                          CreatedAt = review.CreatedAt,
                          ReviewerName = user.FullName // Nối bảng để lấy tên người dùng
                      })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        // POST: api/Reviews
        // CHỈ USER ĐĂNG NHẬP MỚI ĐƯỢC ĐÁNH GIÁ
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostReview([FromBody] ReviewCreateDto dto)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Điểm đánh giá phải từ 1 đến 5 sao.");

            // 1. Lấy ID của User đang đăng nhập từ Token
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            var userId = Guid.Parse(userIdString);

            // 2. Kiểm tra xem User này có thực sự sở hữu Đơn hàng này không
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.UserId == userId);
            if (order == null) return BadRequest("Đơn hàng không tồn tại hoặc không thuộc về bạn.");

            // 3. Kiểm tra xem trong Đơn hàng đó có chứa Sản phẩm này không
            var hasPurchased = await _context.OrderItems.AnyAsync(oi => oi.OrderId == dto.OrderId && oi.VariantId == dto.VariantId);
            if (!hasPurchased) return BadRequest("Bạn không thể đánh giá sản phẩm chưa từng mua trong đơn hàng này.");

            // 4. Kiểm tra xem đã đánh giá chưa (Tránh spam 1 món hàng đánh giá nhiều lần)
            var alreadyReviewed = await _context.Reviews.AnyAsync(r => r.OrderId == dto.OrderId && r.VariantId == dto.VariantId);
            if (alreadyReviewed) return BadRequest("Bạn đã đánh giá sản phẩm này trong đơn hàng này rồi.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // TẠO ĐÁNH GIÁ
                var review = new Review
                {
                    Id = Guid.NewGuid(),
                    ProductId = dto.ProductId,
                    VariantId = dto.VariantId,
                    OrderId = dto.OrderId,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    MediaUrls = dto.MediaUrls
                };
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // CẬP NHẬT ĐIỂM TRUNG BÌNH CỦA SẢN PHẨM
                await UpdateProductRating(dto.ProductId);

                await transaction.CommitAsync();
                return Ok(new { Message = "Cảm ơn bạn đã đánh giá sản phẩm!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }

        // PATCH: api/Reviews/{id}/reply
        // CHỈ ADMIN MỚI ĐƯỢC TRẢ LỜI ĐÁNH GIÁ
        [HttpPatch("{id}/reply")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReplyReview(Guid id, [FromBody] ReviewReplyDto dto)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound("Không tìm thấy đánh giá.");

            review.VendorReply = dto.ReplyMessage;
            review.UpdatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã trả lời đánh giá của khách hàng." });
        }

        // DELETE: api/Reviews/{id}
        // CHỈ ADMIN (HOẶC CHÍNH NGƯỜI VIẾT) MỚI ĐƯỢC XÓA
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            // Nếu không phải Admin và cũng không phải người viết review -> Cấm xóa
            if (!isAdmin && review.UserId.ToString() != userId)
            {
                return Forbid("Bạn không có quyền xóa đánh giá của người khác.");
            }

            var productId = review.ProductId; // Lưu lại để tính điểm

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Tính lại điểm sau khi xóa
            await UpdateProductRating(productId);

            return Ok(new { Message = "Đã xóa đánh giá thành công." });
        }

        // --- HÀM HỖ TRỢ: TÍNH TOÁN LẠI ĐIỂM SẢN PHẨM ---
        private async Task UpdateProductRating(Guid productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                var allReviews = await _context.Reviews.Where(r => r.ProductId == productId).ToListAsync();

                product.TotalReviews = allReviews.Count;
                product.AverageRating = allReviews.Any() ? Math.Round(allReviews.Average(r => r.Rating), 1) : 0;

                _context.Entry(product).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }
    }
}