using Services;

namespace HotelManagement.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoNumber!")]
    [InlineData("NoSpecial1")]
    public void WeakPassword_IsRejected(string password) => Assert.NotNull(PasswordPolicy.Validate(password));

    [Fact]
    public void StrongPassword_IsAccepted() => Assert.Null(PasswordPolicy.Validate("Hotel@2026"));
}
