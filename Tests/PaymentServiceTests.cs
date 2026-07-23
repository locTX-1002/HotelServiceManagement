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
        AppSession.SignIn(new User { Id = 3, Role = new Role { RoleName = "Receptionist" } });
        var repository = new FakePaymentRepository();

        var result = await new PaymentService(repository)
            .RecordAsync(1, 100_000, PaymentMethod.BankTransfer, null);

        Assert.False(result.Ok);
        Assert.False(repository.RecordWasCalled);
    }

    [Fact]
    public async Task Receptionist_CannotVoidCompletedPayment()
    {
        AppSession.SignIn(new User { Id = 3, Role = new Role { RoleName = "Receptionist" } });
        var repository = new FakePaymentRepository();

        var result = await new PaymentService(repository).VoidAsync(1);

        Assert.False(result.Ok);
        Assert.False(repository.VoidWasCalled);
    }

    [Fact]
    public async Task Manager_CanVoidCompletedPayment()
    {
        AppSession.SignIn(new User { Id = 2, Role = new Role { RoleName = "Manager" } });
        var repository = new FakePaymentRepository { PaymentToVoid = new Payment { Id = 9, Status = PaymentStatus.Cancelled } };

        var result = await new PaymentService(repository).VoidAsync(9);

        Assert.True(result.Ok);
        Assert.True(repository.VoidWasCalled);
        Assert.Equal(PaymentStatus.Cancelled, result.Data!.Status);
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public bool RecordWasCalled { get; private set; }
        public bool VoidWasCalled { get; private set; }
        public Payment? PaymentToVoid { get; init; }
        public Task<Invoice?> GetInvoiceAsync(int id) => Task.FromResult<Invoice?>(null);
        public Task<List<Payment>> GetByInvoiceAsync(int id) => Task.FromResult(new List<Payment>());
        public Task<bool> TransactionIdExistsAsync(string id) => Task.FromResult(false);
        public Task<Payment?> RecordAsync(int invoiceId, decimal amount, PaymentMethod method,
            string? transactionId, int? userId, DateTime date)
        { RecordWasCalled = true; return Task.FromResult<Payment?>(new Payment()); }
        public Task<Payment?> VoidAsync(int paymentId) { VoidWasCalled = true; return Task.FromResult(PaymentToVoid); }
    }
}
