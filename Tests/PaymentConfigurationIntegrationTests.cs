using BusinessObjects.Entities;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HotelManagement.Tests;

public class PaymentConfigurationIntegrationTests
{
    [Fact]
    public async Task EfModel_HasUniqueTransactionIdAndPositiveAmountConstraint()
    {
        await using var context = HotelDbContextFactory.Create();
        var model = context.GetService<IDesignTimeModel>().Model;
        var payment = model.FindEntityType(typeof(Payment));

        Assert.NotNull(payment);
        Assert.Contains(payment!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Single().Name == nameof(Payment.TransactionId));
        Assert.Contains(payment.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_Payment_Amount_Positive");
    }
}
