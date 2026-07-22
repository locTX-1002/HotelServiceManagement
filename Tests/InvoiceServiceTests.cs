using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class InvoiceServiceTests
{
    [Fact]
    public async Task PrepareAsync_CalculatesRoomServiceSurchargeDiscountAndDeposit()
    {
        AppSession.SignIn(Admin());
        var stay = new Stay
        {
            Id = 1,
            ActualCheckIn = new DateTime(2026, 7, 20, 14, 0, 0),
            Status = StayStatus.Active,
            Reservation = new Reservation
            {
                BookingCode = "BK01",
                DepositAmount = 100_000,
                DepositPaymentMethod = PaymentMethod.Cash,
                Room = new Room { RoomType = new RoomType { BasePrice = 500_000 } }
            },
            ServiceOrders = [new ServiceOrder { Status = ServiceOrderStatus.Completed, TotalAmount = 200_000 }],
            Surcharges = [new Surcharge { Subtotal = 50_000 }]
        };
        var invoices = new FakeInvoiceRepository(stay);
        var promotions = new FakePromotionRepository(new Promotion
        {
            Code = "SALE10",
            Type = PromotionType.Percentage,
            Value = 10,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 31),
            IsActive = true
        });

        var result = await new InvoiceService(invoices, promotions)
            .PrepareAsync(1, "sale10", new DateTime(2026, 7, 22, 10, 0, 0));

        Assert.True(result.Ok);
        Assert.Equal(1_000_000, result.Data!.RoomCharge);
        Assert.Equal(200_000, result.Data.ServiceCharge);
        Assert.Equal(50_000, result.Data.SurchargeAmount);
        Assert.Equal(125_000, result.Data.DiscountAmount);
        Assert.Equal(1_125_000, result.Data.TotalAmount);
        Assert.Equal(InvoiceStatus.PartiallyPaid, result.Data.Status);
        Assert.Single(result.Data.Payments);
    }

    [Fact]
    public async Task PrepareAsync_WhenInvoiceChangesConcurrently_AsksCallerToReload()
    {
        AppSession.SignIn(Admin());
        var stay = new Stay
        {
            Id = 1,
            ActualCheckIn = new DateTime(2026, 7, 20),
            Status = StayStatus.Active,
            Reservation = new Reservation { Room = new Room { RoomType = new RoomType { BasePrice = 500_000 } } }
        };
        var invoices = new FakeInvoiceRepository(stay) { SaveSucceeds = false };

        var result = await new InvoiceService(invoices, new FakePromotionRepository(new Promotion()))
            .PrepareAsync(1, null, new DateTime(2026, 7, 21));

        Assert.False(result.Ok);
        Assert.Contains("tai lai", result.Message);
    }

    private static User Admin() => new() { Id = 1, Role = new Role { RoleName = "Admin" } };

    private sealed class FakeInvoiceRepository(Stay stay) : IInvoiceRepository
    {
        public Invoice? Saved { get; private set; }
        public bool SaveSucceeds { get; init; } = true;
        public Task<Invoice?> GetByIdAsync(int id) => Task.FromResult(Saved);
        public Task<Invoice?> GetByStayAsync(int id) => Task.FromResult(Saved);
        public Task<Stay?> GetStayForBillingAsync(int id) => Task.FromResult<Stay?>(stay);
        public Task<bool> SaveAsync(Invoice invoice, bool add) { Saved = invoice; return Task.FromResult(SaveSucceeds); }
        public Task<bool> CancelAsync(int id) => Task.FromResult(true);
    }

    private sealed class FakePromotionRepository(Promotion promotion) : IPromotionRepository
    {
        public Task<List<Promotion>> GetAllAsync() => Task.FromResult(new List<Promotion> { promotion });
        public Task<Promotion?> GetByIdAsync(int id) => Task.FromResult<Promotion?>(promotion);
        public Task<Promotion?> GetByCodeAsync(string code) => Task.FromResult<Promotion?>(code == promotion.Code ? promotion : null);
        public Task<bool> CodeExistsAsync(string code, int? excludeId = null) => Task.FromResult(code == promotion.Code);
        public Task SaveAsync(Promotion entity, bool add) => Task.CompletedTask;
    }
}
