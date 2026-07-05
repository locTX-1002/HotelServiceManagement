using HotelServiceManagement.Domain.Entities;
using HotelServiceManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelServiceManagement.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        context.Database.Migrate();

        if (!context.Roles.Any())
        {
            var roles = new[]
            {
                new Role { RoleName = "Admin", Description = "Quản trị hệ thống" },
                new Role { RoleName = "Manager", Description = "Xem dashboard và báo cáo" },
                new Role { RoleName = "Receptionist", Description = "Lễ tân: đặt phòng, check-in/out, hóa đơn" },
                new Role { RoleName = "ServiceStaff", Description = "Nhân viên dịch vụ: nhà hàng, giặt ủi" },
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();

            var password = BCrypt.Net.BCrypt.HashPassword("123456");
            context.Users.AddRange(
                new User { FullName = "Admin Demo", Email = "admin@hotel.com", PasswordHash = password, RoleId = roles[0].RoleId },
                new User { FullName = "Manager Demo", Email = "manager@hotel.com", PasswordHash = password, RoleId = roles[1].RoleId },
                new User { FullName = "Receptionist Demo", Email = "receptionist@hotel.com", PasswordHash = password, RoleId = roles[2].RoleId },
                new User { FullName = "Service Demo", Email = "service@hotel.com", PasswordHash = password, RoleId = roles[3].RoleId });
            context.SaveChanges();
        }

        if (!context.RoomTypes.Any())
        {
            var standard = new RoomType { TypeName = "Standard", Capacity = 2, BasePrice = 500_000m };
            var deluxe = new RoomType { TypeName = "Deluxe", Capacity = 2, BasePrice = 800_000m };
            var suite = new RoomType { TypeName = "Suite", Capacity = 4, BasePrice = 1_200_000m };
            var family = new RoomType { TypeName = "Family Room", Capacity = 6, BasePrice = 1_500_000m };
            context.RoomTypes.AddRange(standard, deluxe, suite, family);
            context.SaveChanges();

            context.Rooms.AddRange(
                new Room { RoomNumber = "101", Floor = 1, RoomTypeId = standard.RoomTypeId },
                new Room { RoomNumber = "102", Floor = 1, RoomTypeId = standard.RoomTypeId, Status = RoomStatus.Cleaning },
                new Room { RoomNumber = "103", Floor = 1, RoomTypeId = deluxe.RoomTypeId },
                new Room { RoomNumber = "201", Floor = 2, RoomTypeId = deluxe.RoomTypeId },
                new Room { RoomNumber = "202", Floor = 2, RoomTypeId = suite.RoomTypeId, Status = RoomStatus.Maintenance },
                new Room { RoomNumber = "301", Floor = 3, RoomTypeId = family.RoomTypeId });
            context.SaveChanges();
        }

        if (!context.ServiceCategories.Any())
        {
            var restaurant = new ServiceCategory { CategoryName = "Restaurant" };
            var laundry = new ServiceCategory { CategoryName = "Laundry" };
            context.ServiceCategories.AddRange(restaurant, laundry);
            context.SaveChanges();

            context.ServiceItems.AddRange(
                new ServiceItem { ServiceName = "Breakfast Set", UnitPrice = 80_000m, ServiceCategoryId = restaurant.ServiceCategoryId },
                new ServiceItem { ServiceName = "Dinner Set", UnitPrice = 150_000m, ServiceCategoryId = restaurant.ServiceCategoryId },
                new ServiceItem { ServiceName = "Bottled Water", UnitPrice = 15_000m, ServiceCategoryId = restaurant.ServiceCategoryId },
                new ServiceItem { ServiceName = "Shirt Washing", UnitPrice = 30_000m, ServiceCategoryId = laundry.ServiceCategoryId },
                new ServiceItem { ServiceName = "Pants Washing", UnitPrice = 30_000m, ServiceCategoryId = laundry.ServiceCategoryId },
                new ServiceItem { ServiceName = "Ironing Service", UnitPrice = 20_000m, ServiceCategoryId = laundry.ServiceCategoryId });
            context.SaveChanges();
        }
    }
}
