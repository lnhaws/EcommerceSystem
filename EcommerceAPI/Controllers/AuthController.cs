using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.DTOs;
using Microsoft.CodeAnalysis.Scripting;
namespace EcommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Tiêm IConfiguration để lấy "Chìa khóa bí mật" tạo Token từ appsettings.json
        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Users/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            // 1. Kiểm tra Email đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email này đã được sử dụng.");
            }

            // 2. Tạo User mới với mật khẩu ĐÃ ĐƯỢC MÃ HÓA (Hash)
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // Băm mật khẩu
                IsActive = true
            };

            _context.Users.Add(user);

            // 3. Gán Role mặc định là "Customer" cho người mới
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = customerRole.Id
                });
            }

            // 4. Khởi tạo điểm Loyalty (UserTier) mặc định
            _context.UserTiers.Add(new UserTier
            {
                UserId = user.Id,
                CurrentTier = "Bronze",
                TotalPoints = 0,
                LastEvaluated = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký tài khoản thành công!" });
        }

        // POST: api/Users/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            // 1. Tìm User theo Email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !user.IsActive)
            {
                return BadRequest("Sai email hoặc tài khoản đã bị khóa.");
            }

            // 2. Kiểm tra mật khẩu (So sánh pass gốc với pass đã Hash trong DB)
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Sai mật khẩu.");
            }

            // 3. Lấy danh sách Quyền (Roles) của User này
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            // 4. Tạo JWT Token
            string token = CreateToken(user, roles);

            // Trả về Token cho Frontend lưu lại
            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Roles = roles
            });
        }

        // HÀM BÍ MẬT: Chế tạo JWT Token
        private string CreateToken(User user, List<string> roles)
        {
            // Nhét thông tin (Claims) vào trong Token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            // Nhét Role vào Token để phân quyền
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Lấy chìa khóa bí mật từ file appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("JwtConfig:Secret").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), // Token có hạn 7 ngày
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        // GET: api/Users
        // Chỉ Admin mới được xem danh sách toàn bộ User
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
    }
}