using HotelServiceManagement.Application.DTOs.Auth;
using HotelServiceManagement.Application.DTOs.Users;
using HotelServiceManagement.Application.Interfaces;
using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly HotelDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserManagementService(HotelDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthServiceResult<IReadOnlyList<UserResponse>>> GetAllAsync()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Id)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    RoleId = u.RoleId,
                    Role = u.Role.RoleName
                })
                .ToListAsync();

            return AuthServiceResult<IReadOnlyList<UserResponse>>.Success(users);
        }

        public async Task<AuthServiceResult<UserResponse>> GetByIdAsync(int id)
        {
            var user = await FindUserAsync(id);
            return user == null
                ? AuthServiceResult<UserResponse>.Failure("User not found.", 404)
                : AuthServiceResult<UserResponse>.Success(ToResponse(user));
        }

        public async Task<AuthServiceResult<UserResponse>> CreateAsync(CreateUserRequest request)
        {
            var validationMessage = ValidateCreateRequest(request);
            if (validationMessage != null)
            {
                return AuthServiceResult<UserResponse>.Failure(validationMessage);
            }

            var normalizedEmail = NormalizeEmail(request.Email);
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                return AuthServiceResult<UserResponse>.Failure("Email already exists.", 409);
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
            if (role == null)
            {
                return AuthServiceResult<UserResponse>.Failure("Role does not exist.");
            }

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                RoleId = role.Id,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            user.Role = role;

            return AuthServiceResult<UserResponse>.Success(ToResponse(user), "User created successfully.");
        }

        public async Task<AuthServiceResult<UserResponse>> UpdateAsync(int id, UpdateUserRequest request)
        {
            var validationMessage = ValidateUpdateRequest(request);
            if (validationMessage != null)
            {
                return AuthServiceResult<UserResponse>.Failure(validationMessage);
            }

            var user = await FindUserAsync(id);
            if (user == null)
            {
                return AuthServiceResult<UserResponse>.Failure("User not found.", 404);
            }

            var normalizedEmail = NormalizeEmail(request.Email);
            var emailExists = await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                return AuthServiceResult<UserResponse>.Failure("Email already exists.", 409);
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
            if (role == null)
            {
                return AuthServiceResult<UserResponse>.Failure("Role does not exist.");
            }

            user.FullName = request.FullName.Trim();
            user.Email = request.Email.Trim();
            user.RoleId = role.Id;

            await _context.SaveChangesAsync();
            user.Role = role;

            return AuthServiceResult<UserResponse>.Success(ToResponse(user), "User updated successfully.");
        }

        public async Task<AuthServiceResult<UserResponse>> UpdateStatusAsync(int id, UpdateUserStatusRequest request)
        {
            var user = await FindUserAsync(id);
            if (user == null)
            {
                return AuthServiceResult<UserResponse>.Failure("User not found.", 404);
            }

            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync();

            return AuthServiceResult<UserResponse>.Success(ToResponse(user), "User status updated successfully.");
        }

        public async Task<AuthServiceResult<AuthMessageResponse>> ResetPasswordAsync(int id, ResetUserPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return MessageFailure("NewPassword is required.");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return MessageFailure("ConfirmPassword must match NewPassword.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return MessageFailure("User not found.", 404);
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return MessageSuccess("Password reset successfully.");
        }

        private async Task<User?> FindUserAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        private static UserResponse ToResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                RoleId = user.RoleId,
                Role = user.Role?.RoleName ?? string.Empty
            };
        }

        private static string? ValidateCreateRequest(CreateUserRequest request)
        {
            if (request == null)
            {
                return "Request body is required.";
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return "FullName is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return "Email is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return "Password is required.";
            }

            if (request.Password != request.ConfirmPassword)
            {
                return "ConfirmPassword must match Password.";
            }

            return request.RoleId <= 0 ? "RoleId is required." : null;
        }

        private static string? ValidateUpdateRequest(UpdateUserRequest request)
        {
            if (request == null)
            {
                return "Request body is required.";
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return "FullName is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return "Email is required.";
            }

            return request.RoleId <= 0 ? "RoleId is required." : null;
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLower();
        }

        private static AuthServiceResult<AuthMessageResponse> MessageSuccess(string message)
        {
            return AuthServiceResult<AuthMessageResponse>.Success(new AuthMessageResponse { Message = message }, message);
        }

        private static AuthServiceResult<AuthMessageResponse> MessageFailure(string message, int statusCode = 400)
        {
            return AuthServiceResult<AuthMessageResponse>.Failure(message, statusCode);
        }
    }
}
