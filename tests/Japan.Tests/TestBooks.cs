using Japan.Core;

namespace Japan.Tests;

internal static class TestBooks
{
    public static GrammarPoint Point(int lesson, int index, string pattern, string meaning, params Example[] examples) =>
        new($"b{lesson}-{index}", lesson, index, pattern,
            new Section([meaning], []),
            new Section([$"Vる + {pattern.TrimStart('～')}"], []),
            null, examples, null, "test.docx");

    public static GrammarBook Sample()
    {
        var uchini = Point(7, 1, "～うちに（１）", "Tranh thủ làm hành động Y trước khi xảy ra sự thay đổi X.",
                new Example("熱いうちに食べてください。", "Hãy ăn khi còn nóng.", "[熱|あつ]いうちに[食|た]べてください。"))
            with
            {
                Enrichment = new GrammarEnrichment(
                    "Nhấn mạnh việc tranh thủ.",
                    [new FormationRule("Tính từ い", "Aい + うちに", ["熱い → 熱いうちに"])],
                    ["Văn nói và văn viết."],
                    ["Không dùng với trạng thái không thay đổi."],
                    [new Comparison("～間に", "b7-3", "間に nhấn mạnh khoảng thời gian.")],
                    [new Example("若いうちに旅行したい。", "Tôi muốn đi du lịch khi còn trẻ.", "[若|わか]いうちに[旅行|りょこう]したい。")]),
            };
        var aidani = Point(7, 3, "～間・～間に", "Trong khoảng thời gian X diễn ra.",
            new Example("夏休みの間に、富士山に登ります。", "Trong kỳ nghỉ hè, tôi sẽ leo núi Phú Sĩ."));
        var zuni = Point(6, 3, "～ずに", "Giống nghĩa với ～ないで nhưng trang trọng hơn.",
            new Example("辞書を使わずに新聞が読めるようになりたい。", "Tôi muốn có thể đọc báo mà không cần dùng từ điển."));

        return new GrammarBook(
            [
                new Lesson(6, "Ngữ pháp N4 biến đổi hình thức", [zuni], []),
                new Lesson(7, "Thời điểm của sự việc", [uchini, aidani],
                    [new Table(["Mẫu câu", "Trọng tâm"], [["～うちに", "Tranh thủ khi còn..."]])]),
            ],
            new DateTimeOffset(2026, 10, 11, 0, 0, 0, TimeSpan.Zero));
    }
}
