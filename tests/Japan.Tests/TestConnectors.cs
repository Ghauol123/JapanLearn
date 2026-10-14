using Japan.Core;

namespace Japan.Tests;

internal static class TestConnectors
{
    public static ConnectorBook Sample()
    {
        var dakara = new Connector("dakara", "だから", "だから", JlptLevel.N4, "vì vậy, cho nên", ConnectorRegister.Spoken,
            "Nối nguyên nhân ở câu trước với kết quả ở câu sau, dùng nhiều trong hội thoại.",
            ["Đứng đầu câu sau, sau dấu 。"],
            ["Không dùng trong văn bản trang trọng."],
            [new Comparison("したがって", "shitagatte", "したがって trang trọng, dùng trong văn viết.")],
            [
                new Example("雨が降った。だから、試合は中止だ。", "Trời mưa. Vì vậy trận đấu bị hủy.",
                    "[雨|あめ]が[降|ふ]った。だから、[試合|しあい]は[中止|ちゅうし]だ。"),
                new Example("疲れた。だから、早く寝る。", "Mệt rồi. Cho nên tôi đi ngủ sớm."),
            ]);
        var shitagatte = new Connector("shitagatte", "したがって", "したがって", JlptLevel.N3, "do đó, vì thế", ConnectorRegister.Written,
            "Rút ra kết luận logic từ câu trước, dùng trong văn viết.", [], [], [],
            [
                new Example("人口が減っている。したがって、税収も減る。", "Dân số đang giảm. Do đó thu thuế cũng giảm."),
                new Example("条件を満たしていない。したがって、申請は無効だ。", "Không đáp ứng điều kiện. Vì thế đơn không hợp lệ."),
            ]);
        var shikashi = new Connector("shikashi", "しかし", "しかし", JlptLevel.N5, "tuy nhiên, nhưng", ConnectorRegister.Both,
            "Nêu ý trái ngược với câu trước.", [], [],
            [new Comparison("でも", null, "でも thân mật hơn, dùng trong hội thoại.")],
            [
                new Example("高い。しかし、品質はいい。", "Đắt. Tuy nhiên chất lượng tốt."),
                new Example("努力した。しかし、だめだった。", "Đã cố gắng. Nhưng không được."),
            ]);

        return new ConnectorBook(
            [
                new ConnectorGroup("thuan-tiep", 1, "Thuận tiếp — kết quả, hệ quả", "順接",
                    "Câu sau là kết quả của câu trước.", [dakara, shitagatte]),
                new ConnectorGroup("nghich-tiep", 2, "Nghịch tiếp — trái ngược", "逆接",
                    "Câu sau trái với điều câu trước gợi ra.", [shikashi]),
            ],
            new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero));
    }
}
