using System.Globalization;
using Tezgah.Application.Common.Text;

namespace Tezgah.UnitTests.Text;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Erkek Çocuk Ayakkabı", "erkek-cocuk-ayakkabi")]
    [InlineData("Ev & Yaşam", "ev-yasam")]
    [InlineData("  İnce Şık Gömlek!  ", "ince-sik-gomlek")]
    public void Slugify_handles_turkish_characters(string input, string expected)
    {
        Assert.Equal(expected, SlugHelper.Slugify(input));
    }

    [Fact]
    public void Email_normalization_is_culture_independent()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            // Türkçe kültürde "i".ToUpper() → "İ" olur; e-posta karşılaştırması bundan etkilenmemeli.
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal("MUSTERI@TEZGAH.TEST", SlugHelper.NormalizeEmail("musteri@tezgah.test"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
