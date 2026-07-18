using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Application.Services;
using HotelServiceManagement.Infrastructure.Data;
using HotelServiceManagement.Infrastructure.Repositories;
using HotelServiceManagement.Infrastructure.Security;
using HotelServiceManagement.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 1. Configure EF Core SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")))
    .SetApplicationName("HotelServiceManagement");

// 2. Configure Dependency Injection
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGuestAuthService, GuestAuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStayService, StayService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IGuestService, GuestService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IServiceCategoryService, ServiceCategoryService>();
builder.Services.AddScoped<IServiceItemService, ServiceItemService>();
builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ISurchargeItemService, SurchargeItemService>();

// 3. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? "PLEASE_CHANGE_THIS_SECRET_KEY_WITH_AT_LEAST_32_CHARS";
var issuer = jwtSettings["Issuer"] ?? "HotelServiceManagement";
var audience = jwtSettings["Audience"] ?? "HotelServiceManagementClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// DefaultPolicy ap dung cho MOI [Authorize]/[Authorize(Roles=...)] khong khai bao Policy rieng -
// tuc la toan bo API van hanh hien co (da phan lon chi dung [Authorize] tran, khong loc theo Role).
// Bat buoc claim token_scope=staff o day de token cua guest portal (JwtService.GenerateGuestAccessToken)
// khong the nao lot duoc vao bat ky endpoint nao trong so do, du controller nao quen loc Role.
// Nguoc lai, GuestOnly danh rieng cho GuestAuthController - token nhan vien khong co claim nay nen
// cung khong the goi duoc API cua khach.
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim(JwtService.TokenScopeClaimType, JwtService.StaffTokenScope)
        .Build();

    options.AddPolicy("GuestOnly", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(JwtService.TokenScopeClaimType, JwtService.GuestTokenScope));
});

// 4. Configure Controllers
builder.Services.AddControllers();

// Lỗi validate tự động từ [ApiController] ([Required], [Range]...) mặc định trả về ValidationProblemDetails,
// khác shape { message } mà mọi lỗi nghiệp vụ khác dùng (AuthServiceResult) - khiến FE apiError() không đọc
// được message cụ thể, rơi về text chung chung. Đưa về cùng shape để lỗi nào cũng hiện rõ ràng trên FE.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var firstError = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Dữ liệu không hợp lệ.";
        return new BadRequestObjectResult(new { message = firstError });
    };
});

// 5. Configure Swagger with JWT Bearer support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hotel & Service Management System API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token only",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// 6. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Service API v1");
    });

    // Máy dev tự tạo/migrate database khi chạy, không cần lệnh tay
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<HotelDbContext>().Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok", timeUtc = DateTime.UtcNow }));

app.Run();
