using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EcommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải đăng nhập mới có giỏ hàng
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy ID của User đang đăng nhập từ Token
        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdString!);
        }

        // 1. LẤY THÔNG TIN GIỎ HÀNG HIỆN TẠI
        [HttpGet]
        public async Task<ActionResult<CartResponseDto>> GetMyCart()
        {
            var userId = GetCurrentUserId();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
            {
                return Ok(new CartResponseDto { CartId = cart?.Id ?? Guid.Empty }); // Trả về giỏ trống
            }

            // Join bảng để lấy thông tin Tên sản phẩm, Giá tiền từ VariantId
            var response = new CartResponseDto { CartId = cart.Id };

            foreach (var item in cart.Items)
            {
                var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                if (variant != null)
                {
                    var product = await _context.Products.FindAsync(variant.ProductId);
                    response.Items.Add(new CartItemResponseDto
                    {
                        VariantId = variant.Id,
                        ProductName = product?.Name ?? "Sản phẩm không xác định",
                        SKU_Code = variant.SKU_Code,
                        Color = variant.Color,
                        UnitPrice = variant.Price, // Hoặc lấy từ product.BasePrice tùy logic kinh doanh
                        Quantity = item.Quantity
                    });
                }
            }

            return Ok(response);
        }

        // 2. THÊM / CẬP NHẬT SẢN PHẨM VÀO GIỎ
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] CartItemAddDto dto)
        {
            if (dto.Quantity <= 0) return BadRequest("Số lượng phải lớn hơn 0.");

            var userId = GetCurrentUserId();

            // Kiểm tra biến thể có tồn tại và đủ hàng không
            var variant = await _context.ProductVariants.FindAsync(dto.VariantId);
            if (variant == null) return NotFound("Sản phẩm không tồn tại.");
            if (variant.StockQuantity < dto.Quantity) return BadRequest($"Kho chỉ còn {variant.StockQuantity} sản phẩm.");

            // Tìm giỏ hàng, nếu chưa có thì tạo mới
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { Id = Guid.NewGuid(), UserId = userId };
                _context.Carts.Add(cart);
            }

            // Kiểm tra món hàng đã có trong giỏ chưa
            var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == dto.VariantId);
            if (existingItem != null)
            {
                // Có rồi thì cộng dồn số lượng
                if (variant.StockQuantity < (existingItem.Quantity + dto.Quantity))
                    return BadRequest("Vượt quá số lượng tồn kho cho phép.");

                existingItem.Quantity += dto.Quantity;
                _context.Entry(existingItem).State = EntityState.Modified;
            }
            else
            {
                // Chưa có thì thêm mới
                cart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã thêm vào giỏ hàng." });
        }

        // 3. XÓA MỘT MÓN KHỎI GIỎ
        [HttpDelete("remove/{variantId}")]
        public async Task<IActionResult> RemoveFromCart(Guid variantId)
        {
            var userId = GetCurrentUserId();
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return NotFound("Giỏ hàng trống.");

            var itemToRemove = cart.Items.FirstOrDefault(i => i.VariantId == variantId);
            if (itemToRemove == null) return NotFound("Không tìm thấy sản phẩm trong giỏ.");

            _context.CartItems.Remove(itemToRemove);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã xóa sản phẩm khỏi giỏ hàng." });
        }
    }
}