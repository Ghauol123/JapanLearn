using Japan.Importer.Parsing;

namespace Japan.Tests.Importer;

public class MarkdownGrammarReaderTests
{
    private const string Sample = """
        # NGỮ PHÁP N3 — FILE 2 (điểm 10–18/106)

        *Bài 4 – Bài 5 (trọn vẹn) + Bài 6 (phần 1/2).*

        ---

        ## BÀI 4: Mong muốn

        ### 1. ～つもりはない

        **Ý nghĩa:** Không có ý định làm gì đó.

        **Cấu trúc:** Vる + つもりはない／つもりはなかった

        **Ví dụ:**
        1. 自分の意見を変えるつもりはない。
           → Tôi không có ý định thay đổi ý kiến của mình.
        2. そんなことを言うつもりはなかった。
           → Tôi đã không có ý định nói điều đó.

        ---

        ## BÀI 5: Các giai đoạn của sự việc

        ### 2. 複合動詞（出す・始める） — Động từ ghép

        **Ý nghĩa:**
        | Đuôi ghép | Ý nghĩa |
        |---|---|
        | ～出す | Bắt đầu đột ngột |
        | ～始める | Bắt đầu |

        **Cấu trúc:** Vます (bỏ ます) + 出す／始める

        **Ví dụ:**
        1. 店員が突然歌い出した。
           → Nhân viên đột nhiên hát lên.

        ## BÀI 6: Ngữ pháp N4 biến đổi hình thức (phần 1/2)

        ### 2. ～なければならない

        **Ý nghĩa:**
        - 「ならない」thể hiện nghĩa vụ khách quan.
        - 「いけない」thể hiện nghĩa vụ chủ quan.

        **Cấu trúc:** Vない + なければならない

        **Ví dụ:**
        1. 本は２週間以内に返さなくてはなりません。
           → Sách phải trả trong vòng 2 tuần.

        ---
        *File 2/12 — điểm 10-18/106.*
        """;

    [Fact]
    public void Read_ParsesRangeLessonsAndPoints()
    {
        var doc = MarkdownGrammarReader.Read("File02.md", Sample);

        Assert.Equal(new PointRange(10, 18), doc.Range);
        Assert.Equal([4, 5, 6], doc.Lessons.Select(l => l.Number));
        Assert.Equal("Ngữ pháp N4 biến đổi hình thức", doc.Lessons[2].Title);
        Assert.Empty(doc.Warnings);
    }

    [Fact]
    public void Read_ParsesBoldLabelsAndListExamples()
    {
        var point = MarkdownGrammarReader.Read("File02.md", Sample).Lessons[0].Points.Single();

        Assert.Equal("～つもりはない", point.Pattern);
        Assert.Equal(["Không có ý định làm gì đó."], point.Meaning.Paragraphs);
        Assert.Equal(["Vる + つもりはない／つもりはなかった"], point.Structure.Paragraphs);
        Assert.Equal(["自分の意見を変えるつもりはない。", "そんなことを言うつもりはなかった。"], point.Examples.Select(e => e.Japanese));
        Assert.Equal("Tôi đã không có ý định nói điều đó.", point.Examples[1].Vietnamese);
    }

    [Fact]
    public void Read_TableDirectlyAfterLabel_BelongsToMeaning()
    {
        var point = MarkdownGrammarReader.Read("File02.md", Sample).Lessons[1].Points.Single();

        var table = point.Meaning.Tables.Single();
        Assert.Equal(["Đuôi ghép", "Ý nghĩa"], table.Header);
        Assert.Equal(["～出す", "Bắt đầu đột ngột"], table.Rows[0]);
        Assert.Equal(2, table.Rows.Count);
    }

    [Fact]
    public void Read_BulletMeaning_BecomesParagraphs()
    {
        var point = MarkdownGrammarReader.Read("File02.md", Sample).Lessons[2].Points.Single();

        Assert.Equal(2, point.Index);
        Assert.Equal(2, point.Meaning.Paragraphs.Count);
        Assert.StartsWith("「ならない」", point.Meaning.Paragraphs[0]);
    }
}
