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
    public class VouchersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VouchersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Vouchers
        // Lấy danh sách mã giảm giá
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VoucherResponseDto>>> GetVouchers()
        {
            return await _context.Vouchers
                .Select(v => new VoucherResponseDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    DiscountType = v.DiscountType,
                    DiscountValue = v.DiscountValue,
                    MinOrderValue = v.MinOrderValue,
                    MaxDiscount = v.MaxDiscount,
                    MaxUses = v.MaxUses,
                    UsedCount = v.UsedCount,
                    ExpiryDate = v.ExpiryDate,
                })
                .ToListAsync();
        }

        // POST: api/Vouchers/validate
        // 👉 Khách hàng nhập mã ở Giỏ hàng
        [HttpPost("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateVoucher([FromBody] VoucherValidateRequestDto request)
        {
            // 1. Tìm Voucher trong hệ thống (Không phân biệt hoa thường)
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code.ToLower() == request.Code.ToLower());

            if (voucher == null)
                return NotFound("Mã giảm giá không tồn tại.");

            // 2. Kiểm tra hạn sử dụng và số lượng
            if (voucher.ExpiryDate < DateTime.UtcNow)
                return BadRequest("Mã giảm giá đã hết hạn.");

            if (voucher.MaxUses > 0 && voucher.UsedCount >= voucher.MaxUses)
                return BadRequest("Mã giảm giá đã hết lượt sử dụng.");

            // 3. Kiểm tra điều kiện đơn hàng tối thiểu
            if (request.OrderTotal < voucher.MinOrderValue)
            {
                return BadRequest($"Đơn hàng chưa đạt mức tối thiểu. Cần mua thêm {voucher.MinOrderValue - request.OrderTotal:N0}đ để sử dụng mã này.");
            }

            // 4. Kiểm tra User này đã dùng mã này bao giờ chưa
            bool isUserAlreadyUsed = await _context.UserVouchers
                .AnyAsync(uv => uv.VoucherId == voucher.Id && uv.UserId == request.UserId && uv.IsUsed == true);

            if (isUserAlreadyUsed)
            {
                return BadRequest("Bạn đã sử dụng mã giảm giá này rồi.");
            }

            // 5. Tính toán số tiền được giảm thực tế
            decimal actualDiscount = 0;

            if (voucher.DiscountType == "Percentage")
            {
                // Giảm theo % của tổng đơn
                actualDiscount = request.OrderTotal * (voucher.DiscountValue / 100);

                // Cắt phần vượt trần (MaxDiscount)
                if (voucher.MaxDiscount > 0 && actualDiscount > voucher.MaxDiscount)
                {
                    actualDiscount = voucher.MaxDiscount;
                }
            }
            else // "FixedAmount" - Giảm tiền mặt
            {
                actualDiscount = voucher.DiscountValue;
            }

            // Đảm bảo không giảm lố âm tiền đơn hàng
            if (actualDiscount > request.OrderTotal)
            {
                actualDiscount = request.OrderTotal;
            }

            return Ok(new
            {
                Message = "Áp dụng mã thành công!",
                VoucherId = voucher.Id,
                DiscountApplied = actualDiscount,
                FinalTotal = request.OrderTotal - actualDiscount
            });
        }

        // POST: api/Vouchers
        // Tạo mã giảm giá mới
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<VoucherResponseDto>> PostVoucher(VoucherCreateDto dto)
        {
            // Kiểm tra trùng lặp Mã Code
            bool isCodeExist = await _context.Vouchers.AnyAsync(v => v.Code.ToLower() == dto.Code.ToLower());
            if (isCodeExist)
            {
                return BadRequest("Mã Code này đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
            }

            var voucher = new Voucher
            {
                Id = Guid.NewGuid(),
                Code = dto.Code.ToUpper(), // Chuẩn hóa viết hoa toàn bộ
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinOrderValue = dto.MinOrderValue,
                MaxDiscount = dto.MaxDiscount,
                MaxUses = dto.MaxUses,
                UsedCount = 0, // Mặc định lúc tạo mới chưa ai dùng
                ExpiryDate = dto.ExpiryDate
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVouchers), new { id = voucher.Id }, voucher);
        }

        // DELETE: api/Vouchers/{id}
        // Chỉ cho phép xóa vĩnh viễn nếu chưa có khách hàng nào dùng
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDeleteVoucher(Guid id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            if (voucher.UsedCount > 0)
            {
                return BadRequest("Không thể xóa vĩnh viễn mã giảm giá đã có người sử dụng để bảo toàn lịch sử đơn hàng.");
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/Vouchers/{id}/toggle-status
        // [TÙY CHỌN] Nếu Model Voucher của bạn có thuộc tính bool IsActive thì bỏ comment hàm này
        /*
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleVoucherStatus(Guid id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            voucher.IsActive = !voucher.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Mã giảm giá {voucher.Code} đã {(voucher.IsActive ? "kích hoạt" : "bị khóa")}." });
        }
        */
    }
}