using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace EcommerceAPI.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetCategories()
        {
            return await _context.Categories
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentId = c.ParentId,
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Where(c => c.Id == id)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentId = c.ParentId,
                    IsActive = c.IsActive
                })
                .FirstOrDefaultAsync();

            if (category == null) return NotFound();

            return category;
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, CategoryUpdateDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Ràng buộc logic: Một danh mục không thể lấy chính nó làm Parent
            if (dto.ParentId == id)
            {
                return BadRequest("Danh mục không thể là danh mục cha của chính nó.");
            }

            category.Name = dto.Name;
            category.ParentId = dto.ParentId;
            category.IsActive = dto.IsActive;

            // UpdatedAt sẽ tự động cập nhật nhờ AppDbContext

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<CategoryResponseDto>> PostCategory(CategoryCreateDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                ParentId = dto.ParentId,
                IsActive = dto.IsActive
                // CreatedAt sẽ tự động thêm
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var responseDto = new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                ParentId = category.ParentId,
                IsActive = category.IsActive
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, responseDto);
        }

        // 1. CHỨC NĂNG ẨN/HIỆN (SOFT-DELETE / RESTORE)
        // PATCH: api/Categories/{id}/toggle-status
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Nếu đang định ẨN (từ true sang false), phải kiểm tra sản phẩm
            if (category.IsActive)
            {
                bool hasActiveProducts = await _context.Products
                    .AnyAsync(p => p.CategoryId == id && p.Status == "Active");

                if (hasActiveProducts)
                {
                    return BadRequest("Không thể ẩn danh mục này vì vẫn còn sản phẩm đang hoạt động.");
                }
            }

            // Đảo ngược trạng thái hiện tại
            category.IsActive = !category.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Danh mục đã được {(category.IsActive ? "hiển thị" : "ẩn")}.", isActive = category.IsActive });
        }

        // 2. CHỨC NĂNG XÓA VĨNH VIỄN (HARD-DELETE)
        // DELETE: api/Categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> HardDeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Kiểm tra tuyệt đối: Dù là sản phẩm ẩn hay hiện, nếu còn liên kết thì không cho xóa vĩnh viễn
            // để tránh lỗi tham chiếu dữ liệu (Foreign Key Constraint)
            bool hasAnyProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasAnyProducts)
            {
                return BadRequest("Không thể xóa vĩnh viễn danh mục này vì đã có dữ liệu sản phẩm liên quan!");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}