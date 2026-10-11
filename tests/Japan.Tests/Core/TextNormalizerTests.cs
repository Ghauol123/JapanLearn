using System.Text;
using Japan.Core;

namespace Japan.Tests.Core;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("～うちに", "うちに")]
    [InlineData("〜ウチニ", "うちに")]
    [InlineData("~うちに", "うちに")]
    [InlineData("Tranh Thủ", "tranh thu")]
    [InlineData("Đã định", "da dinh")]
    [InlineData("Ưu tiên", "uu tien")]
    [InlineData("がる", "がる")]
    [InlineData("ガル", "がる")]
    [InlineData("（１）", "(1)")]
    [InlineData("  nhiều   khoảng  trắng ", "nhieu khoang trang")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("～", "")]
    public void Normalize_FoldsWidthKanaDiacriticsAndWaveDash(string input, string expected) =>
        Assert.Equal(expected, TextNormalizer.Normalize(input));

    [Fact]
    public void Normalize_TreatsDecomposedVietnameseLikeComposed() =>
        Assert.Equal(TextNormalizer.Normalize("nghĩa"), TextNormalizer.Normalize("nghĩa".Normalize(NormalizationForm.FormD)));

    [Fact]
    public void Normalize_Null_ReturnsEmpty() => Assert.Equal("", TextNormalizer.Normalize(null));
}
