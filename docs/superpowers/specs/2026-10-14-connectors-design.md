# Mục "Từ nối" (接続詞) N5–N2 — Thiết kế

Ngày: 2026-10-14

## Mục tiêu

Thêm vào web một mục tra cứu và ôn **接続詞** (từ nối đứng đầu câu, nối hai câu hoặc hai đoạn) từ N5 đến N2, phục vụ ôn thi JLPT — chủ yếu dạng 文章の文法 và đọc hiểu.

Thành công khi: đọc hai câu, người học chọn được đúng từ nối và hiểu vì sao các từ gần nghĩa thì sai.

## Phạm vi

- Chỉ 接続詞. Không gồm 接続助詞 (～ので、～のに、～けど…) và mẫu ngữ pháp nối (～にしたがって…).
- Cấp N5–N2. Từ chỉ có ở N1 (もしくは、よって、ゆえに…) không đưa vào.
- Sắp xếp theo nhóm chức năng, mỗi từ có nhãn cấp, lọc được theo cấp.
- Không có bài tập / quiz trong phiên bản này.

## Nội dung

### File nguồn: `content/connectors/<nhóm>.json`

Mỗi nhóm chức năng một file, tên file = id nhóm. Nội dung giải thích và ví dụ tự viết, không chép từ nguồn.

```json
{
  "id": "thuan-tiep",
  "order": 1,
  "title": "Thuận tiếp — kết quả, hệ quả",
  "jaTitle": "順接",
  "description": "Câu sau là kết quả hoặc hệ quả tự nhiên của câu trước.",
  "connectors": [
    {
      "id": "dakara",
      "word": "だから",
      "reading": "だから",
      "level": "N4",
      "meaning": "vì vậy, cho nên",
      "register": "spoken",
      "explanation": "…",
      "usage": ["…"],
      "pitfalls": ["…"],
      "comparisons": [
        { "pattern": "それで", "targetId": "sorede", "difference": "…" }
      ],
      "examples": [
        { "japanese": "…。だから、…。", "vietnamese": "…" }
      ]
    }
  ]
}
```

- `id`: chữ thường ASCII và dấu `-` (romaji), duy nhất trên toàn bộ các nhóm; dùng làm URL.
- `level`: `N5` | `N4` | `N3` | `N2`.
- `register`: `spoken` (văn nói) | `written` (văn viết) | `both`.
- `comparisons[].targetId`: id một từ nối khác hoặc `null`.
- Mỗi ví dụ là hai câu được nối bằng từ đó, kèm bản dịch.

### Nhóm chức năng và danh sách từ

Phân nhóm theo cách chia phổ biến của ngữ pháp tiếng Nhật: 順接・逆接・並立/添加・対比/選択・説明/補足・転換.

**Quy tắc chọn cấp:** JLPT không công bố danh sách chính thức từ 2010. Cấp lấy từ hai nguồn — bảng 接続詞 của JLPT Sensei và nhãn JLPT trên Jisho. Khi hai nguồn khác nhau, lấy **cấp thấp hơn** (cấp sớm nhất người học gặp từ đó). Từ không nguồn nào gắn nhãn thì tự đánh giá, ghi "tự đánh giá".

| Nhóm (id) | Từ | Sensei | Jisho | Chọn |
|---|---|---|---|---|
| **Thuận tiếp** `thuan-tiep` 順接 | だから | — | N4 | N4 |
| | それで | N4 | N4 | N4 |
| | すると | — | N4 | N4 |
| | そこで | — | N3 | N3 |
| | そのため | N3 | N2 | N3 |
| | したがって | — | N3 | N3 |
| **Nghịch tiếp** `nghich-tiep` 逆接 | でも | N5 | N5 | N5 |
| | しかし | N5 | N5 | N5 |
| | けれども | N5 | N4 | N5 |
| | それでも | N4 | N3 | N4 |
| | だが | — | N3 | N3 |
| | ところが | N3 | N3 | N3 |
| | それなのに | N2 | N2 | N2 |
| **Bổ sung** `bo-sung` 添加 | それに | N4 | N4 | N4 |
| | さらに | N3 | N3 | N3 |
| | しかも | N2 | N3 | N3 |
| | そのうえ | N2 | N2 | N2 |
| | おまけに | N2 | — | N2 |
| **Song song** `song-song` 並立 | また | — | N5 | N5 |
| | および | N2 | N1 | N2 |
| **Đối lập, lựa chọn** `doi-lap-lua-chon` 対比・選択 | または | N4 | N4 | N4 |
| | それとも | N3 | N3 | N3 |
| | あるいは | N2 | N3 | N3 |
| | 一方 | N2 | N3 | N3 |
| **Giải thích, tóm lại** `giai-thich` 説明 | つまり | — | N3 | N3 |
| | すなわち | N3 | N3 | N3 |
| | 要するに | — | N3 | N3 |
| | なぜなら | N3 | N3 | N3 |
| | というのは | N3 | — | N3 |
| | 結局 | — | N3 | N3 |
| **Bổ sung điều kiện, ngoại lệ** `bo-sung-dieu-kien` 補足 | ただ | — | N3 | N3 |
| | ただし | — | N2 | N2 |
| | もっとも | N2 | N3 | N3 |
| | なお | N2 | N1 | N2 |
| | ちなみに | N2 | — | N2 |
| **Chuyển ý** `chuyen-y` 転換 | では / じゃあ | N5 | N5 | N5 |
| | ところで | N3 | N3 | N3 |
| | さて | N3 | N3 | N3 |
| | それなら | N2 | — | N2 |
| **Ví dụ** `vi-du` 例示 | 例えば | N3 | N4 | N4 |
| **Trình tự** `thu-tu` 順序 | それから | N5 | N5 | N5 |
| | まず | — | N3 | N3 |
| | 次に | — | — | N4 (tự đánh giá) |
| | 最後に | — | — | N4 (tự đánh giá) |

Tổng 44 từ (では/じゃあ là một mục). Các nhãn trên Jisho gộp nhiều nghĩa của cùng một từ, nên đây là ước lượng; danh sách có thể chỉnh khi viết nội dung, miễn tuân theo quy tắc chọn cấp.

Nguồn:
- JLPT Sensei — Complete Japanese Conjunctions List: https://jlptsensei.com/complete-japanese-conjunctions-list/
- Jisho.org (API `api/v1/search/words`, trường `jlpt`)

## Importer

Lệnh mới, không cần bộ docx:

```
dotnet run --project src/Japan.Importer -- connectors
```

- Đọc mọi `content/connectors/*.json`.
- Kiểm tra, báo **tất cả** lỗi rồi thoát mã 1, không ghi đè file cũ:
  - JSON hỏng hoặc thiếu trường bắt buộc (`word`, `reading`, `meaning`, `explanation`).
  - `id` nhóm khác tên file; trùng `order` giữa các nhóm.
  - `id` từ nối trùng hoặc sai định dạng.
  - `level` / `register` không hợp lệ.
  - `targetId` không trỏ tới từ nối nào.
  - Từ nối có ít hơn 2 ví dụ.
- Sinh furigana cho mọi ví dụ bằng `NMeCabFuriganaGenerator` (dùng `content/furigana-overrides.json`).
- Ghi `data/connectors.json` qua file tạm + move, giống `ImportPipeline`.
- Lệnh cũ (không có `connectors`) giữ nguyên hành vi.

Dữ liệu ra (`ConnectorBook`): `groups` sắp theo `order`, trong mỗi nhóm từ nối sắp theo cấp N5→N2 rồi theo thứ tự trong file; kèm `generatedAt`.

## Core (`Japan.Core`)

- Model: `ConnectorBook(Groups, GeneratedAt)`, `ConnectorGroup(Id, Order, Title, JaTitle, Description, Connectors)`, `Connector(Id, Word, Reading, Level, Meaning, Register, Explanation, Usage, Pitfalls, Comparisons, Examples)`. Dùng lại `Comparison` và `Example` sẵn có.
- `JlptLevel` (N5, N4, N3, N2) và `Register` là enum, ghi ra JSON dạng chuỗi.
- `ConnectorJson`: serialize/deserialize, cùng options với `GrammarJson`.
- `ConnectorSearch`: chuẩn hoá bằng `TextNormalizer`; trọng số: từ + cách đọc 100, nghĩa 50, giải thích / cách dùng / lỗi / so sánh 20, ví dụ 10. Trả `ConnectorHit(Connector, Field, Snippet)`.

## Web (`Japan.Web`)

### Nạp dữ liệu

- Tách phần đọc file + `FileSystemWatcher` + debounce + retry trong `GrammarRepository` ra lớp dùng chung `ReloadingJsonFile<T>`. `GrammarRepository` dùng lại lớp này, hành vi không đổi.
- `ConnectorRepository` (singleton) đọc `ConnectorData:Path`, mặc định `../../data/connectors.json`. Cung cấp: danh sách nhóm, lọc theo cấp, `Locate(id)` trả (từ, trước, sau) theo thứ tự phẳng, `Search(q)`, `LoadError`.
- Thiếu hoặc hỏng `connectors.json` chỉ ảnh hưởng các trang từ nối; phần ngữ pháp chạy bình thường.

### Trang

- **`/tu-noi`** — các nhóm theo `order`: tiêu đề (kèm `jaTitle`), mô tả, thẻ từ nối (từ, cách đọc, nhãn cấp, nghĩa). Thanh lọc *Tất cả · N5 · N4 · N3 · N2* bằng link `?cap=N3`; ẩn nhóm không còn từ. Giá trị `cap` không hợp lệ coi như *Tất cả*.
- **`/tu-noi/{id}`** — từ, cách đọc, cấp, sắc thái, nghĩa, giải thích, cách dùng, lỗi hay gặp, so sánh (`targetId` thành link), ví dụ có furigana (dùng `_Examples`). Trước/sau bằng nút và phím ← / →. Id sai → 404.
- Header: thêm link "Từ nối"; placeholder ô tìm kiếm thành "Tìm ngữ pháp, từ nối…". Trang mục lục: thêm một ô dẫn sang `/tu-noi`.

### Tìm kiếm

`/timkiem` hiện hai nhóm kết quả: "Ngữ pháp" (như cũ) và "Từ nối" (partial mới). Nhóm nào không có kết quả thì ẩn.

## Deploy

`Dockerfile` và `Dockerfile.vercel`: chép thêm `data/connectors.json` vào `./data/`, đặt `ENV ConnectorData__Path=data/connectors.json`. README: thêm mục cập nhật từ nối.

## Test (xUnit, theo cấu trúc `tests/Japan.Tests` hiện có)

- **Importer:** mỗi loại lỗi ở mục Importer có một test báo đúng lỗi và không ghi file; trường hợp hợp lệ ghi file với furigana trên ví dụ (dùng generator giả như test hiện có); sắp xếp nhóm và từ đúng quy tắc.
- **Core:** `ConnectorSearch` tìm được bằng tiếng Việt không dấu (`nhung ma`), kana (`しかし`, `シカシ`), xếp đúng trọng số; round-trip `ConnectorJson`.
- **Web:** `/tu-noi` hiện đủ nhóm; `?cap=N3` chỉ còn từ N3; `cap` sai → hiện tất cả; `/tu-noi/{id}` đúng nội dung, id sai → 404; `/timkiem` hiện cả hai nhóm; thiếu `connectors.json` → `/tu-noi` báo lỗi còn `/` vẫn chạy.
- **Dữ liệu thật:** đọc `data/connectors.json` trong repo, kiểm tra hợp lệ (cùng các quy tắc của importer).
