using System.Text;
using Japan.Core;
using Japan.Importer.Parsing;

namespace Japan.Tests.Importer;

public class DocumentBuilderTests
{
    private static SourceDocument BuildFile03Sample()
    {
        var b = new DocumentBuilder("File03.docx");
        b.Paragraph("NGỮ PHÁP N3");
        b.Paragraph("FILE 3 — điểm 19–27/106 (Bài 6 phần 2, Bài 7)");
        b.Paragraph("Tổng hợp & dịch từ Giáo trình Ngữ pháp N3");
        b.LessonHeading("BÀI 6: Ngữ pháp N4 biến đổi hình thức (phần 2/2)");
        b.PointHeading("3. ～ずに");
        b.Paragraph("Ý nghĩa: Giống nghĩa với \"～ないで\".");
        b.Paragraph("Cấu trúc: Vない (bỏ ない) + ず(に)");
        b.Paragraph("Ví dụ:");
        b.Paragraph("1. 急いでいたので、かぎをかけずに出かけてしまった。");
        b.Paragraph("→ Vì vội nên tôi đã ra ngoài mà quên khóa cửa.");
        b.LessonHeading("BÀI 7: Thời điểm của sự việc");
        b.PointHeading("3. ～間・～間に");
        b.Paragraph("Ý nghĩa: X間、Y: suốt khi X diễn ra.");
        b.Paragraph("Cấu trúc:");
        b.Table(new Table(["Mẫu", "Cấu trúc"], [["～間", "Nの／Vる + 間"]]));
        b.Paragraph("");
        b.Paragraph("Lưu ý: Phân biệt với ～うちに.");
        b.Paragraph("Ví dụ:");
        b.Paragraph("1. 夏休みの間に、富士山に登ります。");
        b.Paragraph("→ Trong kỳ nghỉ hè, tôi sẽ leo núi Phú Sĩ.");
        b.PointHeading("Bảng so sánh nhanh: các mẫu chỉ thời điểm");
        b.Table(new Table(["Mẫu câu", "Trọng tâm"], [["～うちに", "Nhấn mạnh trạng thái"]]));
        b.Paragraph("File 3/12 — điểm 19-27/106.");
        return b.Build();
    }

    [Fact]
    public void Build_ReadsRangeFromHeaderOnly()
    {
        Assert.Equal(new PointRange(19, 27), BuildFile03Sample().Range);
    }

    [Fact]
    public void Build_ReadsLessonsDeclaredOutsideThe106Points()
    {
        var b = new DocumentBuilder("File08.docx");
        b.Paragraph("FILE 8 — điểm 64–72/106 (Bài 17, Bài 18, Bài 20 phần 1)");
        b.Paragraph("(Bài 19 — Trợ từ 1 — là bảng tổng hợp trợ từ riêng, không tính vào 106 điểm; sẽ gửi làm phụ lục riêng nếu cần)");
        b.LessonHeading("BÀI 17: Ý định");

        var doc = b.Build();

        Assert.Equal([19], doc.ExcludedLessons);
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void Build_ParsesLessonsWithoutPartSuffix()
    {
        var doc = BuildFile03Sample();

        Assert.Equal([6, 7], doc.Lessons.Select(l => l.Number));
        Assert.Equal("Ngữ pháp N4 biến đổi hình thức", doc.Lessons[0].Title);
        Assert.Equal("Thời điểm của sự việc", doc.Lessons[1].Title);
    }

    [Fact]
    public void Build_ParsesPointSections()
    {
        var zuni = BuildFile03Sample().Lessons[0].Points.Single();

        Assert.Equal(3, zuni.Index);
        Assert.Equal("～ずに", zuni.Pattern);
        Assert.Equal(["Giống nghĩa với \"～ないで\"."], zuni.Meaning.Paragraphs);
        Assert.Equal(["Vない (bỏ ない) + ず(に)"], zuni.Structure.Paragraphs);
        Assert.Null(zuni.Note);
        var example = Assert.Single(zuni.Examples);
        Assert.Equal("急いでいたので、かぎをかけずに出かけてしまった。", example.Japanese);
        Assert.Equal("Vì vội nên tôi đã ra ngoài mà quên khóa cửa.", example.Vietnamese);
    }

    [Fact]
    public void Build_AttachesTablesAndNotes()
    {
        var aida = BuildFile03Sample().Lessons[1].Points.Single();

        Assert.Empty(aida.Structure.Paragraphs);
        Assert.Equal("Nの／Vる + 間", aida.Structure.Tables.Single().Rows[0][1]);
        Assert.Equal(["Phân biệt với ～うちに."], aida.Note!.Paragraphs);
    }

    [Fact]
    public void Build_QuickComparisonGoesToLessonNotPoints()
    {
        var doc = BuildFile03Sample();

        Assert.Single(doc.Lessons[1].Points);
        Assert.Equal("Nhấn mạnh trạng thái", doc.Lessons[1].QuickComparisons.Single().Rows[0][1]);
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void Paragraph_InlineArrowExample_IsSplit()
    {
        var b = StartPoint();
        b.Paragraph("自分の意見を変えるつもりはない。 → Tôi không có ý định thay đổi ý kiến của mình.");

        var example = b.Build().Lessons[0].Points[0].Examples.Single();
        Assert.Equal("自分の意見を変えるつもりはない。", example.Japanese);
        Assert.Equal("Tôi không có ý định thay đổi ý kiến của mình.", example.Vietnamese);
    }

    [Fact]
    public void Paragraph_MissingTranslation_KeepsExampleWithEmptyVietnamese()
    {
        var b = StartPoint();
        b.Paragraph("1. 一つ目。");
        b.Paragraph("2. 二つ目。");
        b.Paragraph("→ Câu thứ hai.");

        var examples = b.Build().Lessons[0].Points[0].Examples;
        Assert.Equal(["一つ目。", "二つ目。"], examples.Select(e => e.Japanese));
        Assert.Equal(["", "Câu thứ hai."], examples.Select(e => e.Vietnamese));
    }

    [Theory]
    [InlineData("３．～ず", 3, "～ず")]
    [InlineData("  4 .  ～ところ（＋助詞） ", 4, "～ところ（＋助詞）")]
    public void PointHeading_AcceptsWidthAndSpacingVariants(string heading, int index, string pattern)
    {
        var b = new DocumentBuilder("x.docx");
        b.LessonHeading("BÀI 6: Tên");
        b.PointHeading(heading);

        var point = b.Build().Lessons[0].Points.Single();
        Assert.Equal(index, point.Index);
        Assert.Equal(pattern, point.Pattern);
    }

    [Fact]
    public void Labels_AcceptDecomposedVietnamese()
    {
        var b = new DocumentBuilder("x.docx");
        b.LessonHeading("BÀI 1: Tên".Normalize(NormalizationForm.FormD));
        b.PointHeading("1. ～ば");
        b.Paragraph("Ý nghĩa: Nếu.".Normalize(NormalizationForm.FormD));

        var point = b.Build().Lessons.Single().Points.Single();
        Assert.Equal(["Nếu."], point.Meaning.Paragraphs);
    }

    [Fact]
    public void Paragraph_MultipleMeaningParagraphs_AreKept()
    {
        var b = new DocumentBuilder("x.docx");
        b.LessonHeading("BÀI 6: Tên");
        b.PointHeading("2. ～なければならない");
        b.Paragraph("Ý nghĩa:");
        b.Paragraph("「ならない」thể hiện nghĩa vụ khách quan.");
        b.Paragraph("「いけない」thể hiện nghĩa vụ chủ quan.");

        Assert.Equal(2, b.Build().Lessons[0].Points[0].Meaning.Paragraphs.Count);
    }

    [Fact]
    public void Paragraph_OutsideAnySection_IsWarned()
    {
        var b = new DocumentBuilder("x.docx");
        b.LessonHeading("BÀI 6: Tên");
        b.PointHeading("1. ～ず");
        b.Paragraph("Đoạn lạc chỗ");

        Assert.Contains(b.Build().Warnings, w => w.Contains("Đoạn lạc chỗ"));
    }

    private static DocumentBuilder StartPoint()
    {
        var b = new DocumentBuilder("x.md");
        b.LessonHeading("BÀI 4: Mong muốn");
        b.PointHeading("1. ～つもりはない");
        b.Paragraph("Ví dụ:");
        return b;
    }
}
