using BusinessObjects;
using BusinessObjects.Entities;
using BusinessObjects.Enums;
using Repositories;
using Services;

namespace HotelManagement.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task BankTransfer_RequiresTransactionId()
    {
        AppSession.SignIn(new User { Id = 3, Role = new Role { RoleName = RoleNames.Receptionist } });
        var repository = new FakePaymentRepository();

        var result = await new PaymentService(repository)
            .RecordAsync(1, 100_000, PaymentMethod.BankTransfer, null);

        Assert.False(result.Ok);
        Assert.False(repository.RecordWasCalled);
    }

    [Fact]
    public async Task Receptionist_CannotVoidCompletedPayment()
    {
        AppSession.SignIn(new User { Id = 3, Role = new Role { RoleName = RoleNames.Receptionist } });
        var repository = new FakePaymentRepository();

        var result = await new PaymentService(repository).VoidAsync(1);

        Assert.False(result.Ok);
        Assert.False(repository.VoidWasCalled);
    }

    [Fact]
    public async Task Manager_CanVoidCompletedPayment()
    {
        AppSession.SignIn(new User { Id = 2, Role = new Role { RoleName = RoleNames.Manager } });
        var repository = new FakePaymentRepository { PaymentToVoid = new Payment { Id = 9, Status = PaymentStatus.Cancelled } };

        var result = await new PaymentService(repository).VoidAsync(9);

        Assert.True(result.Ok);
        Assert.True(repository.VoidWasCalled);
        Assert.Equal(PaymentStatus.Cancelled, result.Data!.Status);
    }

    [Theory]
    [InlineData(0, 0, 2_500_000)]
    [InlineData(1_000_000, 1_000_000, 1_500_000)]
    [InlineData(2_500_000, 2_500_000, 0)]
    public async Task PaymentSummary_ShowsUnpaidPartialAndPaidBalances(
        decimal completedAmount, decimal expectedPaid, decimal expectedRemaining)
    {
        var payments = completedAmount == 0
            ? new List<Payment>()
            : [new Payment { Amount = completedAmount, Status = PaymentStatus.Completed }];
        var repository = new FakePaymentRepository
        {
            InvoiceForSummary = new Invoice
            {
                Id = 10,
                TotalAmount = 2_500_000,
                Payments = payments,
            },
        };

        var result = await new PaymentService(repository).GetSummaryAsync(10);

        Assert.True(result.Ok);
        Assert.Equal(expectedPaid, result.Data!.PaidAmount);
        Assert.Equal(expectedRemaining, result.Data.RemainingAmount);
    }

    [Fact]
    public async Task PaymentSummary_DoesNotCountCancelledTransactions()
    {
        var repository = new FakePaymentRepository
        {
            InvoiceForSummary = new Invoice
            {
                Id = 11,
                TotalAmount = 2_500_000,
                Payments =
                [
                    new Payment { Amount = 1_000_000, Status = PaymentStatus.Completed },
                    new Payment { Amount = 1_500_000, Status = PaymentStatus.Cancelled },
                ],
            },
        };

        var result = await new PaymentService(repository).GetSummaryAsync(11);

        Assert.True(result.Ok);
        Assert.Equal(1_000_000, result.Data!.PaidAmount);
        Assert.Equal(1_500_000, result.Data.RemainingAmount);
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public bool RecordWasCalled { get; private set; }
        public bool VoidWasCalled { get; private set; }
        public Payment? PaymentToVoid { get; init; }
        public Invoice? InvoiceForSummary { get; init; }
        public Task<Invoice?> GetInvoiceAsync(int id) => Task.FromResult(InvoiceForSummary);
        public Task<List<Payment>> GetByInvoiceAsync(int id) => Task.FromResult(new List<Payment>());
        public Task<bool> TransactionIdExistsAsync(string id) => Task.FromResult(false);
        public Task<Payment?> RecordAsync(int invoiceId, decimal amount, PaymentMethod method,
            string? transactionId, int? userId, DateTime date)
        { RecordWasCalled = true; return Task.FromResult<Payment?>(new Payment()); }
        public Task<Payment?> VoidAsync(int paymentId) { VoidWasCalled = true; return Task.FromResult(PaymentToVoid); }
    }
}
