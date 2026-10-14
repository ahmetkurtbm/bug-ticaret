using Tezgah.Payments.Logic;

namespace Tezgah.Payments.Tests;

public class ApiKeyValidatorTests
{
    [Fact]
    public void MatchingKey_IsValid() =>
        Assert.True(ApiKeyValidator.IsValid("anahtar", "anahtar"));

    [Theory]
    [InlineData("yanlis")]
    [InlineData("anahtar ")]
    [InlineData("ANAHTAR")]
    [InlineData("")]
    [InlineData(null)]
    public void WrongOrMissingKey_IsInvalid(string? provided) =>
        Assert.False(ApiKeyValidator.IsValid(provided, "anahtar"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MissingExpectedKey_IsAlwaysInvalid(string? expected) =>
        Assert.False(ApiKeyValidator.IsValid(expected, expected));
}
