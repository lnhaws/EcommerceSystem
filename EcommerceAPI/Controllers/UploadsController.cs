using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được quyền upload ảnh
    public class UploadsController : ControllerBase
    {
        // IWebHostEnvironment giúp lấy được đường dẫn vật lý của thư mục wwwroot
        private readonly IWebHostEnvironment _env;

        public UploadsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // POST: api/Uploads/image
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file nào được tải lên.");

            // 1. Kiểm tra đuôi file (Chỉ cho phép hình ảnh)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Chỉ cho phép tải lên file hình ảnh (.jpg, .png, .gif, .webp).");

            // 2. Kiểm tra dung lượng (Ví dụ: Giới hạn tối đa 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("Dung lượng file không được vượt quá 5MB.");

            // 3. Định tuyến nơi lưu file (Tạo thư mục wwwroot/uploads/products nếu chưa có)
            string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // 4. Đổi tên file (Dùng Guid để tên file không bao giờ bị trùng dù up cùng 1 ảnh)
            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 5. Lưu file vật lý xuống ổ cứng
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 6. Tạo đường link công khai để Frontend gọi ra
            // Request.Scheme = http hoặc https | Request.Host = localhost:7226
            var imageUrl = $"{Request.Scheme}://{Request.Host}/uploads/products/{uniqueFileName}";

            // Trả về Link ảnh
            return Ok(new
            {
                Message = "Tải ảnh lên thành công!",
                Url = imageUrl
            });
        }
    }
}