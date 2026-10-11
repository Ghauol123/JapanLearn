# 文法 N3 — Web học ngữ pháp JLPT N3

Web chạy cục bộ để tra cứu và ôn ngữ pháp JLPT N3: 28 bài, 106 điểm ngữ pháp, lấy từ bộ giáo trình docx, kèm phần **Bổ sung** (giải thích chi tiết, công thức theo loại từ, cách dùng, lỗi hay gặp, so sánh, ví dụ thêm).

## Tính năng

- Mục lục theo bài, trang riêng cho từng điểm ngữ pháp, chuyển điểm trước/sau bằng phím ← / →.
- Furigana tự sinh cho toàn bộ tiếng Nhật, bật/tắt bằng nút **Furigana**.
- Tìm kiếm tiếng Việt không dấu, hiragana/katakana và cả `～` (ví dụ `tranh thu`, `うちに`, `ウチニ`); bấm `/` để vào ô tìm kiếm.
- Giao diện sáng/tối, font Be Vietnam Pro và Noto Sans JP tự host (dùng được khi không có mạng).

## Chạy web

Cần [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet run --project src/Japan.Web
```

Mở http://localhost:5126. Dữ liệu đọc từ `data/grammar.json` (đã có sẵn trong repo); web tự nạp lại khi file này thay đổi.

## Cập nhật dữ liệu

`data/grammar.json` được sinh bởi importer từ các file `NguPhap_N3_FileXX_*.docx` (hoặc `.md` cùng tên, được ưu tiên nếu có). Bộ docx gốc không nằm trong repo.

```powershell
dotnet run --project src/Japan.Importer -- "D:\đường\dẫn\thư mục docx"
```

Nếu không truyền đường dẫn, importer dùng `sourceDirectory` trong `src/Japan.Importer/appsettings.json`. Khi gặp lỗi cấu trúc (thiếu mục "Cấu trúc", "Ví dụ", trùng id…), importer báo lỗi và không ghi đè dữ liệu cũ.

Nội dung bổ sung và hiệu chỉnh nằm trong `content/`:

- `content/enrichment/<id>.json`: phần Bổ sung của từng điểm (ví dụ `b1-1.json` là Bài 1, điểm 1). Sửa xong chạy lại importer.
- `content/furigana-overrides.json`: sửa cách đọc khi furigana tự sinh bị sai (ví dụ `"一晩": "ひとばん"`).

## Cấu trúc

| Thư mục | Nội dung |
| --- | --- |
| `src/Japan.Core` | Model, chuẩn hoá văn bản, tìm kiếm, định dạng ruby `[漢字\|かんじ]` |
| `src/Japan.Importer` | Đọc docx/md, gắn phần Bổ sung, sinh furigana (NMeCab), ghi `data/grammar.json` |
| `src/Japan.Web` | Razor Pages + Bootstrap 5 |
| `tests/Japan.Tests` | Test xUnit cho cả ba project |

## Test

```powershell
dotnet test Japan.slnx
```

Lưu ý: tắt web đang chạy trước khi test, vì tiến trình web khoá file build.
