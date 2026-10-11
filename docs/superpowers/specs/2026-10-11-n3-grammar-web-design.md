# Web tra cứu ngữ pháp N3 (chạy local) — Thiết kế

Ngày: 2026-10-11

## 1. Mục tiêu

Một web chạy trên máy cá nhân để **tra cứu và đọc lại** 106 điểm ngữ pháp N3 (Bài 1–30).

- Xem theo bài, xem chi tiết từng điểm, tìm kiếm bằng tiếng Nhật hoặc tiếng Việt.
- Mỗi điểm có **phần gốc** (lấy từ file Word của người dùng) và **phần mở rộng** (giải thích sâu hơn, công thức chi tiết, cách dùng, so sánh, thêm ví dụ).
- Câu ví dụ có furigana, bật/tắt được.
- Chỉ làm ngữ pháp, nhưng cấu trúc cho phép thêm từ vựng (CSV) sau này.

Ngoài phạm vi: quiz/flashcard, lưu tiến độ, tài khoản, chạy trên internet, gọi API AI lúc chạy.

### Tiêu chí thành công

1. Chạy importer trên thư mục nguồn ra đúng **106 điểm, Bài 1–30**, kể cả bảng; khớp với số điểm ghi ở đầu mỗi file ("điểm 19–27/106").
2. Mở web, đọc được mọi điểm; điểm nào có phần mở rộng thì hiển thị kèm, có nhãn "Bổ sung".
3. Tìm `うちに`, `ウチニ`, `tranh thủ` hoặc `tranh thu` (không dấu) đều ra ～うちに. Không hỗ trợ tìm bằng romaji.
4. Không cần internet khi chạy web.

## 2. Nguồn dữ liệu

Thư mục: `C:\Users\Admin\Downloads\Tiếng Nhật - N3\Ngữ pháp`

- Dùng các file `NguPhap_N3_File01…12_*.docx`.
- Nếu cùng tên gốc có cả `.md` và `.docx` (hiện chỉ có File02) thì **ưu tiên `.md`**, vì `.docx` của File02 dùng khuôn khác (heading lệch cấp, ví dụ dồn một dòng, bảng bị dẹt thành chữ).
- Bỏ qua `Ngữ pháp N3_1.docx` và file PDF.
- Đường dẫn nguồn để trong `appsettings.json` của importer, có thể ghi đè bằng tham số dòng lệnh.

### Khuôn dạng `.docx` (File01, File03–12)

| Thành phần | Nhận diện |
|---|---|
| Đầu file | Đoạn chứa `điểm X–Y/106` → dùng để kiểm tra số lượng |
| Bài | Style `Heading1`, text `BÀI n: Tên bài` có thể kèm `(phần k/2)` |
| Điểm ngữ pháp | Style `Heading2`, text bắt đầu bằng `số. ` (vd `3. ～ずに`) |
| Bảng so sánh nhanh | Style `Heading2` **không** bắt đầu bằng số → không phải điểm; bảng ngay sau gắn vào bài hiện tại dưới dạng `QuickComparison` |
| Ý nghĩa / Cấu trúc / Lưu ý | Đoạn bắt đầu bằng `Ý nghĩa:` / `Cấu trúc:` / `Lưu ý:`; phần sau dấu `:` là nội dung; các đoạn tiếp theo (không phải nhãn khác) nối vào cùng mục; bảng (`w:tbl`) nằm trong mục nào thì thuộc mục đó |
| Ví dụ | Sau `Ví dụ:`, đoạn `số. câu tiếng Nhật` rồi đoạn `→ câu dịch` |
| Chân file | Đoạn bắt đầu bằng `File n/12` → bỏ qua |

Bài bị tách giữa hai file (vd Bài 6 phần 1/2 ở File02, phần 2/2 ở File03) được **gộp theo số bài**; số thứ tự điểm tiếp nối nhau (Bài 6: 1, 2 ở File02; 3, 4 ở File03).

### Khuôn dạng `.md` (File02)

`## BÀI n:` = bài, `### số. mẫu` = điểm, `**Ý nghĩa:**` / `**Cấu trúc:**` / `**Ví dụ:**` = nhãn, `1. câu` + `   → dịch` = ví dụ, bảng Markdown, `---` bỏ qua. Đọc bằng Markdig (AST), không dùng regex trên toàn file.

## 3. Kiến trúc

```
Japan.slnx
src/
  Japan.Core/       mô hình dữ liệu + tìm kiếm + chuẩn hoá chuỗi (không phụ thuộc web)
  Japan.Importer/   console: đọc nguồn + enrichment → sinh data/grammar.json
  Japan.Web/        ASP.NET Core Razor Pages, chỉ đọc grammar.json
tests/
  Japan.Tests/      xUnit
content/
  enrichment/       b{bài}-{điểm}.json — phần mở rộng, viết/sửa tay
  furigana-overrides.json
data/
  grammar.json      do importer sinh, commit vào git, không sửa tay
```

- .NET 10, nullable bật, warnings-as-errors.
- **Importer là nơi duy nhất** đọc Word/Markdown, chạy NMeCab và ghép enrichment. Web chỉ nạp một file `grammar.json` nên khởi động nhanh và không cần thư viện nặng.
- Sửa enrichment hoặc file Word → chạy lại importer → web tự nạp lại `grammar.json` (xem mục 6), chỉ cần F5 trình duyệt.
- `grammar.json` được commit để web chạy được kể cả khi không có thư mục nguồn.

### Mở rộng sang từ vựng (sau này, không làm bây giờ)

Thêm `VocabEntry` vào Core, `CsvVocabReader` vào Importer ghi `data/vocab.json`, trang `/Vocab` vào Web. Không tạo interface/plugin trước.

## 4. Mô hình dữ liệu (Japan.Core)

```csharp
record GrammarBook(IReadOnlyList<Lesson> Lessons, DateTimeOffset GeneratedAt);

record Lesson(int Number, string Title,
              IReadOnlyList<GrammarPoint> Points,
              IReadOnlyList<Table> QuickComparisons);

record GrammarPoint(string Id,            // "b7-1"
                    int Lesson, int Index,
                    string Pattern,        // "～うちに（１）"
                    Section Meaning, Section Structure, Section? Note,
                    IReadOnlyList<Example> Examples,
                    GrammarEnrichment? Enrichment,
                    string SourceFile);

record Section(IReadOnlyList<string> Paragraphs, IReadOnlyList<Table> Tables);

record Table(IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<string>> Rows);

record Example(string Japanese, string Vietnamese, string? Ruby = null);
// Ruby: "[会社|かいしゃ]へ[行|い]かずに…" — chỉ kanji mới có cách đọc; importer điền, file enrichment không ghi

record GrammarEnrichment(
    string Explanation,                         // giải thích chi tiết, sắc thái
    IReadOnlyList<FormationRule> Formation,     // công thức theo loại từ
    IReadOnlyList<string> Usage,                // dùng khi nào, văn nói/viết, mức trang trọng
    IReadOnlyList<string> Pitfalls,             // không dùng được khi…, lỗi hay gặp
    IReadOnlyList<Comparison> Comparisons,
    IReadOnlyList<Example> Examples);

record FormationRule(string WordType,           // "Động từ nhóm 1", "Tính từ な", "Danh từ", "する"
                     string Rule,                // "Vない bỏ ない + ずに"
                     IReadOnlyList<string> Samples); // "書く → 書かずに"

record Comparison(string Pattern, string? TargetId, string Difference);
// TargetId trỏ tới điểm khác trong sách nếu có (vd ～ないで không phải điểm N3 → null)
```

Id = `b{số bài}-{số thứ tự điểm trong bài}` theo đánh số trong heading, nên ổn định qua các lần import.

### File enrichment

`content/enrichment/b6-3.json` — cùng cấu trúc `GrammarEnrichment`, ví dụ chỉ ghi `japanese` + `vietnamese` (importer tự sinh `ruby`). Thiếu file → điểm đó không có phần mở rộng. File có id không tồn tại → importer báo lỗi.

## 5. Importer

```
dotnet run --project src/Japan.Importer [-- <thư mục nguồn>]
```

Các bước:

1. Liệt kê nguồn theo mục 2, sắp theo số file.
2. Đọc từng file → danh sách bài/điểm thô (`DocxGrammarReader`, `MarkdownGrammarReader`, cùng trả về một kiểu trung gian).
3. Gộp bài theo số bài, kiểm tra:
   - Số điểm mỗi file khớp `điểm X–Y` ở đầu file.
   - Tổng 106 điểm, bài 1–30 liên tục, không trùng id.
   - Mỗi điểm có Pattern, Meaning, Structure và ≥1 ví dụ có cả câu Nhật lẫn câu dịch.
4. Nạp `content/enrichment/*.json`, ghép theo id.
5. Sinh `Ruby` cho mọi ví dụ (gốc + bổ sung) bằng **NMeCab** (từ điển IPADIC, chạy offline): token có kanji → lấy cách đọc (katakana → hiragana), cắt phần okurigana trùng ở đầu/cuối để furigana chỉ nằm trên kanji. Áp `furigana-overrides.json` (map `từ → cách đọc`) trước khi phân tích để sửa chỗ đọc sai.
6. Ghi `data/grammar.json` (UTF-8, indent, không escape ký tự Nhật/Việt).
7. In báo cáo: số bài/điểm theo từng file, số điểm có/không có enrichment, cảnh báo.

Lỗi:
- File đang mở trong Word (bị khoá) → báo rõ "Hãy đóng Word: <tên file>", exit code ≠ 0.
- Vi phạm kiểm tra ở bước 3/4 → in danh sách lỗi kèm tên file + đoạn văn, **không ghi** `grammar.json`, exit code ≠ 0.
- Đoạn văn không nhận diện được → cảnh báo (không dừng).

## 6. Web (Razor Pages)

`GrammarRepository` (singleton) nạp `grammar.json` lúc khởi động, giữ index `Id → GrammarPoint`. Tự nạp lại khi file đổi (`FileSystemWatcher`, debounce), để chạy importer xong chỉ cần F5.

| Trang | Nội dung |
|---|---|
| `/` | Ô tìm kiếm + danh sách 30 bài, mỗi bài liệt kê các mẫu (link tới chi tiết) |
| `/bai/{n}` | Toàn bộ điểm của bài n (dạng gọn: mẫu + ý nghĩa) + bảng so sánh nhanh của bài |
| `/nguphap/{id}` | Chi tiết: mẫu; **Gốc**: ý nghĩa, cấu trúc, lưu ý, bảng, ví dụ; **Bổ sung**: giải thích, bảng công thức theo loại từ, cách dùng, lỗi hay gặp, so sánh (link nếu có `TargetId`), ví dụ thêm; nút điểm trước/sau |
| `/timkiem?q=` | Kết quả tìm kiếm (cũng dùng cho tìm-khi-gõ trên trang `/` qua `fetch` trả partial HTML) |

Giao diện:
- Bootstrap 5 có sẵn trong template, **đóng gói local** (không CDN).
- Font Nhật dùng font hệ thống (`"Yu Gothic UI", Meiryo, sans-serif`) — không tải font ngoài.
- Furigana: render `<ruby>漢字<rt>かんじ</rt></ruby>`; nút bật/tắt đổi class trên `<body>` (CSS ẩn `rt`), lưu lựa chọn vào `localStorage`.
- Phần "Bổ sung" có viền/nhãn riêng để phân biệt với nội dung giáo trình.

### Tìm kiếm (Japan.Core, `GrammarSearch`)

Duyệt tuần tự trong bộ nhớ (106 điểm là đủ nhanh). Chuẩn hoá cả câu truy vấn và nội dung:
- Unicode NFKC (`～`/`~`, chữ toàn/bán độ rộng), chữ thường.
- Katakana → hiragana.
- Tiếng Việt: bỏ dấu (`tranh thủ` khớp `tranh thu`), `đ` → `d`.
- Bỏ `～` ở đầu khi so khớp mẫu.

Xếp hạng: khớp mẫu (Pattern) > ý nghĩa > cấu trúc/giải thích bổ sung > ví dụ. Kết quả hiển thị mẫu, bài, đoạn trích có tô sáng từ khoá.

## 7. Nội dung mở rộng

- Viết bởi AI (trong Cursor) cho cả 106 điểm, thành các file `content/enrichment/*.json`, theo từng nhóm bài; AI tự rà soát, người dùng yêu cầu sửa sau nếu cần.
- Mỗi điểm: giải thích 1–3 đoạn; công thức tách theo từng loại từ áp dụng được, mỗi loại 1–3 ví dụ chia; 2–5 ý cách dùng; 1–4 lỗi hay gặp; so sánh với mẫu gần nghĩa (ưu tiên mẫu có trong sách, ghi `TargetId`); 3–5 ví dụ mới, khác ví dụ gốc, độ khó N3.
- Văn phong tiếng Việt thống nhất với giáo trình (dùng "Vる", "Aい", "Nの"...).
- Web hiển thị nhãn "Bổ sung" trên toàn bộ phần này.

## 8. Kiểm thử (xUnit)

- `DocxGrammarReader`: dựng file `.docx` nhỏ trong test bằng OpenXML SDK (bài, điểm, nhãn, bảng trong Cấu trúc, mục "Bảng so sánh nhanh", chân file) → kiểm tra kết quả.
- `MarkdownGrammarReader`: chuỗi Markdown mẫu theo khuôn File02.
- Gộp bài nhiều phần, kiểm tra số lượng, id trùng, enrichment id không tồn tại.
- Furigana: `勉強する` → `[勉強|べんきょう]する`; okurigana `行かずに` → `[行|い]かずに`; override được áp dụng.
- `GrammarSearch`: chuẩn hoá (NFKC, kana, bỏ dấu), thứ tự xếp hạng.
- Web: `WebApplicationFactory` gọi `/`, `/bai/7`, `/nguphap/b7-1`, `/timkiem?q=tranh thu` → 200 và có nội dung mong đợi.
- Kiểm thử thật (thủ công/CI local): chạy importer trên thư mục nguồn thật → 106 điểm, 0 lỗi.

## 9. Rủi ro

- **Khuôn Word lệch** ở file chưa đọc được (File01, File08 đang bị Word khoá khi thiết kế) → kiểm tra số lượng ở bước 3 sẽ phát hiện; xử lý từng trường hợp khi chạy import lần đầu.
- **NMeCab đọc sai** một số từ (tên riêng, từ nhiều cách đọc) → `furigana-overrides.json`.
- **Nội dung AI có thể sai sắc thái** → luôn gắn nhãn "Bổ sung", người dùng yêu cầu sửa khi phát hiện.
