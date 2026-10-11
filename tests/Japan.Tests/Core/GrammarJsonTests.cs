using Japan.Core;

namespace Japan.Tests.Core;

public class GrammarJsonTests
{
    [Fact]
    public void RoundTrip_PreservesContent()
    {
        var back = GrammarJson.Deserialize(GrammarJson.Serialize(TestBooks.Sample()));

        var point = back.Lessons[1].Points[0];
        Assert.Equal("b7-1", point.Id);
        Assert.Equal("～うちに（１）", point.Pattern);
        Assert.Equal("Hãy ăn khi còn nóng.", point.Examples[0].Vietnamese);
        Assert.Equal("[熱|あつ]いうちに[食|た]べてください。", point.Examples[0].Ruby);
        Assert.Equal("b7-3", point.Enrichment!.Comparisons[0].TargetId);
        Assert.Equal("Tranh thủ khi còn...", back.Lessons[1].QuickComparisons[0].Rows[0][1]);
        Assert.Null(back.Lessons[1].Points[1].Enrichment);
    }

    [Fact]
    public void Serialize_KeepsJapaneseAndVietnameseUnescaped_AndSkipsComputedProperties()
    {
        var json = GrammarJson.Serialize(TestBooks.Sample());

        Assert.Contains("熱いうちに食べてください。", json);
        Assert.Contains("Tranh thủ", json);
        Assert.Contains("\"lessons\"", json);
        Assert.DoesNotContain("isEmpty", json);
    }
}
