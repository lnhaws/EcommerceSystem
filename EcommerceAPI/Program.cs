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
var jwtSecret = builder.Configuration["JwtConfig:Secret"];

// Thêm dòng kiểm tra này để nếu appsettings.json bị lỗi, app sẽ báo ngay lúc chạy
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new Exception("LỖI NGHIÊM TRỌNG: Không tìm thấy JwtConfig:Secret trong file appsettings.json!");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.UseSecurityTokenValidators = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,   // Trong thực tế đưa lên host, bạn nên đổi thành true
        ValidateAudience = false, // Trong thực tế đưa lên host, bạn nên đổi thành true
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Không cho phép độ trễ thời gian khi token hết hạn
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CẤU HÌNH CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// =========================================================
// 2. CẤU HÌNH SWAGGER
// =========================================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce API", Version = "v1" });

    // Đổi Type thành Http để Swagger TỰ ĐỘNG dán chữ "Bearer " cho bạn
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "CHỈ CẦN DÁN TOKEN VÀO Ô BÊN DƯỚI (Tuyệt đối không copy dấu ngoặc kép, không cần gõ chữ Bearer).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

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
            }
            ,Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- BẮT ĐẦU: TỰ ĐỘNG CẬP NHẬT DATABASE KHI CHẠY ---
// Lưu ý: Khi đưa lên Somee, nếu User không có quyền CREATE TABLE, đoạn này có thể gây lỗi. 
// Hiện tại mình vẫn giữ nguyên theo ý bạn, vì bạn đã chạy Script trực tiếp rồi nên đoạn này sẽ bị bỏ qua an toàn.
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

// =========================================================
// 3. CẤU HÌNH MIDDLEWARE & TÍCH HỢP GIAO DIỆN REACT
// =========================================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API V1");
    // Không dùng c.RoutePrefix = string.Empty nữa vì đường dẫn gốc đã nhường cho giao diện React
});

app.UseHttpsRedirection();

// ---> 2 DÒNG QUAN TRỌNG ĐỂ C# ĐỌC ĐƯỢC GIAO DIỆN REACT <---
app.UseDefaultFiles(); // Tự động lấy file index.html làm trang chủ
app.UseStaticFiles();  // Cho phép lấy các file .js, .css trong wwwroot

app.UseRouting();
app.UseCors("AllowAll");

// KÍCH HOẠT ĐƯỜNG ỐNG BẢO MẬT (Thứ tự cực kỳ quan trọng)
app.UseAuthentication(); // 1. Hỏi giấy thông hành (Token) TRƯỚC
app.UseAuthorization();  // 2. Xét quyền hạn (Role) SAU

app.MapControllers();

// ---> DÒNG QUAN TRỌNG ĐỂ ĐIỀU HƯỚNG REACT ROUTER BỀN BỈ <---
app.MapFallbackToFile("/index.html");

app.Run();