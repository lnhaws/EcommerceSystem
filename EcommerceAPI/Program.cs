using Microsoft.EntityFrameworkCore;
using EcommerceAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================================================
// 1. CẤU HÌNH BẢO MẬT JWT (Lấy chìa khóa từ appsettings.json)
// =========================================================
var jwtSecret = builder.Configuration.GetSection("JwtConfig:Secret").Value;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
        ValidateIssuer = false,   // Trong thực tế đưa lên host, bạn nên đổi thành true
        ValidateAudience = false, // Trong thực tế đưa lên host, bạn nên đổi thành true
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Không cho phép độ trễ thời gian khi token hết hạn
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// =========================================================
// 2. CẤU HÌNH SWAGGER (Thêm ổ khóa Authorize góc phải)
// =========================================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce API", Version = "v1" });

    // Định nghĩa form nhập Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập Token vào đây. Chú ý: Gõ chữ 'Bearer ' ở trước khoảng trắng rồi mới dán token vào! Ví dụ: Bearer eyJhb...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Ép Swagger phải gửi Token vào Header của mỗi request
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- BẮT ĐẦU: TỰ ĐỘNG CẬP NHẬT DATABASE KHI CHẠY ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
        DbSeeder.Seed(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi khi tự động cập nhật Database.");
    }
}
// --- KẾT THÚC ---

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// =========================================================
// 3. KÍCH HOẠT ĐƯỜNG ỐNG BẢO MẬT (Thứ tự cực kỳ quan trọng)
// =========================================================
app.UseAuthentication(); // 1. Hỏi giấy thông hành (Token) TRƯỚC
app.UseAuthorization();  // 2. Xét quyền hạn (Role) SAU

app.MapControllers();
app.Run();