using Tezgah.Notifications.Logic;

namespace Tezgah.Notifications.Tests;

public class ApiKeyValidatorTests
{
    [Fact]
    public void IsValid_MatchingKey_ReturnsTrue() =>
        Assert.True(ApiKeyValidator.IsValid("anahtar", "anahtar"));

    [Theory]
    [InlineData("yanlis", "anahtar")]
    [InlineData("anahtar ", "anahtar")]
    [InlineData("ANAHTAR", "anahtar")]
    [InlineData("a", "anahtar")]
    public void IsValid_DifferentKey_ReturnsFalse(string provided, string expected) =>
        Assert.False(ApiKeyValidator.IsValid(provided, expected));

    [Theory]
    [InlineData(null, "anahtar")]
    [InlineData("", "anahtar")]
    [InlineData("anahtar", null)]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void IsValid_MissingValue_ReturnsFalse(string? provided, string? expected) =>
        Assert.False(ApiKeyValidator.IsValid(provided, expected));
}
