using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization; // Thêm dòng này

namespace EcommerceAPI.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
        {
            // Dùng .Select() để map trực tiếp từ Product sang ProductResponseDto
            return await _context.Products
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    AverageRating = p.AverageRating,
                    Status = p.Status,
                    CategoryId = p.CategoryId
                })
                .ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(Guid id)
        {
            var product = await _context.Products
                .Where(p => p.Id == id)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    AverageRating = p.AverageRating,
                    Status = p.Status,
                    CategoryId = p.CategoryId
                })
                .FirstOrDefaultAsync();

            if (product == null) return NotFound();

            return product;
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProduct(Guid id, ProductUpdateDto dto)
        {
            // 1. Tìm sản phẩm cũ trong DB
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // 2. Chỉ cập nhật những trường được phép
            product.Name = dto.Name;
            product.BasePrice = dto.BasePrice;
            product.CategoryId = dto.CategoryId;
            product.Status = dto.Status;

            // Hệ thống sẽ tự động cập nhật UpdatedAt nhờ đoạn code ở AppDbContext

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // POST: api/Products
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductResponseDto>> PostProduct(ProductCreateDto dto)
        {
            // 1. Map từ DTO sang Entity thực tế
            var product = new Product
            {
                Id = Guid.NewGuid(),
                VendorId = dto.VendorId,
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                BasePrice = dto.BasePrice,
                Status = "Active", // Mặc định khi tạo mới
                AverageRating = 0,
                TotalReviews = 0
                // CreatedAt sẽ tự động sinh nhờ AppDbContext
            };

            // 2. Lưu vào DB
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // 3. Trả về DTO
            var responseDto = new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                BasePrice = product.BasePrice,
                AverageRating = product.AverageRating,
                Status = product.Status,
                CategoryId = product.CategoryId
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, responseDto);
        }

        // POST: api/Products/bulk
        // Thêm nhiều sản phẩm cùng lúc
        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> PostProductsBulk(IEnumerable<ProductCreateDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest("Danh sách sản phẩm trống.");
            }

            // 1. Chuyển đổi toàn bộ DTO thành Entity thực thể
            var products = dtos.Select(dto => new Product
            {
                Id = Guid.NewGuid(),
                VendorId = dto.VendorId,
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                BasePrice = dto.BasePrice,
                Status = "Active",
                AverageRating = 0,
                TotalReviews = 0
            }).ToList();

            // 2. Dùng AddRange để thêm toàn bộ vào bộ nhớ tạm của EF Core
            _context.Products.AddRange(products);

            // 3. Lưu tất cả xuống Database trong 1 lần duy nhất (Rất tối ưu)
            await _context.SaveChangesAsync();

            // 4. Map ngược lại ra DTO để trả về cho người dùng xem
            var responseDtos = products.Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                BasePrice = p.BasePrice,
                AverageRating = p.AverageRating,
                Status = p.Status,
                CategoryId = p.CategoryId
            });

            return Ok(responseDtos);
        }

        // DELETE: api/Products/5 (Hàm này giữ nguyên)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(Guid id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        // DELETE: api/Products/bulk
        // Xóa nhiều sản phẩm cùng lúc
        [HttpDelete("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductsBulk([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest("Vui lòng cung cấp danh sách ID cần xóa.");
            }

            // 1. Tìm tất cả các sản phẩm có Id nằm trong danh sách gửi lên
            var productsToDelete = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            if (!productsToDelete.Any())
            {
                return NotFound("Không tìm thấy sản phẩm nào khớp với danh sách ID.");
            }

            // 2. Dùng RemoveRange để xóa toàn bộ
            _context.Products.RemoveRange(productsToDelete);

            // 3. Thực thi xuống Database
            await _context.SaveChangesAsync();

            return NoContent(); // Trả về 204 Báo hiệu xóa thành công
        }
    }
}