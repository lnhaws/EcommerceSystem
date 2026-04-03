using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Data;
using EcommerceAPI.Models;
using EcommerceAPI.DTOs;

namespace EcommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] OrderCreateDto request)
        {
            if (request.Items == null || !request.Items.Any())
                return BadRequest("Giỏ hàng đang trống!");

            // Bắt đầu Transaction: Đảm bảo dữ liệu các bảng đồng bộ tuyệt đối
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. KIỂM TRA ĐỊA CHỈ GIAO HÀNG
                var addressExists = await _context.UserAddresses.AnyAsync(a => a.Id == request.ShippingAddressId && a.UserId == request.UserId);
                if (!addressExists)
                    return BadRequest("Địa chỉ giao hàng không hợp lệ.");

                decimal totalAmount = 0;
                var orderDetailsList = new List<OrderItem>();
                var variantsToUpdate = new List<ProductVariant>();

                // 2. DUYỆT QUA TỪNG SẢN PHẨM KHÁCH MUA
                foreach (var item in request.Items)
                {
                    // Lấy biến thể sản phẩm
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                    if (variant == null) return BadRequest($"Không tìm thấy sản phẩm có mã phân loại {item.VariantId}");

                    // Lấy thông tin gốc của sản phẩm (Tên, Giá gốc)
                    var product = await _context.Products.FindAsync(variant.ProductId);
                    if (product == null || product.Status != "Active")
                        return BadRequest("Sản phẩm đã ngừng kinh doanh.");

                    // Kiểm tra tồn kho
                    if (variant.StockQuantity < item.Quantity)
                        return BadRequest($"Sản phẩm '{product.Name}' không đủ số lượng trong kho. (Còn lại: {variant.StockQuantity})");

                    // Tính tiền (Sử dụng BasePrice là giá gốc chuẩn duy nhất của sản phẩm)
                    decimal unitPrice = product.BasePrice;
                    totalAmount += unitPrice * item.Quantity;

                    // Chuẩn bị dữ liệu OrderItem
                    orderDetailsList.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        VariantId = variant.Id,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice
                    });

                    // Trừ tồn kho tạm thời vào danh sách
                    variant.StockQuantity -= item.Quantity;
                    variantsToUpdate.Add(variant);
                }

                // 3. XỬ LÝ MÃ GIẢM GIÁ (Nếu có)
                decimal discountAmount = 0;
                if (!string.IsNullOrEmpty(request.VoucherCode))
                {
                    var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code.ToLower() == request.VoucherCode.ToLower());
                    if (voucher == null) return BadRequest("Mã giảm giá không tồn tại.");
                    if (voucher.ExpiryDate < DateTime.UtcNow) return BadRequest("Mã giảm giá đã hết hạn.");
                    if (voucher.MaxUses > 0 && voucher.UsedCount >= voucher.MaxUses) return BadRequest("Mã giảm giá đã hết lượt.");
                    if (totalAmount < voucher.MinOrderValue) return BadRequest($"Đơn hàng chưa đạt tối thiểu {voucher.MinOrderValue:N0}đ.");

                    bool isUsed = await _context.UserVouchers.AnyAsync(uv => uv.VoucherId == voucher.Id && uv.UserId == request.UserId && uv.IsUsed);
                    if (isUsed) return BadRequest("Bạn đã dùng mã này rồi.");

                    // Tính giảm giá
                    if (voucher.DiscountType == "Percentage")
                    {
                        discountAmount = totalAmount * (voucher.DiscountValue / 100);
                        if (voucher.MaxDiscount > 0 && discountAmount > voucher.MaxDiscount) discountAmount = voucher.MaxDiscount;
                    }
                    else
                    {
                        discountAmount = voucher.DiscountValue;
                    }

                    if (discountAmount > totalAmount) discountAmount = totalAmount;

                    // Cập nhật lượt dùng
                    voucher.UsedCount += 1;
                    _context.Entry(voucher).State = EntityState.Modified;

                    // Ghi nhận User đã dùng mã
                    _context.UserVouchers.Add(new UserVoucher
                    {
                        Id = Guid.NewGuid(),
                        UserId = request.UserId,
                        VoucherId = voucher.Id,
                        IsUsed = true,
                        UsedAt = DateTime.UtcNow
                    });
                }

                // 4. TẠO ĐƠN HÀNG (Bảng Order)
                decimal shippingFee = 30000; // Giả sử phí ship đồng giá 30k
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    ShippingAddressId = request.ShippingAddressId,
                    TotalAmount = totalAmount,
                    ShippingFee = shippingFee,
                    DiscountAmount = discountAmount,
                    FinalAmount = (totalAmount + shippingFee) - discountAmount,
                    Status = "Pending"
                };
                _context.Orders.Add(order);

                // Gắn OrderId cho các OrderItem và thêm vào DB
                foreach (var detail in orderDetailsList)
                {
                    detail.OrderId = order.Id;
                    _context.OrderItems.Add(detail);
                }

                // Cập nhật tồn kho vào DB
                foreach (var variant in variantsToUpdate)
                {
                    _context.Entry(variant).State = EntityState.Modified;
                }

                // 5. TẠO GIAO DỊCH THANH TOÁN (Bảng PaymentTransaction)
                var paymentTransaction = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    PaymentMethod = request.PaymentMethod,
                    Amount = order.FinalAmount,
                    Status = "Pending"
                };
                _context.PaymentTransactions.Add(paymentTransaction);

                // 6. LƯU TẤT CẢ VÀ COMMIT TRANSACTION
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "Đặt hàng thành công!",
                    OrderId = order.Id,
                    FinalAmount = order.FinalAmount
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Có lỗi xảy ra: " + ex.Message);
            }
        }
    }
}