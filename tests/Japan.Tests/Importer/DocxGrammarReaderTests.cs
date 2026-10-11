using Japan.Importer.Parsing;
using static Japan.Tests.DocxFixture;

namespace Japan.Tests.Importer;

public class DocxGrammarReaderTests
{
    private static SourceDocument ReadSample()
    {
        using var stream = Create(
            P("NGỮ PHÁP N3"),
            P("FILE 3 — điểm 19–20/106"),
            P("BÀI 7: Thời điểm của sự việc", "Heading1"),
            P("3. ～間・～間に", "Heading2"),
            P("Ý nghĩa: Trong khoảng thời gian X."),
            P("Cấu trúc:"),
            T(["Mẫu", "Cấu trúc"], ["～間", "Nの／Vる + 間"]),
            P("Ví dụ:"),
            P("1. 夏休みの間に、富士山に登ります。"),
            P("→ Trong kỳ nghỉ hè, tôi sẽ leo núi Phú Sĩ."),
            P("4. ～ところ", "a2"),
            P("Ý nghĩa: Thời điểm cụ thể."),
            P("Cấu trúc: Vる + ところ"),
            P("Ví dụ:"),
            P("1. 出かけるところです。"),
            P("→ Tôi đang chuẩn bị ra ngoài."),
            P("Bảng so sánh nhanh", "Heading2"),
            T(["Mẫu câu", "Trọng tâm"], ["～間", "Khoảng thời gian"]),
            P("File 3/12 — điểm 19-20/106."));
        return DocxGrammarReader.Read("File03.docx", stream);
    }

    [Fact]
    public void Read_UsesHeadingStylesForLessonsAndPoints()
    {
        var doc = ReadSample();

        Assert.Equal(new PointRange(19, 20), doc.Range);
        var lesson = Assert.Single(doc.Lessons);
        Assert.Equal(7, lesson.Number);
        Assert.Equal(["～間・～間に", "～ところ"], lesson.Points.Select(p => p.Pattern));
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void Read_ResolvesHeadingFromStyleNameWhenIdIsCustom()
    {
        Assert.Equal(4, ReadSample().Lessons[0].Points[1].Index);
    }

    [Fact]
    public void Read_ConvertsTables()
    {
        var doc = ReadSample();

        var structure = doc.Lessons[0].Points[0].Structure.Tables.Single();
        Assert.Equal(["Mẫu", "Cấu trúc"], structure.Header);
        Assert.Equal(["～間", "Nの／Vる + 間"], structure.Rows.Single());
        Assert.Equal("Khoảng thời gian", doc.Lessons[0].QuickComparisons.Single().Rows[0][1]);
    }

    [Fact]
    public void ReadFile_FileLockedByAnotherProcess_ThrowsFriendlyError()
    {
        var path = Path.Combine(Path.GetTempPath(), $"locked-{Guid.NewGuid():N}.docx");
        Save(path, P("BÀI 1: Tên", "Heading1"));
        try
        {
            using var _ = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var ex = Assert.Throws<SourceFileLockedException>(() => DocxGrammarReader.ReadFile(path));
            Assert.Contains("đóng Word", ex.Message);
            Assert.Contains(Path.GetFileName(path), ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadFile_FileOpenWithReadSharing_IsReadable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"shared-{Guid.NewGuid():N}.docx");
        Save(path, P("BÀI 1: Tên", "Heading1"));
        try
        {
            using var _ = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

            Assert.Equal(1, DocxGrammarReader.ReadFile(path).Lessons.Single().Number);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
