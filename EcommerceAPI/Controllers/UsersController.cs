using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.DTOs;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.AspNetCore.Authorization;

namespace EcommerceAPI.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        // Lấy danh sách toàn bộ người dùng
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
        {
            return await _context.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    IsActive = u.IsActive
                }).ToListAsync();
        }

        // POST: api/Users
        // Chức năng: Admin thêm người dùng mới (Có thể cấp quyền Admin hoặc Staff)
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto dto)
        {
            // Kiểm tra trùng Email
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("Email này đã tồn tại trong hệ thống.");
            }

            // Kiểm tra Role có hợp lệ không
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == dto.RoleName.ToLower());
            if (role == null)
            {
                return BadRequest($"Quyền '{dto.RoleName}' không tồn tại trong hệ thống.");
            }

            // Tạo User
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsActive = true
            };

            _context.Users.Add(user);

            // Cấp quyền
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Đã tạo thành công tài khoản {dto.Email} với quyền {role.Name}." });
        }

        // PATCH: api/Users/{id}/toggle-lock
        // Chức năng: Khóa / Mở khóa tài khoản (Cấm đăng nhập)
        [HttpPatch("{id}/toggle-lock")]
        public async Task<IActionResult> ToggleLockUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // Đảo ngược trạng thái
            user.IsActive = !user.IsActive;

            // Nếu bạn có cột UpdatedBy trong AuditableEntity, bạn có thể lưu ID của Admin thao tác vào đây
            // user.UpdatedBy = "Admin ID..."; 

            await _context.SaveChangesAsync();

            string statusMessage = user.IsActive ? "Đã MỞ KHÓA" : "Đã KHÓA";
            return Ok(new { Message = $"Tài khoản {user.Email} {statusMessage} thành công." });
        }
    }
}