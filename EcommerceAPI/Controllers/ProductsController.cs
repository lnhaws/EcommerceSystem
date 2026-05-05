using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace EcommerceAPI.Controllers
{
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
            return await _context.Products
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    AverageRating = p.AverageRating,
                    Status = p.Status,
                    CategoryId = p.CategoryId,

                    // Lấy Tên danh mục từ bảng Categories
                    CategoryName = _context.Categories
                        .Where(c => c.Id == p.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault() ?? "Không xác định",

                    // Lấy Ảnh chính (IsMain = true) của sản phẩm từ bảng ProductImages
                    ImageUrl = _context.ProductImages
                        .Where(i => i.ProductId == p.Id && i.IsMain)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
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
                    CategoryId = p.CategoryId,

                    // Bổ sung lấy Tên danh mục
                    CategoryName = _context.Categories
                        .Where(c => c.Id == p.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault() ?? "Không xác định",

                    // Bổ sung lấy Ảnh
                    ImageUrl = _context.ProductImages
                        .Where(i => i.ProductId == p.Id && i.IsMain)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
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
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = dto.Name;
            product.BasePrice = dto.BasePrice;
            product.CategoryId = dto.CategoryId;
            product.Status = dto.Status;

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
            // 1. Tạo Sản phẩm mới
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
            };

            _context.Products.Add(product);

            // 2. NẾU CÓ LINK ẢNH TỪ FRONTEND GỬI LÊN -> TẠO THÊM RECORD ẢNH
            if (!string.IsNullOrEmpty(dto.ImageUrl))
            {
                _context.ProductImages.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ImageUrl = dto.ImageUrl,
                    IsMain = true, // Đánh dấu đây là ảnh đại diện
                    DisplayOrder = 1
                });
            }

            await _context.SaveChangesAsync();

            // 3. Trả về DTO
            var responseDto = new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                BasePrice = product.BasePrice,
                AverageRating = product.AverageRating,
                Status = product.Status,
                CategoryId = product.CategoryId,
                ImageUrl = dto.ImageUrl // Trả về ảnh luôn để Frontend cập nhật ngay
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, responseDto);
        }

        // POST: api/Products/bulk
        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> PostProductsBulk(IEnumerable<ProductCreateDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest("Danh sách sản phẩm trống.");
            }

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

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

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

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Lưu ý: Nếu xóa Product thì EF Core sẽ tự động xóa luôn các ProductImage liên quan
            // (Nếu bạn đã setup Cascade Delete trong Database)
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(Guid id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // POST: api/Products/delete-bulk
        [HttpPost("delete-bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductsBulk([FromBody] List<Guid> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest("Vui lòng cung cấp danh sách ID cần xóa.");
            }

            var productsToDelete = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            if (!productsToDelete.Any())
            {
                return NotFound("Không tìm thấy sản phẩm nào khớp với danh sách ID.");
            }

            _context.Products.RemoveRange(productsToDelete);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}