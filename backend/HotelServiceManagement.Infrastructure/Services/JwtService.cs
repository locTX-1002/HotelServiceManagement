using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        // Dung chung o Program.cs de cau hinh DefaultPolicy/GuestOnly policy - giu public const de
        // khong phai go lai chuoi rai rac nhieu noi.
        public const string TokenScopeClaimType = "token_scope";
        public const string StaffTokenScope = "staff";
        public const string GuestTokenScope = "guest";

        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var keyString = jwtSettings["Key"] ?? "PLEASE_CHANGE_THIS_SECRET_KEY_WITH_AT_LEAST_32_CHARS";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? ""),
                new Claim(JwtService.TokenScopeClaimType, StaffTokenScope)
            };

            var expiryMinutes = int.Parse(jwtSettings["ExpireMinutes"] ?? "15");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "HotelServiceManagement",
                audience: jwtSettings["Audience"] ?? "HotelServiceManagementClient",
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public string GenerateToken(User user)
        {
            return GenerateAccessToken(user).Token;
        }

        // Token cua khach (guest portal) - luon mang claim token_scope=guest, KHONG bao gio co
        // ClaimTypes.Role khop voi role nhan vien nao. Program.cs bat DefaultPolicy yeu cau
        // token_scope=staff nen token nay tu dong bi tu choi tren toan bo API van hanh hien co
        // (da phan lon chi dung [Authorize] tran), khong can sua tung controller.
        public (string Token, DateTime ExpiresAt) GenerateGuestAccessToken(Guest guest)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var keyString = jwtSettings["Key"] ?? "PLEASE_CHANGE_THIS_SECRET_KEY_WITH_AT_LEAST_32_CHARS";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, guest.Id.ToString()),
                new Claim(ClaimTypes.Name, guest.FullName),
                new Claim(JwtService.TokenScopeClaimType, GuestTokenScope)
            };

            var expiryMinutes = int.Parse(jwtSettings["ExpireMinutes"] ?? "15");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "HotelServiceManagement",
                audience: jwtSettings["Audience"] ?? "HotelServiceManagementClient",
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
