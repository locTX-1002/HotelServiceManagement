using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Promotions;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly HotelDbContext _context;

        public PromotionService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<AuthServiceResult<IReadOnlyList<PromotionResponse>>> GetAllAsync()
        {
            var promotions = await _context.Promotions
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<PromotionResponse>>.Success(promotions.Select(ToResponse).ToList());
        }

        public async Task<AuthServiceResult<PromotionResponse>> GetByIdAsync(int id)
        {
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
            return promotion == null
                ? AuthServiceResult<PromotionResponse>.Failure("Promotion not found.", 404)
                : AuthServiceResult<PromotionResponse>.Success(ToResponse(promotion));
        }

        public async Task<AuthServiceResult<PromotionResponse>> CreateAsync(CreatePromotionRequest request)
        {
            var validationMessage = Validate(request.Code, request.Type, request.Value, request.StartDate, request.EndDate, out var type);
            if (validationMessage != null)
            {
                return AuthServiceResult<PromotionResponse>.Failure(validationMessage);
            }

            var code = request.Code.Trim().ToUpperInvariant();
            var exists = await _context.Promotions.AnyAsync(p => p.Code == code);
            if (exists)
            {
                return AuthServiceResult<PromotionResponse>.Failure("Promotion code already exists.", 409);
            }

            var promotion = new Promotion
            {
                Code = code,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Type = type,
                Value = request.Value,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            return AuthServiceResult<PromotionResponse>.Success(ToResponse(promotion), "Promotion created successfully.");
        }

        public async Task<AuthServiceResult<PromotionResponse>> UpdateAsync(int id, UpdatePromotionRequest request)
        {
            var validationMessage = Validate(request.Code, request.Type, request.Value, request.StartDate, request.EndDate, out var type);
            if (validationMessage != null)
            {
                return AuthServiceResult<PromotionResponse>.Failure(validationMessage);
            }

            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
            if (promotion == null)
            {
                return AuthServiceResult<PromotionResponse>.Failure("Promotion not found.", 404);
            }

            var code = request.Code.Trim().ToUpperInvariant();
            var exists = await _context.Promotions.AnyAsync(p => p.Id != id && p.Code == code);
            if (exists)
            {
                return AuthServiceResult<PromotionResponse>.Failure("Promotion code already exists.", 409);
            }

            promotion.Code = code;
            promotion.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            promotion.Type = type;
            promotion.Value = request.Value;
            promotion.StartDate = request.StartDate;
            promotion.EndDate = request.EndDate;
            promotion.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return AuthServiceResult<PromotionResponse>.Success(ToResponse(promotion), "Promotion updated successfully.");
        }

        private static PromotionResponse ToResponse(Promotion promotion)
        {
            return new PromotionResponse
            {
                Id = promotion.Id,
                Code = promotion.Code,
                Description = promotion.Description,
                Type = promotion.Type.ToString(),
                Value = promotion.Value,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive
            };
        }

        private static string? Validate(
            string code,
            string type,
            decimal value,
            DateTime startDate,
            DateTime endDate,
            out PromotionType parsedType)
        {
            parsedType = PromotionType.Percentage;

            if (string.IsNullOrWhiteSpace(code))
            {
                return "Code is required.";
            }

            if (!Enum.TryParse(type, ignoreCase: true, out parsedType))
            {
                return "Type must be Percentage or FixedAmount.";
            }

            if (value <= 0)
            {
                return "Value must be greater than 0.";
            }

            if (parsedType == PromotionType.Percentage && value > 100)
            {
                return "Value cannot exceed 100 when Type is Percentage.";
            }

            return endDate.Date < startDate.Date
                ? "EndDate must be later than or equal to StartDate."
                : null;
        }
    }
}
