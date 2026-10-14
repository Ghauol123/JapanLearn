# Mục "Từ nối" N5–N2 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm mục tra cứu 44 từ nối (接続詞) N5–N2, chia theo chức năng, lọc theo cấp, có trang chi tiết và nằm trong tìm kiếm chung.

**Architecture:** Nội dung viết tay trong `content/connectors/*.json` → lệnh importer `connectors` kiểm tra, sắp xếp, sinh furigana và ghi `data/connectors.json` → web nạp file đó qua `ConnectorRepository` (dùng chung cơ chế tự nạp lại với `GrammarRepository`) và hiển thị ở `/tu-noi`, `/tu-noi/{id}`, `/timkiem`.

**Tech Stack:** .NET 10, ASP.NET Core Razor Pages, Bootstrap 5, System.Text.Json, NMeCab (furigana), xUnit 2.9 + `Microsoft.AspNetCore.Mvc.Testing`.

**Spec:** `docs/superpowers/specs/2026-10-14-connectors-design.md`

## Global Constraints

- Chỉ 接続詞, cấp N5–N2; đúng 44 mục, đúng cấp như bảng trong spec (người dùng đã duyệt, không tự đổi cấp).
- URL: `/tu-noi`, `/tu-noi/{id}`, lọc bằng query `?cap=N5|N4|N3|N2` (không phân biệt hoa thường; giá trị khác = Tất cả).
- `level`: `N5` | `N4` | `N3` | `N2`. `register`: `spoken` | `written` | `both`.
- `id` từ nối và id nhóm: `^[a-z0-9]+(-[a-z0-9]+)*$`, duy nhất. Mỗi từ nối ≥ 2 ví dụ.
- Lệnh importer: `dotnet run --project src/Japan.Importer -- connectors`; lỗi → thoát mã 1, không ghi đè file cũ.
- Cấu hình web: section `ConnectorData:Path`, mặc định `../../data/connectors.json`; env `ConnectorData__Path` trong Docker.
- Không thêm gói NuGet mới. Toàn bộ chữ trên giao diện bằng tiếng Việt.
- Nội dung giải thích và ví dụ tự viết, không chép nguyên văn từ nguồn nào.
- Commit theo kiểu `feat: …` / `test: …` / `docs: …`, kết thúc message bằng dòng `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Chạy test: `dotnet test Japan.slnx` (hiện có 121 test, tất cả phải tiếp tục pass).

## Review Focus

- `?cap=n3`, `?cap= N3 ` → phải lọc như `N3`; `?cap=3`, `?cap=N1` → hiện tất cả (test ở Task 1 và Task 8).
- `comparisons[].targetId` trỏ về chính từ đó → importer phải báo lỗi, không tạo link vòng (test ở Task 3).
- `data/connectors.json` bị sửa hỏng khi web đang chạy → giữ dữ liệu cũ, phần ngữ pháp không bị ảnh hưởng (test ở Task 6).
- Từ có kanji (一方, 要するに, 例えば…) → thẻ và trang chi tiết phải hiện cách đọc (test ở Task 7 và Task 8).
- Sửa `content/connectors/` mà quên chạy importer → bản deploy sẽ dùng dữ liệu cũ; test so khớp content với data phải fail (test ở Task 10).

---

## File Structure

**Core (`src/Japan.Core`)**
- Create `Connectors.cs` — enum `JlptLevel`, `ConnectorRegister`, helper `JlptLevels`, record `ConnectorBook`, `ConnectorGroup`, `Connector`.
- Create `ConnectorJson.cs` — serialize/deserialize `ConnectorBook`.
- Create `ConnectorValidator.cs` — mọi quy tắc nội dung (dùng chung cho importer, web và test dữ liệu thật).
- Create `ConnectorSearch.cs` — tìm kiếm từ nối, `ConnectorHit`.
- Create `SearchSnippet.cs` — cắt đoạn trích, tách ra từ `GrammarSearch.cs`.
- Modify `GrammarSearch.cs` — dùng `SearchSnippet`.

**Importer (`src/Japan.Importer`)**
- Create `Assembly/ConnectorLoader.cs` — đọc `content/connectors/*.json`, lỗi cấp file + gọi validator.
- Create `ConnectorPipeline.cs` — load → sắp xếp → furigana → ghi file.
- Create `OutputFile.cs` — ghi file qua file tạm (tách từ `ImportPipeline`).
- Modify `ImportPipeline.cs`, `Furigana/FuriganaApplier.cs`, `Program.cs`.

**Web (`src/Japan.Web`)**
- Create `Services/ReloadingJsonFile.cs` — đọc file + watcher + debounce + retry (tách từ `GrammarRepository`).
- Modify `Services/GrammarRepository.cs` — dùng `ReloadingJsonFile`.
- Create `Services/ConnectorDataOptions.cs`, `Services/ConnectorRepository.cs`.
- Create `Rendering/ConnectorLabels.cs`.
- Create `Pages/Connectors.cshtml(.cs)` (`/tu-noi`), `Pages/Connector.cshtml(.cs)` (`/tu-noi/{id}`).
- Modify `Program.cs`, `appsettings.json`, `Pages/Shared/_Layout.cshtml`, `Pages/Index.cshtml(.cs)`, `Pages/Search.cshtml(.cs)`, `Pages/Shared/_SearchResults.cshtml`, `wwwroot/css/site.css`.

**Nội dung / deploy**
- Create `content/connectors/*.json` (10 file), `data/connectors.json` (sinh ra).
- Modify `Dockerfile`, `Dockerfile.vercel`, `README.md`.

**Test (`tests/Japan.Tests`)**
- Create `TestConnectors.cs`, `Core/ConnectorJsonTests.cs`, `Core/ConnectorSearchTests.cs`, `Core/ConnectorValidatorTests.cs`, `Importer/ConnectorLoaderTests.cs`, `Importer/ConnectorPipelineTests.cs`, `Web/ReloadingJsonFileTests.cs`, `Web/ConnectorRepositoryTests.cs`, `Web/ConnectorPagesTests.cs`, `Data/ConnectorDataTests.cs`.
- Modify `Web/GrammarWebFactory.cs`, `Web/SearchPageTests.cs`.

---

### Task 1: Model và JSON cho từ nối

**Files:**
- Create: `src/Japan.Core/Connectors.cs`
- Create: `src/Japan.Core/ConnectorJson.cs`
- Create: `tests/Japan.Tests/TestConnectors.cs`
- Test: `tests/Japan.Tests/Core/ConnectorJsonTests.cs`

**Interfaces:**
- Consumes: `Comparison(string Pattern, string? TargetId, string Difference)`, `Example(string Japanese, string Vietnamese, string? Ruby = null)`, `GrammarJson.Options` (đã có).
- Produces:
  - `enum JlptLevel { N5, N4, N3, N2 }` (JSON dạng chuỗi `"N5"`…)
  - `enum ConnectorRegister { Spoken, Written, Both }` (JSON `"spoken"`, `"written"`, `"both"`)
  - `static class JlptLevels { IReadOnlyList<JlptLevel> All; bool TryParse(string? text, out JlptLevel level) }`
  - `record ConnectorBook(IReadOnlyList<ConnectorGroup> Groups, DateTimeOffset GeneratedAt)`
  - `record ConnectorGroup(string Id, int Order, string Title, string JaTitle, string Description, IReadOnlyList<Connector> Connectors)`
  - `record Connector(string Id, string Word, string Reading, JlptLevel Level, string Meaning, ConnectorRegister Register, string Explanation, IReadOnlyList<string> Usage, IReadOnlyList<string> Pitfalls, IReadOnlyList<Comparison> Comparisons, IReadOnlyList<Example> Examples)`
  - `static class ConnectorJson { string Serialize(ConnectorBook); ConnectorBook Deserialize(string json) }`
  - Test fixture `TestConnectors.Sample()`: nhóm `thuan-tiep` (order 1: `dakara` N4, `shitagatte` N3) và `nghich-tiep` (order 2: `shikashi` N5).

- [ ] **Step 1: Viết fixture và test thất bại**

`tests/Japan.Tests/TestConnectors.cs`:

```csharp
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
```

`tests/Japan.Tests/Core/ConnectorJsonTests.cs`:

```csharp
using System.Text.Json;
using Japan.Core;

namespace Japan.Tests.Core;

public class ConnectorJsonTests
{
    [Fact]
    public void RoundTrip_KeepsContent_AndWritesEnumsAsStrings()
    {
        var json = ConnectorJson.Serialize(TestConnectors.Sample());

        var again = ConnectorJson.Serialize(ConnectorJson.Deserialize(json));

        Assert.Equal(json, again);
        Assert.Contains("\"level\": \"N3\"", json);
        Assert.Contains("\"register\": \"written\"", json);
        Assert.Contains("だから", json);
    }

    [Fact]
    public void Deserialize_UnknownLevel_Throws()
    {
        var json = ConnectorJson.Serialize(TestConnectors.Sample()).Replace("\"N4\"", "\"N9\"");

        Assert.Throws<JsonException>(() => ConnectorJson.Deserialize(json));
    }

    [Theory]
    [InlineData("N3", true, JlptLevel.N3)]
    [InlineData("n3", true, JlptLevel.N3)]
    [InlineData(" N2 ", true, JlptLevel.N2)]
    [InlineData("N5", true, JlptLevel.N5)]
    [InlineData("3", false, default(JlptLevel))]
    [InlineData("N1", false, default(JlptLevel))]
    [InlineData("", false, default(JlptLevel))]
    [InlineData(null, false, default(JlptLevel))]
    public void TryParse_AcceptsOnlyLevelNames(string? text, bool ok, JlptLevel expected)
    {
        Assert.Equal(ok, JlptLevels.TryParse(text, out var level));
        if (ok) Assert.Equal(expected, level);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorJsonTests"`
Expected: build FAIL — `The type or namespace name 'Connector' could not be found`.

- [ ] **Step 3: Viết code**

`src/Japan.Core/Connectors.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Japan.Core;

[JsonConverter(typeof(JsonStringEnumConverter<JlptLevel>))]
public enum JlptLevel
{
    N5,
    N4,
    N3,
    N2,
}

[JsonConverter(typeof(JsonStringEnumConverter<ConnectorRegister>))]
public enum ConnectorRegister
{
    [JsonStringEnumMemberName("spoken")] Spoken,
    [JsonStringEnumMemberName("written")] Written,
    [JsonStringEnumMemberName("both")] Both,
}

public static class JlptLevels
{
    public static IReadOnlyList<JlptLevel> All { get; } = [JlptLevel.N5, JlptLevel.N4, JlptLevel.N3, JlptLevel.N2];

    public static bool TryParse(string? text, out JlptLevel level)
    {
        var trimmed = text?.Trim();
        foreach (var candidate in All)
        {
            if (string.Equals(candidate.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                level = candidate;
                return true;
            }
        }

        level = default;
        return false;
    }
}

public sealed record ConnectorBook(IReadOnlyList<ConnectorGroup> Groups, DateTimeOffset GeneratedAt);

public sealed record ConnectorGroup(
    string Id,
    int Order,
    string Title,
    string JaTitle,
    string Description,
    IReadOnlyList<Connector> Connectors);

public sealed record Connector(
    string Id,
    string Word,
    string Reading,
    JlptLevel Level,
    string Meaning,
    ConnectorRegister Register,
    string Explanation,
    IReadOnlyList<string> Usage,
    IReadOnlyList<string> Pitfalls,
    IReadOnlyList<Comparison> Comparisons,
    IReadOnlyList<Example> Examples);
```

`src/Japan.Core/ConnectorJson.cs`:

```csharp
using System.Text.Json;

namespace Japan.Core;

public static class ConnectorJson
{
    public static string Serialize(ConnectorBook book) => JsonSerializer.Serialize(book, GrammarJson.Options);

    public static ConnectorBook Deserialize(string json) =>
        JsonSerializer.Deserialize<ConnectorBook>(json, GrammarJson.Options)
        ?? throw new InvalidDataException("connectors.json rỗng.");
}
```

- [ ] **Step 4: Chạy test, xác nhận pass**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorJsonTests"`
Expected: PASS (10 test).

- [ ] **Step 5: Commit**

```bash
git add src/Japan.Core/Connectors.cs src/Japan.Core/ConnectorJson.cs tests/Japan.Tests/TestConnectors.cs tests/Japan.Tests/Core/ConnectorJsonTests.cs
git commit -m "feat: add the connector model and JSON format"
```

---

### Task 2: Tìm kiếm từ nối

**Files:**
- Create: `src/Japan.Core/SearchSnippet.cs`
- Create: `src/Japan.Core/ConnectorSearch.cs`
- Modify: `src/Japan.Core/GrammarSearch.cs` (bỏ hằng `SnippetLength`, `SnippetLead` và hàm `Snippet`, gọi `SearchSnippet.Make`)
- Test: `tests/Japan.Tests/Core/ConnectorSearchTests.cs`

**Interfaces:**
- Consumes: `ConnectorBook`, `Connector` (Task 1), `TextNormalizer.Normalize(string?)`, `TextNormalizer.FindIn(string raw, string normalizedQuery)`.
- Produces:
  - `record ConnectorHit(Connector Connector, string Field, string Snippet)`
  - `class ConnectorSearch(ConnectorBook book) { IReadOnlyList<ConnectorHit> Search(string? query, int limit = 50) }`
  - Tên field: `"Từ nối"` (100), `"Nghĩa"` (50), `"Giải thích"` (20), `"So sánh"` (20), `"Ví dụ"` (10).

- [ ] **Step 1: Viết test thất bại**

`tests/Japan.Tests/Core/ConnectorSearchTests.cs`:

```csharp
using Japan.Core;

namespace Japan.Tests.Core;

public class ConnectorSearchTests
{
    private readonly ConnectorSearch _search = new(TestConnectors.Sample());

    [Theory]
    [InlineData("cho nen")]
    [InlineData("CHO NÊN")]
    public void Search_VietnameseIgnoresDiacriticsAndCase(string query)
    {
        var hit = Assert.Single(_search.Search(query));

        Assert.Equal("dakara", hit.Connector.Id);
        Assert.Equal("Nghĩa", hit.Field);
    }

    [Theory]
    [InlineData("しかし")]
    [InlineData("シカシ")]
    public void Search_WordMatchesInKanaVariants(string query)
    {
        var hit = _search.Search(query)[0];

        Assert.Equal("shikashi", hit.Connector.Id);
        Assert.Equal("Từ nối", hit.Field);
    }

    [Fact]
    public void Search_RanksWordAboveComparison()
    {
        var hits = _search.Search("したがって");

        Assert.Equal(["shitagatte", "dakara"], hits.Select(h => h.Connector.Id));
        Assert.Equal("So sánh", hits[1].Field);
    }

    [Fact]
    public void Search_MatchesReadingWhenWordIsKanji()
    {
        var ippou = TestConnectors.Sample().Groups[0].Connectors[0] with { Id = "ippou", Word = "一方", Reading = "いっぽう" };
        var search = new ConnectorSearch(new ConnectorBook(
            [new ConnectorGroup("doi-lap", 1, "Đối lập", "対比", "Mô tả.", [ippou])], DateTimeOffset.UnixEpoch));

        var hit = Assert.Single(search.Search("イッポウ"));

        Assert.Equal("ippou", hit.Connector.Id);
        Assert.Equal("Từ nối", hit.Field);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Search_EmptyQuery_ReturnsNothing(string? query) => Assert.Empty(_search.Search(query));
}
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorSearchTests"`
Expected: build FAIL — `ConnectorSearch` not found.

- [ ] **Step 3: Tách `SearchSnippet` và viết `ConnectorSearch`**

`src/Japan.Core/SearchSnippet.cs` (chuyển nguyên logic từ `GrammarSearch.Snippet`):

```csharp
namespace Japan.Core;

internal static class SearchSnippet
{
    private const int Length = 160;
    private const int Lead = 40;

    public static string Make(string raw, string normalizedQuery)
    {
        if (raw.Length <= Length) return raw;

        var match = TextNormalizer.FindIn(raw, normalizedQuery)?.Start ?? 0;
        var start = match < Length - Lead
            ? 0
            : Math.Min(match - Lead, raw.Length - (Length - 1));
        var end = Math.Min(raw.Length, start + Length - (start > 0 ? 2 : 1));
        return (start > 0 ? "…" : "") + raw[start..end] + (end < raw.Length ? "…" : "");
    }
}
```

Trong `src/Japan.Core/GrammarSearch.cs`: xoá hai dòng `private const int SnippetLength = 160;` và `private const int SnippetLead = 40;`, xoá toàn bộ hàm `private static string Snippet(...)`, và đổi `Snippet(x.Best.Raw, q)` thành `SearchSnippet.Make(x.Best.Raw, q)`.

`src/Japan.Core/ConnectorSearch.cs`:

```csharp
namespace Japan.Core;

public sealed record ConnectorHit(Connector Connector, string Field, string Snippet);

public sealed class ConnectorSearch
{
    private readonly record struct Field(int Weight, string Name, string Raw, string Normalized);

    private readonly IReadOnlyList<(Connector Connector, IReadOnlyList<Field> Fields)> _entries;

    public ConnectorSearch(ConnectorBook book)
    {
        _entries = book.Groups
            .SelectMany(g => g.Connectors)
            .Select(c => (c, (IReadOnlyList<Field>)Fields(c).ToList()))
            .ToList();
    }

    public IReadOnlyList<ConnectorHit> Search(string? query, int limit = 50)
    {
        var q = TextNormalizer.Normalize(query);
        if (q.Length == 0) return [];

        return _entries
            .Select((entry, order) => (entry.Connector, Order: order,
                Best: entry.Fields.Where(f => f.Normalized.Contains(q, StringComparison.Ordinal))
                                  .OrderByDescending(f => f.Weight)
                                  .FirstOrDefault()))
            .Where(x => x.Best.Weight > 0)
            .OrderByDescending(x => x.Best.Weight)
            .ThenBy(x => x.Order)
            .Take(limit)
            .Select(x => new ConnectorHit(x.Connector, x.Best.Name, SearchSnippet.Make(x.Best.Raw, q)))
            .ToList();
    }

    private static IEnumerable<Field> Fields(Connector c)
    {
        Field F(int weight, string name, string raw) => new(weight, name, raw, TextNormalizer.Normalize(raw));

        yield return F(100, "Từ nối", c.Word);
        if (c.Reading != c.Word) yield return F(100, "Từ nối", c.Reading);
        yield return F(50, "Nghĩa", c.Meaning);
        yield return F(20, "Giải thích", c.Explanation);
        foreach (var s in c.Usage.Concat(c.Pitfalls)) yield return F(20, "Giải thích", s);
        foreach (var cmp in c.Comparisons) yield return F(20, "So sánh", $"{cmp.Pattern}: {cmp.Difference}");
        foreach (var ex in c.Examples) yield return F(10, "Ví dụ", $"{ex.Japanese} — {ex.Vietnamese}");
    }
}
```

- [ ] **Step 4: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS — gồm `ConnectorSearchTests` và mọi `GrammarSearchTests` cũ (xác nhận việc tách snippet không đổi hành vi).

- [ ] **Step 5: Commit**

```bash
git add src/Japan.Core/SearchSnippet.cs src/Japan.Core/ConnectorSearch.cs src/Japan.Core/GrammarSearch.cs tests/Japan.Tests/Core/ConnectorSearchTests.cs
git commit -m "feat: search connectors by word, reading, meaning and examples"
```

---

### Task 3: Kiểm tra nội dung và đọc file nguồn

**Files:**
- Create: `src/Japan.Core/ConnectorValidator.cs`
- Create: `src/Japan.Importer/Assembly/ConnectorLoader.cs`
- Test: `tests/Japan.Tests/Core/ConnectorValidatorTests.cs`
- Test: `tests/Japan.Tests/Importer/ConnectorLoaderTests.cs`

**Interfaces:**
- Consumes: model Task 1.
- Produces:
  - `static class ConnectorValidator { const int MinExamples = 2; IReadOnlyList<string> Validate(IReadOnlyList<ConnectorGroup>? groups) }` — trả danh sách lỗi (rỗng = hợp lệ); an toàn với giá trị `null` trong dữ liệu.
  - `record ConnectorLoadResult(IReadOnlyList<ConnectorGroup> Groups, IReadOnlyList<string> Errors)`
  - `static class ConnectorLoader { ConnectorLoadResult Load(string directory) }` — chuẩn hoá list `null` thành `[]`, kiểm tra JSON và id nhóm = tên file, rồi gọi `ConnectorValidator.Validate`.

- [ ] **Step 1: Viết test validator thất bại**

`tests/Japan.Tests/Core/ConnectorValidatorTests.cs`:

```csharp
using Japan.Core;

namespace Japan.Tests.Core;

public class ConnectorValidatorTests
{
    private static IReadOnlyList<ConnectorGroup> Groups => TestConnectors.Sample().Groups;

    private static IReadOnlyList<ConnectorGroup> WithFirst(Func<Connector, Connector> change)
    {
        var g = Groups;
        return [g[0] with { Connectors = [change(g[0].Connectors[0]), .. g[0].Connectors.Skip(1)] }, .. g.Skip(1)];
    }

    private static string Single(IReadOnlyList<ConnectorGroup> groups) => Assert.Single(ConnectorValidator.Validate(groups));

    [Fact]
    public void Sample_IsValid() => Assert.Empty(ConnectorValidator.Validate(Groups));

    [Fact]
    public void NoGroups_IsError()
    {
        Assert.NotEmpty(ConnectorValidator.Validate(null));
        Assert.NotEmpty(ConnectorValidator.Validate([]));
    }

    [Fact]
    public void DuplicateConnectorIdAcrossGroups_IsError()
    {
        var g = Groups;
        var groups = new[] { g[0], g[1] with { Connectors = [.. g[1].Connectors, g[0].Connectors[0]] } };

        Assert.Contains("trùng id", Single(groups));
    }

    [Theory]
    [InlineData("Da Kara")]
    [InlineData("dakara-")]
    [InlineData("")]
    public void BadConnectorId_IsError(string id) =>
        Assert.Contains(ConnectorValidator.Validate(WithFirst(c => c with { Id = id })), e => e.Contains("id phải là"));

    [Fact]
    public void DuplicateGroupOrder_IsError()
    {
        var g = Groups;

        Assert.Contains("trùng order", Single([g[0], g[1] with { Order = g[0].Order }]));
    }

    [Fact]
    public void UndefinedLevelOrRegister_IsError()
    {
        Assert.Contains("level", Single(WithFirst(c => c with { Level = (JlptLevel)99 })));
        Assert.Contains("register", Single(WithFirst(c => c with { Register = (ConnectorRegister)99 })));
    }

    [Fact]
    public void MissingMeaning_IsError() =>
        Assert.Contains("\"meaning\"", Single(WithFirst(c => c with { Meaning = " " })));

    [Fact]
    public void FewerThanTwoExamples_IsError() =>
        Assert.Contains("ít nhất 2 ví dụ", Single(WithFirst(c => c with { Examples = [c.Examples[0]] })));

    [Fact]
    public void UnknownTargetId_IsError() =>
        Assert.Contains("\"khong-co\"", Single(WithFirst(c => c with { Comparisons = [new Comparison("x", "khong-co", "y")] })));

    [Fact]
    public void TargetIdPointingToItself_IsError() =>
        Assert.Contains("chính nó", Single(WithFirst(c => c with { Comparisons = [new Comparison("x", c.Id, "y")] })));
}
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorValidatorTests"`
Expected: build FAIL — `ConnectorValidator` not found.

- [ ] **Step 3: Viết `ConnectorValidator`**

`src/Japan.Core/ConnectorValidator.cs`:

```csharp
using System.Text.RegularExpressions;

namespace Japan.Core;

public static partial class ConnectorValidator
{
    public const int MinExamples = 2;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex IdRegex();

    public static IReadOnlyList<string> Validate(IReadOnlyList<ConnectorGroup>? groups)
    {
        var errors = new List<string>();
        if (groups is null || groups.Count == 0)
        {
            errors.Add("Không có nhóm từ nối nào.");
            return errors;
        }

        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        var orders = new HashSet<int>();
        var all = new List<(string Where, Connector Connector)>();

        foreach (var g in groups)
        {
            var name = string.IsNullOrWhiteSpace(g.Id) ? "(nhóm không có id)" : g.Id;
            if (string.IsNullOrWhiteSpace(g.Id) || !IdRegex().IsMatch(g.Id))
                errors.Add($"{name}: id phải là chữ thường không dấu, nối bằng '-'");
            else if (!groupIds.Add(g.Id))
                errors.Add($"{name}: trùng id nhóm");
            if (!orders.Add(g.Order)) errors.Add($"{name}: trùng order {g.Order} với nhóm khác");
            if (string.IsNullOrWhiteSpace(g.Title)) errors.Add($"{name}: thiếu \"title\"");
            if (string.IsNullOrWhiteSpace(g.JaTitle)) errors.Add($"{name}: thiếu \"jaTitle\"");
            if (string.IsNullOrWhiteSpace(g.Description)) errors.Add($"{name}: thiếu \"description\"");
            if (g.Connectors is null || g.Connectors.Count == 0)
            {
                errors.Add($"{name}: không có từ nối nào");
                continue;
            }

            foreach (var c in g.Connectors)
                all.Add(($"{name}/{(string.IsNullOrWhiteSpace(c.Id) ? "(không có id)" : c.Id)}", c));
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (where, c) in all)
        {
            if (string.IsNullOrWhiteSpace(c.Id) || !IdRegex().IsMatch(c.Id))
                errors.Add($"{where}: id phải là chữ thường không dấu, nối bằng '-'");
            else if (!ids.Add(c.Id))
                errors.Add($"{where}: trùng id từ nối");

            foreach (var (field, value) in new[] { ("word", c.Word), ("reading", c.Reading), ("meaning", c.Meaning), ("explanation", c.Explanation) })
            {
                if (string.IsNullOrWhiteSpace(value)) errors.Add($"{where}: thiếu \"{field}\"");
            }

            if (!Enum.IsDefined(c.Level)) errors.Add($"{where}: level phải là N5, N4, N3 hoặc N2");
            if (!Enum.IsDefined(c.Register)) errors.Add($"{where}: register phải là spoken, written hoặc both");

            var examples = c.Examples ?? [];
            if (examples.Count < MinExamples) errors.Add($"{where}: cần ít nhất {MinExamples} ví dụ");
            if (examples.Any(e => string.IsNullOrWhiteSpace(e.Japanese) || string.IsNullOrWhiteSpace(e.Vietnamese)))
                errors.Add($"{where}: ví dụ thiếu câu tiếng Nhật hoặc bản dịch");
        }

        foreach (var (where, c) in all)
        {
            foreach (var cmp in c.Comparisons ?? [])
            {
                if (cmp.TargetId is null) continue;
                if (cmp.TargetId == c.Id) errors.Add($"{where}: so sánh trỏ về chính nó");
                else if (!ids.Contains(cmp.TargetId)) errors.Add($"{where}: targetId \"{cmp.TargetId}\" không tồn tại");
            }
        }

        return errors;
    }
}
```

- [ ] **Step 4: Chạy test validator, xác nhận pass**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorValidatorTests"`
Expected: PASS.

- [ ] **Step 5: Viết test loader thất bại**

`tests/Japan.Tests/Importer/ConnectorLoaderTests.cs`:

```csharp
using System.Text.Json;
using Japan.Core;
using Japan.Importer.Assembly;

namespace Japan.Tests.Importer;

public sealed class ConnectorLoaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("connectors-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void Write(ConnectorGroup group, string? fileName = null) =>
        File.WriteAllText(Path.Combine(_dir, (fileName ?? group.Id) + ".json"), JsonSerializer.Serialize(group, GrammarJson.Options));

    private void WriteSample()
    {
        foreach (var g in TestConnectors.Sample().Groups) Write(g);
    }

    [Fact]
    public void Load_ValidFiles_ReturnsGroups()
    {
        WriteSample();

        var result = ConnectorLoader.Load(_dir);

        Assert.Empty(result.Errors);
        Assert.Equal(["nghich-tiep", "thuan-tiep"], result.Groups.Select(g => g.Id));
    }

    [Fact]
    public void Load_MissingLists_BecomeEmpty()
    {
        WriteSample();
        File.WriteAllText(Path.Combine(_dir, "nghich-tiep.json"), """
            {
              "id": "nghich-tiep", "order": 2, "title": "Nghịch tiếp", "jaTitle": "逆接", "description": "Mô tả.",
              "connectors": [
                { "id": "demo", "word": "でも", "reading": "でも", "level": "N5", "meaning": "nhưng", "register": "spoken",
                  "explanation": "Giải thích.",
                  "examples": [ { "japanese": "a。でも、b。", "vietnamese": "x" }, { "japanese": "c。でも、d。", "vietnamese": "y" } ] }
              ]
            }
            """);

        var result = ConnectorLoader.Load(_dir);

        Assert.Empty(result.Errors);
        var demo = result.Groups.Single(g => g.Id == "nghich-tiep").Connectors[0];
        Assert.Empty(demo.Usage);
        Assert.Empty(demo.Pitfalls);
        Assert.Empty(demo.Comparisons);
    }

    [Fact]
    public void Load_BrokenJson_ReportsFile()
    {
        WriteSample();
        File.WriteAllText(Path.Combine(_dir, "hong.json"), "{ hỏng");

        Assert.Contains(ConnectorLoader.Load(_dir).Errors, e => e.Contains("hong.json"));
    }

    [Fact]
    public void Load_GroupIdDifferentFromFileName_IsError()
    {
        WriteSample();
        Write(TestConnectors.Sample().Groups[1] with { Order = 9 }, "ten-khac");

        Assert.Contains(ConnectorLoader.Load(_dir).Errors, e => e.Contains("ten-khac.json") && e.Contains("nghich-tiep"));
    }

    [Fact]
    public void Load_RunsValidator()
    {
        var group = TestConnectors.Sample().Groups[1];
        Write(group with { Connectors = [group.Connectors[0] with { Examples = [group.Connectors[0].Examples[0]] }] });

        Assert.Contains(ConnectorLoader.Load(_dir).Errors, e => e.Contains("ít nhất 2 ví dụ"));
    }

    [Fact]
    public void Load_EmptyOrMissingDirectory_IsError()
    {
        Assert.NotEmpty(ConnectorLoader.Load(_dir).Errors);
        Assert.NotEmpty(ConnectorLoader.Load(Path.Combine(_dir, "khong-co")).Errors);
    }
}
```

- [ ] **Step 6: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorLoaderTests"`
Expected: build FAIL — `ConnectorLoader` not found.

- [ ] **Step 7: Viết `ConnectorLoader`**

`src/Japan.Importer/Assembly/ConnectorLoader.cs`:

```csharp
using System.Text.Json;
using Japan.Core;

namespace Japan.Importer.Assembly;

public sealed record ConnectorLoadResult(IReadOnlyList<ConnectorGroup> Groups, IReadOnlyList<string> Errors);

public static class ConnectorLoader
{
    public static ConnectorLoadResult Load(string directory)
    {
        var groups = new List<ConnectorGroup>();
        var errors = new List<string>();
        if (!Directory.Exists(directory))
        {
            errors.Add($"Không tìm thấy thư mục {directory}");
            return new ConnectorLoadResult(groups, errors);
        }

        var files = Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal).ToList();
        if (files.Count == 0)
        {
            errors.Add($"Không có file .json nào trong {directory}");
            return new ConnectorLoadResult(groups, errors);
        }

        foreach (var path in files)
        {
            var name = Path.GetFileName(path);
            var id = Path.GetFileNameWithoutExtension(path);

            ConnectorGroup? group;
            try
            {
                group = JsonSerializer.Deserialize<ConnectorGroup>(File.ReadAllText(path), GrammarJson.Options);
            }
            catch (JsonException ex)
            {
                errors.Add($"{name}: JSON không hợp lệ — {ex.Message}");
                continue;
            }

            if (group is null)
            {
                errors.Add($"{name}: file rỗng");
                continue;
            }

            if (group.Id != id) errors.Add($"{name}: \"id\" là \"{group.Id}\" nhưng tên file là \"{id}\"");
            groups.Add(Normalize(group));
        }

        if (groups.Count > 0) errors.AddRange(ConnectorValidator.Validate(groups));
        return new ConnectorLoadResult(groups, errors);
    }

    private static ConnectorGroup Normalize(ConnectorGroup group) => group with
    {
        Connectors = (group.Connectors ?? []).Select(c => c with
        {
            Usage = c.Usage ?? [],
            Pitfalls = c.Pitfalls ?? [],
            Comparisons = c.Comparisons ?? [],
            Examples = c.Examples ?? [],
        }).ToList(),
    };
}
```

- [ ] **Step 8: Chạy test, xác nhận pass**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorLoaderTests|FullyQualifiedName~ConnectorValidatorTests"`
Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add src/Japan.Core/ConnectorValidator.cs src/Japan.Importer/Assembly/ConnectorLoader.cs tests/Japan.Tests/Core/ConnectorValidatorTests.cs tests/Japan.Tests/Importer/ConnectorLoaderTests.cs
git commit -m "feat: validate and load connector content files"
```

---

### Task 4: Lệnh importer `connectors`

**Files:**
- Create: `src/Japan.Importer/OutputFile.cs`
- Create: `src/Japan.Importer/ConnectorPipeline.cs`
- Modify: `src/Japan.Importer/ImportPipeline.cs` (đoạn ghi file tạm, ~dòng 61-65)
- Modify: `src/Japan.Importer/Furigana/FuriganaApplier.cs`
- Modify: `src/Japan.Importer/Program.cs`
- Test: `tests/Japan.Tests/Importer/ConnectorPipelineTests.cs`

**Interfaces:**
- Consumes: `ConnectorLoader.Load` (Task 3), `ConnectorJson.Serialize`, `JlptLevels.All` (Task 1), `IFuriganaGenerator.ToRuby(string)`.
- Produces:
  - `static class OutputFile { void Write(string path, string content) }`
  - `FuriganaApplier.Apply(ConnectorBook book, IFuriganaGenerator generator) : ConnectorBook`
  - `static class ConnectorPipeline { int Run(string contentDirectory, string outputPath, TextWriter output, IFuriganaGenerator generator, TimeProvider? clock = null) }` — trả 0 khi ghi thành công, 1 khi có lỗi.

- [ ] **Step 1: Viết test thất bại**

`tests/Japan.Tests/Importer/ConnectorPipelineTests.cs`:

```csharp
using System.Text.Json;
using Japan.Core;
using Japan.Importer;
using Japan.Importer.Furigana;

namespace Japan.Tests.Importer;

public sealed class ConnectorPipelineTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("connector-import-").FullName;
    private string Content => Path.Combine(_root, "connectors");
    private string Output => Path.Combine(_root, "data", "connectors.json");

    public ConnectorPipelineTests()
    {
        Directory.CreateDirectory(Content);
        var sample = TestConnectors.Sample();
        var thuanTiep = sample.Groups[0];
        // Ghi ngược thứ tự cấp (N3 trước N4) để kiểm tra việc sắp xếp.
        var reversed = thuanTiep with { Connectors = [thuanTiep.Connectors[1], thuanTiep.Connectors[0]] };
        File.WriteAllText(Path.Combine(Content, "thuan-tiep.json"), JsonSerializer.Serialize(reversed, GrammarJson.Options));
        File.WriteAllText(Path.Combine(Content, "nghich-tiep.json"), JsonSerializer.Serialize(sample.Groups[1], GrammarJson.Options));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Run_WritesSortedBookWithFurigana()
    {
        var output = new StringWriter();

        var exit = ConnectorPipeline.Run(Content, Output, output, new FakeGenerator());

        Assert.Equal(0, exit);
        var book = ConnectorJson.Deserialize(File.ReadAllText(Output));
        Assert.Equal(["thuan-tiep", "nghich-tiep"], book.Groups.Select(g => g.Id));
        Assert.Equal(["dakara", "shitagatte"], book.Groups[0].Connectors.Select(c => c.Id));
        Assert.Equal("R:高い。しかし、品質はいい。", book.Groups[1].Connectors[0].Examples[0].Ruby);
        Assert.Contains("2 nhóm, 3 từ nối", output.ToString());
        Assert.Contains("N5: 1, N4: 1, N3: 1, N2: 0", output.ToString());
    }

    [Fact]
    public void Run_BadContent_FailsWithoutOverwritingOutput()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Output)!);
        File.WriteAllText(Output, "cũ");
        File.WriteAllText(Path.Combine(Content, "hong.json"), "{ hỏng");
        var output = new StringWriter();

        var exit = ConnectorPipeline.Run(Content, Output, output, new FakeGenerator());

        Assert.Equal(1, exit);
        Assert.Equal("cũ", File.ReadAllText(Output));
        Assert.Contains("hong.json", output.ToString());
    }

    private sealed class FakeGenerator : IFuriganaGenerator
    {
        public string ToRuby(string japanese) => "R:" + japanese;
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorPipelineTests"`
Expected: build FAIL — `ConnectorPipeline` not found.

- [ ] **Step 3: Tách `OutputFile` và dùng trong `ImportPipeline`**

`src/Japan.Importer/OutputFile.cs`:

```csharp
using System.Text;

namespace Japan.Importer;

public static class OutputFile
{
    public static void Write(string path, string content)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temp = full + ".tmp";
        File.WriteAllText(temp, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temp, full, overwrite: true);
    }
}
```

Trong `src/Japan.Importer/ImportPipeline.cs`, thay khối

```csharp
        var fullOutput = Path.GetFullPath(settings.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutput)!);
        var temp = fullOutput + ".tmp";
        File.WriteAllText(temp, GrammarJson.Serialize(book), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temp, fullOutput, overwrite: true);
```

bằng

```csharp
        OutputFile.Write(settings.OutputPath, GrammarJson.Serialize(book));
```

và xoá `using System.Text;` nếu không còn dùng.

- [ ] **Step 4: Thêm overload furigana cho `ConnectorBook`**

Thay toàn bộ `src/Japan.Importer/Furigana/FuriganaApplier.cs`:

```csharp
using Japan.Core;

namespace Japan.Importer.Furigana;

public static class FuriganaApplier
{
    public static GrammarBook Apply(GrammarBook book, IFuriganaGenerator generator) => book with
    {
        Lessons = book.Lessons.Select(l => l with
        {
            Points = l.Points.Select(p => p with
            {
                Examples = Fill(p.Examples, generator),
                Enrichment = p.Enrichment is null
                    ? null
                    : p.Enrichment with { Examples = Fill(p.Enrichment.Examples, generator) },
            }).ToList(),
        }).ToList(),
    };

    public static ConnectorBook Apply(ConnectorBook book, IFuriganaGenerator generator) => book with
    {
        Groups = book.Groups.Select(g => g with
        {
            Connectors = g.Connectors.Select(c => c with { Examples = Fill(c.Examples, generator) }).ToList(),
        }).ToList(),
    };

    private static List<Example> Fill(IEnumerable<Example> examples, IFuriganaGenerator generator) =>
        examples.Select(e => e with { Ruby = generator.ToRuby(e.Japanese) }).ToList();
}
```

- [ ] **Step 5: Viết `ConnectorPipeline`**

`src/Japan.Importer/ConnectorPipeline.cs`:

```csharp
using Japan.Core;
using Japan.Importer.Assembly;
using Japan.Importer.Furigana;

namespace Japan.Importer;

public static class ConnectorPipeline
{
    public static int Run(string contentDirectory, string outputPath, TextWriter output, IFuriganaGenerator generator, TimeProvider? clock = null)
    {
        var loaded = ConnectorLoader.Load(contentDirectory);
        if (loaded.Errors.Count > 0)
        {
            output.WriteLine($"LỖI ({loaded.Errors.Count}) — không ghi {outputPath}:");
            foreach (var error in loaded.Errors) output.WriteLine($"  - {error}");
            return 1;
        }

        var groups = loaded.Groups
            .OrderBy(g => g.Order)
            .Select(g => g with { Connectors = g.Connectors.OrderBy(c => c.Level).ToList() })
            .ToList();
        var book = FuriganaApplier.Apply(new ConnectorBook(groups, (clock ?? TimeProvider.System).GetUtcNow()), generator);
        OutputFile.Write(outputPath, ConnectorJson.Serialize(book));

        var connectors = book.Groups.SelectMany(g => g.Connectors).ToList();
        var byLevel = string.Join(", ", JlptLevels.All.Select(l => $"{l}: {connectors.Count(c => c.Level == l)}"));
        output.WriteLine($"Đã ghi {outputPath}: {book.Groups.Count} nhóm, {connectors.Count} từ nối ({byLevel}).");
        return 0;
    }
}
```

(`OrderBy` của LINQ là sắp xếp ổn định, nên các từ cùng cấp giữ thứ tự trong file.)

- [ ] **Step 6: Nối lệnh vào `Program.cs`**

Thay toàn bộ `src/Japan.Importer/Program.cs`:

```csharp
using System.Text;
using System.Text.Json;
using Japan.Core;
using Japan.Importer;
using Japan.Importer.Furigana;

Console.OutputEncoding = Encoding.UTF8;

try
{
    var repoRoot = RepoRoot.Find(Directory.GetCurrentDirectory());
    var contentDirectory = Path.Combine(repoRoot, "content");
    var overrides = FuriganaOverrides.Load(Path.Combine(contentDirectory, "furigana-overrides.json"));
    using var generator = new NMeCabFuriganaGenerator(overrides);

    if (args is ["connectors", ..])
    {
        return ConnectorPipeline.Run(
            Path.Combine(contentDirectory, "connectors"),
            Path.Combine(repoRoot, "data", "connectors.json"),
            Console.Out,
            generator);
    }

    var config = JsonSerializer.Deserialize<ImporterConfig>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")), GrammarJson.Options)
        ?? throw new InvalidDataException("appsettings.json rỗng.");
    var settings = new ImportSettings(
        SourceDirectory: args.Length > 0 ? args[0] : config.SourceDirectory,
        ContentDirectory: contentDirectory,
        OutputPath: Path.Combine(repoRoot, "data", "grammar.json"));

    return ImportPipeline.Run(settings, Console.Out, generator);
}
catch (Exception ex) when (ex is InvalidDataException or DirectoryNotFoundException)
{
    Console.Error.WriteLine($"LỖI: {ex.Message}");
    return 1;
}

internal sealed record ImporterConfig(string SourceDirectory);
```

- [ ] **Step 7: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS — gồm `ConnectorPipelineTests`, `ImportPipelineTests` và `FuriganaTests` cũ.

- [ ] **Step 8: Thử lệnh thật khi chưa có nội dung**

Run: `dotnet run --project src/Japan.Importer -- connectors`
Expected: in `LỖI (1) — không ghi …connectors.json:` và `Không tìm thấy thư mục …content\connectors`, mã thoát 1.

- [ ] **Step 9: Commit**

```bash
git add src/Japan.Importer tests/Japan.Tests/Importer/ConnectorPipelineTests.cs
git commit -m "feat: add the connectors importer command"
```

---

### Task 5: Tách cơ chế tự nạp lại file JSON

**Files:**
- Create: `src/Japan.Web/Services/ReloadingJsonFile.cs`
- Modify: `src/Japan.Web/Services/GrammarRepository.cs` (viết lại, giữ nguyên public API)
- Test: `tests/Japan.Tests/Web/ReloadingJsonFileTests.cs`

**Interfaces:**
- Produces: `sealed class ReloadingJsonFile<T> : IDisposable where T : class` với ctor `(string path, Func<string, T> parse, Func<T, string> describe, ILogger logger)`, thuộc tính `string Path`, `string? LoadError`, `T? Value`, `bool IsLoaded` (thử nạp lại nếu chưa có dữ liệu), method `bool Reload()`.
- `GrammarRepository` giữ nguyên: `DataPath`, `LoadError`, `IsLoaded`, `Lessons`, `FindLesson`, `Find`, `Locate`, `Search`, `Reload`, `Dispose`.

- [ ] **Step 1: Viết test thất bại**

`tests/Japan.Tests/Web/ReloadingJsonFileTests.cs`:

```csharp
using Japan.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Japan.Tests.Web;

public class ReloadingJsonFileTests
{
    private static string TempFile() => Path.Combine(Directory.CreateTempSubdirectory("reload-").FullName, "x.json");

    private static string Parse(string text) =>
        text.Contains("bad") ? throw new InvalidDataException("bad") : text.Trim();

    [Fact]
    public void MissingFile_IsNotLoaded_UntilFileAppears()
    {
        var path = TempFile();
        using var file = new ReloadingJsonFile<string>(path, Parse, v => v, NullLogger.Instance);

        Assert.False(file.IsLoaded);
        Assert.Contains(path, file.LoadError);

        File.WriteAllText(path, " ok ");

        Assert.True(file.Reload());
        Assert.Equal("ok", file.Value);
        Assert.Null(file.LoadError);
    }

    [Fact]
    public void ParseFailure_KeepsPreviousValue()
    {
        var path = TempFile();
        File.WriteAllText(path, "v1");
        using var file = new ReloadingJsonFile<string>(path, Parse, v => v, NullLogger.Instance);

        File.WriteAllText(path, "bad");

        Assert.False(file.Reload());
        Assert.Equal("v1", file.Value);
        Assert.Contains(path, file.LoadError);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ReloadingJsonFileTests"`
Expected: build FAIL — `ReloadingJsonFile<>` not found.

- [ ] **Step 3: Viết `ReloadingJsonFile<T>`**

`src/Japan.Web/Services/ReloadingJsonFile.cs`:

```csharp
namespace Japan.Web.Services;

public sealed class ReloadingJsonFile<T> : IDisposable where T : class
{
    private const int DebounceMs = 300;
    private const int RetryMs = 1000;
    private const int MaxRetries = 3;

    private readonly Func<string, T> _parse;
    private readonly Func<T, string> _describe;
    private readonly ILogger _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly Timer _debounce;
    private readonly Lock _reloadLock = new();
    private volatile T? _value;
    private int _retries;

    public ReloadingJsonFile(string path, Func<string, T> parse, Func<T, string> describe, ILogger logger)
    {
        Path = System.IO.Path.GetFullPath(path);
        _parse = parse;
        _describe = describe;
        _logger = logger;
        _debounce = new Timer(_ => OnDebounce(), null, Timeout.Infinite, Timeout.Infinite);
        Reload();

        var directory = System.IO.Path.GetDirectoryName(Path)!;
        if (!Directory.Exists(directory)) return;

        _watcher = new FileSystemWatcher(directory, System.IO.Path.GetFileName(Path))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += (_, _) => ScheduleReload();
        _watcher.Created += (_, _) => ScheduleReload();
        _watcher.Renamed += (_, _) => ScheduleReload();
        _watcher.EnableRaisingEvents = true;
    }

    public string Path { get; }

    public string? LoadError { get; private set; }

    public T? Value => _value;

    public bool IsLoaded
    {
        get
        {
            if (_value is null) Reload();
            return _value is not null;
        }
    }

    public bool Reload()
    {
        lock (_reloadLock)
        {
            try
            {
                if (!File.Exists(Path))
                {
                    LoadError = $"Không tìm thấy {Path}";
                    _logger.LogWarning("Không tìm thấy {Path}", Path);
                    return false;
                }

                var value = _parse(File.ReadAllText(Path));
                _value = value;
                LoadError = null;
                _logger.LogInformation("Đã nạp {Content} từ {Path}", _describe(value), Path);
                return true;
            }
            catch (Exception ex)
            {
                LoadError = $"Không đọc được {Path}: {ex.Message}";
                _logger.LogError(ex, "Không đọc được {Path}, giữ dữ liệu cũ", Path);
                return false;
            }
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce.Dispose();
    }

    private void ScheduleReload()
    {
        Interlocked.Exchange(ref _retries, 0);
        _debounce.Change(DebounceMs, Timeout.Infinite);
    }

    private void OnDebounce()
    {
        if (Reload()) return;
        if (Interlocked.Increment(ref _retries) <= MaxRetries)
            _debounce.Change(RetryMs, Timeout.Infinite);
    }
}
```

- [ ] **Step 4: Viết lại `GrammarRepository` trên nền `ReloadingJsonFile`**

Thay toàn bộ `src/Japan.Web/Services/GrammarRepository.cs`:

```csharp
using Japan.Core;
using Microsoft.Extensions.Options;

namespace Japan.Web.Services;

public sealed class GrammarRepository : IDisposable
{
    private sealed record Snapshot(
        GrammarBook Book,
        IReadOnlyList<GrammarPoint> Ordered,
        IReadOnlyDictionary<string, int> IndexById,
        GrammarSearch Search);

    private readonly ReloadingJsonFile<Snapshot> _file;

    public GrammarRepository(IOptions<GrammarDataOptions> options, IWebHostEnvironment env, ILogger<GrammarRepository> logger) =>
        _file = new ReloadingJsonFile<Snapshot>(
            Path.Combine(env.ContentRootPath, options.Value.Path), Parse, s => $"{s.Ordered.Count} điểm ngữ pháp", logger);

    public string DataPath => _file.Path;

    public string? LoadError => _file.LoadError;

    public bool IsLoaded => _file.IsLoaded;

    public IReadOnlyList<Lesson> Lessons => _file.Value?.Book.Lessons ?? [];

    public Lesson? FindLesson(int number) => Lessons.FirstOrDefault(l => l.Number == number);

    public GrammarPoint? Find(string id) => Locate(id).Point;

    public (GrammarPoint? Point, GrammarPoint? Previous, GrammarPoint? Next) Locate(string id)
    {
        if (_file.Value is not { } s || !s.IndexById.TryGetValue(id, out var i)) return (null, null, null);
        return (s.Ordered[i], i > 0 ? s.Ordered[i - 1] : null, i < s.Ordered.Count - 1 ? s.Ordered[i + 1] : null);
    }

    public IReadOnlyList<SearchHit> Search(string? query) => _file.Value?.Search.Search(query) ?? [];

    public bool Reload() => _file.Reload();

    public void Dispose() => _file.Dispose();

    private static Snapshot Parse(string json)
    {
        var book = GrammarJson.Deserialize(json);
        var ordered = book.Lessons.SelectMany(l => l.Points).ToList();
        var index = ordered.Select((p, i) => (p.Id, i)).ToDictionary(x => x.Id, x => x.i, StringComparer.Ordinal);
        return new Snapshot(book, ordered, index, new GrammarSearch(book));
    }
}
```

- [ ] **Step 5: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS — `ReloadingJsonFileTests` cùng mọi `GrammarRepositoryTests`, `PagesTests`, `SearchPageTests` cũ (xác nhận refactor không đổi hành vi).

- [ ] **Step 6: Commit**

```bash
git add src/Japan.Web/Services/ReloadingJsonFile.cs src/Japan.Web/Services/GrammarRepository.cs tests/Japan.Tests/Web/ReloadingJsonFileTests.cs
git commit -m "refactor: share the reloading JSON file logic"
```

---

### Task 6: `ConnectorRepository` và cấu hình web

**Files:**
- Create: `src/Japan.Web/Services/ConnectorDataOptions.cs`
- Create: `src/Japan.Web/Services/ConnectorRepository.cs`
- Modify: `src/Japan.Web/Program.cs` (sau dòng `builder.Services.AddSingleton<GrammarRepository>();`)
- Modify: `src/Japan.Web/appsettings.json`
- Modify: `tests/Japan.Tests/Web/GrammarWebFactory.cs`
- Test: `tests/Japan.Tests/Web/ConnectorRepositoryTests.cs`

**Interfaces:**
- Consumes: `ReloadingJsonFile<T>` (Task 5), `ConnectorJson`, `ConnectorValidator`, `ConnectorSearch`, `JlptLevel` (Task 1–3).
- Produces:
  - `record ConnectorLocation(Connector Connector, ConnectorGroup Group, Connector? Previous, Connector? Next)`
  - `ConnectorRepository`: `string DataPath`, `string? LoadError`, `bool IsLoaded`, `IReadOnlyList<ConnectorGroup> Groups`, `int Count`, `IReadOnlyList<ConnectorGroup> Filter(JlptLevel? level)`, `ConnectorLocation? Locate(string id)`, `IReadOnlyList<ConnectorHit> Search(string? query)`, `bool Reload()`, `Dispose()`.
  - `GrammarWebFactory`: `WithBook(GrammarBook book, ConnectorBook? connectors = null)`, `WithConnectors(ConnectorBook)`, `WithoutConnectors()`, thuộc tính `ConnectorDataPath`. Mọi factory mặc định ghi `TestConnectors.Sample()`.

- [ ] **Step 1: Mở rộng `GrammarWebFactory`**

Thay toàn bộ `tests/Japan.Tests/Web/GrammarWebFactory.cs`:

```csharp
using Japan.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Japan.Tests.Web;

public sealed class GrammarWebFactory(string dataPath, string connectorDataPath) : WebApplicationFactory<Program>
{
    public static GrammarWebFactory WithSampleData() => WithBook(TestBooks.Sample());

    public static GrammarWebFactory WithConnectors(ConnectorBook connectors) => WithBook(TestBooks.Sample(), connectors);

    public static GrammarWebFactory WithoutConnectors()
    {
        var factory = WithSampleData();
        File.Delete(factory.ConnectorDataPath);
        return factory;
    }

    public static GrammarWebFactory WithBook(GrammarBook book, ConnectorBook? connectors = null)
    {
        var directory = Directory.CreateTempSubdirectory("web-").FullName;
        var path = Path.Combine(directory, "grammar.json");
        File.WriteAllText(path, GrammarJson.Serialize(book));
        var connectorPath = Path.Combine(directory, "connectors.json");
        File.WriteAllText(connectorPath, ConnectorJson.Serialize(connectors ?? TestConnectors.Sample()));
        return new GrammarWebFactory(path, connectorPath);
    }

    public string DataPath => dataPath;

    public string ConnectorDataPath => connectorDataPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("GrammarData:Path", dataPath);
        builder.UseSetting("ConnectorData:Path", connectorDataPath);
    }
}
```

- [ ] **Step 2: Viết test thất bại**

`tests/Japan.Tests/Web/ConnectorRepositoryTests.cs`:

```csharp
using Japan.Core;
using Japan.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Japan.Tests.Web;

public class ConnectorRepositoryTests
{
    [Fact]
    public void Locate_ReturnsConnectorGroupAndNeighbors()
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();

        var middle = repo.Locate("shitagatte");
        var first = repo.Locate("dakara");

        Assert.Equal("thuan-tiep", middle?.Group.Id);
        Assert.Equal("dakara", middle?.Previous?.Id);
        Assert.Equal("shikashi", middle?.Next?.Id);
        Assert.Null(first?.Previous);
        Assert.Null(repo.Locate("khong-co"));
    }

    [Fact]
    public void Filter_KeepsOnlyLevelAndDropsEmptyGroups()
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();

        var n3 = repo.Filter(JlptLevel.N3);

        var group = Assert.Single(n3);
        Assert.Equal("thuan-tiep", group.Id);
        Assert.Equal(["shitagatte"], group.Connectors.Select(c => c.Id));
        Assert.Equal(2, repo.Filter(null).Count);
        Assert.Equal(3, repo.Count);
    }

    [Fact]
    public void MissingFile_IsNotLoaded_AndGrammarStillLoads()
    {
        using var factory = GrammarWebFactory.WithoutConnectors();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();

        Assert.False(repo.IsLoaded);
        Assert.Contains(factory.ConnectorDataPath, repo.LoadError);
        Assert.Empty(repo.Groups);
        Assert.True(factory.Services.GetRequiredService<GrammarRepository>().IsLoaded);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{"groups":[],"generatedAt":"2026-10-14T00:00:00+00:00"}""")]
    public void Reload_BadFile_KeepsPreviousData(string content)
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();
        Assert.True(repo.IsLoaded);

        File.WriteAllText(factory.ConnectorDataPath, content);

        Assert.False(repo.Reload());
        Assert.NotNull(repo.Locate("dakara"));
        Assert.Contains(factory.ConnectorDataPath, repo.LoadError);
    }

    [Fact]
    public void Reload_DuplicateIds_KeepsPreviousData()
    {
        using var factory = GrammarWebFactory.WithSampleData();
        var repo = factory.Services.GetRequiredService<ConnectorRepository>();
        var sample = TestConnectors.Sample();
        var duplicated = sample with
        {
            Groups = [sample.Groups[0], sample.Groups[1] with { Connectors = [.. sample.Groups[1].Connectors, sample.Groups[0].Connectors[0]] }],
        };

        File.WriteAllText(factory.ConnectorDataPath, ConnectorJson.Serialize(duplicated));

        Assert.False(repo.Reload());
        Assert.Equal(3, repo.Count);
    }
}
```

- [ ] **Step 3: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorRepositoryTests"`
Expected: build FAIL — `ConnectorRepository` not found.

- [ ] **Step 4: Viết options, repository và đăng ký DI**

`src/Japan.Web/Services/ConnectorDataOptions.cs`:

```csharp
namespace Japan.Web.Services;

public sealed class ConnectorDataOptions
{
    public const string Section = "ConnectorData";

    public string Path { get; set; } = "../../data/connectors.json";
}
```

`src/Japan.Web/Services/ConnectorRepository.cs`:

```csharp
using Japan.Core;
using Microsoft.Extensions.Options;

namespace Japan.Web.Services;

public sealed record ConnectorLocation(Connector Connector, ConnectorGroup Group, Connector? Previous, Connector? Next);

public sealed class ConnectorRepository : IDisposable
{
    private sealed record Snapshot(
        ConnectorBook Book,
        IReadOnlyList<(Connector Connector, ConnectorGroup Group)> Ordered,
        IReadOnlyDictionary<string, int> IndexById,
        ConnectorSearch Search);

    private readonly ReloadingJsonFile<Snapshot> _file;

    public ConnectorRepository(IOptions<ConnectorDataOptions> options, IWebHostEnvironment env, ILogger<ConnectorRepository> logger) =>
        _file = new ReloadingJsonFile<Snapshot>(
            Path.Combine(env.ContentRootPath, options.Value.Path), Parse, s => $"{s.Ordered.Count} từ nối", logger);

    public string DataPath => _file.Path;

    public string? LoadError => _file.LoadError;

    public bool IsLoaded => _file.IsLoaded;

    public IReadOnlyList<ConnectorGroup> Groups => _file.Value?.Book.Groups ?? [];

    public int Count => _file.Value?.Ordered.Count ?? 0;

    public IReadOnlyList<ConnectorGroup> Filter(JlptLevel? level) => level is not { } l
        ? Groups
        : Groups.Select(g => g with { Connectors = g.Connectors.Where(c => c.Level == l).ToList() })
                .Where(g => g.Connectors.Count > 0)
                .ToList();

    public ConnectorLocation? Locate(string id)
    {
        if (_file.Value is not { } s || !s.IndexById.TryGetValue(id, out var i)) return null;
        var (connector, group) = s.Ordered[i];
        return new ConnectorLocation(connector, group,
            i > 0 ? s.Ordered[i - 1].Connector : null,
            i < s.Ordered.Count - 1 ? s.Ordered[i + 1].Connector : null);
    }

    public IReadOnlyList<ConnectorHit> Search(string? query) => _file.Value?.Search.Search(query) ?? [];

    public bool Reload() => _file.Reload();

    public void Dispose() => _file.Dispose();

    private static Snapshot Parse(string json)
    {
        var book = ConnectorJson.Deserialize(json);
        var errors = ConnectorValidator.Validate(book.Groups);
        if (errors.Count > 0) throw new InvalidDataException(string.Join("; ", errors));

        var ordered = book.Groups.SelectMany(g => g.Connectors.Select(c => (c, g))).ToList();
        var index = ordered.Select((x, i) => (x.c.Id, i)).ToDictionary(x => x.Id, x => x.i, StringComparer.Ordinal);
        return new Snapshot(book, ordered, index, new ConnectorSearch(book));
    }
}
```

Trong `src/Japan.Web/Program.cs`, ngay sau `builder.Services.AddSingleton<GrammarRepository>();` thêm:

```csharp
builder.Services.Configure<ConnectorDataOptions>(builder.Configuration.GetSection(ConnectorDataOptions.Section));
builder.Services.AddSingleton<ConnectorRepository>();
```

Trong `src/Japan.Web/appsettings.json`, sau khối `"GrammarData"` thêm (nhớ dấu phẩy):

```json
  "ConnectorData": {
    "Path": "../../data/connectors.json"
  }
```

- [ ] **Step 5: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Japan.Web/Services src/Japan.Web/Program.cs src/Japan.Web/appsettings.json tests/Japan.Tests/Web/GrammarWebFactory.cs tests/Japan.Tests/Web/ConnectorRepositoryTests.cs
git commit -m "feat: load connectors.json in the web app"
```

---

### Task 7: Trang chi tiết `/tu-noi/{id}`

**Files:**
- Create: `src/Japan.Web/Rendering/ConnectorLabels.cs`
- Create: `src/Japan.Web/Pages/Connector.cshtml`
- Create: `src/Japan.Web/Pages/Connector.cshtml.cs`
- Test: `tests/Japan.Tests/Web/ConnectorPagesTests.cs` (tạo mới)

**Interfaces:**
- Consumes: `ConnectorRepository.Locate(string) : ConnectorLocation?` (Task 6), partial `_Examples` (model `IReadOnlyList<Example>`), CSS của Grammar page (`point-layout`, `toc`, `block`, `compare-grid`, `pager`…), class `level-badge` (style thêm ở Task 8; chưa có style thì không ảnh hưởng test).
- Produces: `ConnectorLabels.Label(this ConnectorRegister) : string` — `"Văn nói"`, `"Văn viết"`, `"Văn nói & văn viết"`; lớp test `ConnectorPagesTests` (field `_factory`, helper `Get(string url)`) để Task 8 thêm test.

- [ ] **Step 1: Tạo `ConnectorPagesTests` với test thất bại**

`tests/Japan.Tests/Web/ConnectorPagesTests.cs`:

```csharp
using System.Net;

namespace Japan.Tests.Web;

public sealed class ConnectorPagesTests : IDisposable
{
    private readonly GrammarWebFactory _factory = GrammarWebFactory.WithSampleData();

    public void Dispose() => _factory.Dispose();

    private Task<string> Get(string url) => _factory.CreateClient().GetStringAsync(url);

    [Fact]
    public async Task Detail_ShowsContentRubyAndLinks()
    {
        var html = await Get("/tu-noi/dakara");

        Assert.Contains("vì vậy, cho nên", html);
        Assert.Contains("Văn nói", html);
        Assert.Contains("Nối nguyên nhân ở câu trước", html);
        Assert.Contains("Không dùng trong văn bản trang trọng.", html);
        Assert.Contains("<ruby>雨<rt>あめ</rt></ruby>", html);
        Assert.Contains("href=\"/tu-noi/shitagatte\"", html);
        Assert.Contains("rel=\"next\"", html);
        Assert.DoesNotContain("rel=\"prev\"", html);
    }

    [Fact]
    public async Task Detail_MiddleItem_HasBothNeighbors_AndPlainComparison()
    {
        var middle = await Get("/tu-noi/shitagatte");
        var last = await Get("/tu-noi/shikashi");

        Assert.Contains("href=\"/tu-noi/dakara\"", middle);
        Assert.Contains("href=\"/tu-noi/shikashi\"", middle);
        Assert.Contains("でも thân mật hơn", last);
        Assert.DoesNotContain("rel=\"next\"", last);
    }

    [Fact]
    public async Task Detail_KanjiWord_ShowsReading()
    {
        var sample = TestConnectors.Sample();
        var ippou = sample.Groups[1].Connectors[0] with { Id = "ippou", Word = "一方", Reading = "いっぽう", Comparisons = [] };
        using var factory = GrammarWebFactory.WithConnectors(sample with
        {
            Groups = [sample.Groups[0], sample.Groups[1] with { Connectors = [ippou] }],
        });

        var page = await factory.CreateClient().GetStringAsync("/tu-noi/ippou");

        Assert.Contains("一方", page);
        Assert.Contains("いっぽう", page);
    }

    [Fact]
    public async Task Detail_UnknownId_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/tu-noi/khong-co");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorPagesTests.Detail"`
Expected: FAIL — 404 cho `/tu-noi/dakara`.

- [ ] **Step 3: Nhãn sắc thái và page model**

`src/Japan.Web/Rendering/ConnectorLabels.cs`:

```csharp
using Japan.Core;

namespace Japan.Web.Rendering;

public static class ConnectorLabels
{
    public static string Label(this ConnectorRegister register) => register switch
    {
        ConnectorRegister.Spoken => "Văn nói",
        ConnectorRegister.Written => "Văn viết",
        _ => "Văn nói & văn viết",
    };
}
```

`src/Japan.Web/Pages/Connector.cshtml.cs`:

```csharp
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class ConnectorModel(ConnectorRepository repo) : PageModel
{
    public ConnectorLocation Location { get; private set; } = null!;

    public IActionResult OnGet(string id)
    {
        if (repo.Locate(id) is not { } location) return NotFound();
        Location = location;
        return Page();
    }
}
```

- [ ] **Step 4: View chi tiết**

`src/Japan.Web/Pages/Connector.cshtml`:

```cshtml
@page "/tu-noi/{id}"
@model ConnectorModel
@{
    var c = Model.Location.Connector;
    var g = Model.Location.Group;
    ViewData["Title"] = c.Word;

    var toc = new List<(string Id, string Title)> { ("y-nghia", "Ý nghĩa"), ("giai-thich", "Giải thích") };
    if (c.Usage.Count > 0) toc.Add(("cach-dung", "Cách dùng"));
    if (c.Pitfalls.Count > 0) toc.Add(("loi-hay-gap", "Lỗi hay gặp"));
    if (c.Comparisons.Count > 0) toc.Add(("so-sanh", "So sánh"));
    toc.Add(("vi-du", "Ví dụ"));
}
<div class="point-layout">
    <aside class="toc" aria-label="Mục lục trang">
        <p class="toc-label">Trong trang này</p>
        <ul>
            @foreach (var item in toc)
            {
                <li><a href="#@item.Id">@item.Title</a></li>
            }
        </ul>
    </aside>

    <article class="point-main">
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb">
                <li class="breadcrumb-item"><a asp-page="/Index">Mục lục</a></li>
                <li class="breadcrumb-item"><a asp-page="/Connectors">Từ nối</a></li>
                <li class="breadcrumb-item active ja" aria-current="page">@c.Word</li>
            </ol>
        </nav>
        <header class="point-head">
            <p class="eyebrow">@g.Title · <span class="level-badge">@c.Level</span> · @c.Register.Label()</p>
            <h1 class="point-title ja">@c.Word</h1>
            @if (c.Reading != c.Word)
            {
                <p class="connector-reading ja">@c.Reading</p>
            }
        </header>

        <section id="y-nghia" class="block">
            <h2 class="block-title">Ý nghĩa</h2>
            <p class="lead-text">@c.Meaning</p>
        </section>

        <section id="giai-thich" class="block">
            <h2 class="block-title">Giải thích</h2>
            @foreach (var para in c.Explanation.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                <p class="jp">@para</p>
            }
        </section>

        @if (c.Usage.Count > 0)
        {
            <section id="cach-dung" class="block">
                <h2 class="block-title">Cách dùng</h2>
                <ul class="bullets">@foreach (var u in c.Usage) { <li class="jp">@u</li> }</ul>
            </section>
        }

        @if (c.Pitfalls.Count > 0)
        {
            <section id="loi-hay-gap" class="block block-warn">
                <h2 class="block-title">Lưu ý &amp; lỗi hay gặp</h2>
                <ul class="bullets">@foreach (var u in c.Pitfalls) { <li class="jp">@u</li> }</ul>
            </section>
        }

        @if (c.Comparisons.Count > 0)
        {
            <section id="so-sanh" class="block">
                <h2 class="block-title">So sánh</h2>
                <div class="compare-grid">
                    @foreach (var cmp in c.Comparisons)
                    {
                        <div class="compare-card">
                            @if (cmp.TargetId is not null)
                            {
                                <a class="compare-pattern ja" asp-page="/Connector" asp-route-id="@cmp.TargetId">@cmp.Pattern <span aria-hidden="true">→</span></a>
                            }
                            else
                            {
                                <span class="compare-pattern ja">@cmp.Pattern</span>
                            }
                            <p class="jp mb-0">@cmp.Difference</p>
                        </div>
                    }
                </div>
            </section>
        }

        <section id="vi-du" class="block">
            <h2 class="block-title">Ví dụ</h2>
            <partial name="_Examples" model="c.Examples" />
        </section>

        <nav class="pager" aria-label="Từ nối trước và sau">
            @if (Model.Location.Previous is { } prev)
            {
                <a class="pager-link" rel="prev" asp-page="/Connector" asp-route-id="@prev.Id">
                    <span class="pager-dir">← Trước</span>
                    <span class="pager-pattern ja">@prev.Word</span>
                </a>
            }
            else
            {
                <span></span>
            }
            @if (Model.Location.Next is { } next)
            {
                <a class="pager-link pager-next" rel="next" asp-page="/Connector" asp-route-id="@next.Id">
                    <span class="pager-dir">Sau →</span>
                    <span class="pager-pattern ja">@next.Word</span>
                </a>
            }
        </nav>
    </article>
</div>
```

- [ ] **Step 5: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Japan.Web/Rendering/ConnectorLabels.cs src/Japan.Web/Pages/Connector.cshtml src/Japan.Web/Pages/Connector.cshtml.cs tests/Japan.Tests/Web/ConnectorPagesTests.cs
git commit -m "feat: add the connector detail page"
```

---

### Task 8: Trang danh sách `/tu-noi`, link trên header và mục lục

**Files:**
- Create: `src/Japan.Web/Pages/Connectors.cshtml`
- Create: `src/Japan.Web/Pages/Connectors.cshtml.cs`
- Modify: `src/Japan.Web/Pages/Shared/_Layout.cshtml` (thẻ `<nav>` trong header)
- Modify: `src/Japan.Web/Pages/Index.cshtml`, `src/Japan.Web/Pages/Index.cshtml.cs`
- Modify: `src/Japan.Web/wwwroot/css/site.css` (thêm cuối file)
- Test: `tests/Japan.Tests/Web/ConnectorPagesTests.cs` (thêm test)

**Interfaces:**
- Consumes: `ConnectorRepository.IsLoaded/LoadError/Count/Filter` (Task 6), `JlptLevels.TryParse/All` (Task 1).
- Produces: page `/Connectors` (route `/tu-noi`, query `cap`); CSS class `level-filter`, `level-chip`, `connector-grid`, `connector-card`, `connector-word`, `connector-reading`, `connector-meaning`, `level-badge`, `group-ja`, `group-desc`, `nav-main`, `feature-link` (trang chi tiết ở Task 7 đã dùng sẵn `level-badge`).

- [ ] **Step 1: Thêm test thất bại vào `ConnectorPagesTests`** (file tạo ở Task 7)

```csharp
    [Fact]
    public async Task List_ShowsGroupsAndConnectors()
    {
        var html = await Get("/tu-noi");

        Assert.Contains("Thuận tiếp — kết quả, hệ quả", html);
        Assert.Contains("順接", html);
        Assert.Contains("Nghịch tiếp — trái ngược", html);
        Assert.Contains("href=\"/tu-noi/dakara\"", html);
        Assert.Contains("vì vậy, cho nên", html);
        Assert.Contains("href=\"/tu-noi?cap=N3\"", html);
    }

    [Theory]
    [InlineData("/tu-noi?cap=N3")]
    [InlineData("/tu-noi?cap=n3")]
    [InlineData("/tu-noi?cap=%20N3%20")]
    public async Task List_FilterByLevel_HidesOtherLevelsAndEmptyGroups(string url)
    {
        var html = await Get(url);

        Assert.Contains("href=\"/tu-noi/shitagatte\"", html);
        Assert.DoesNotContain("href=\"/tu-noi/dakara\"", html);
        Assert.DoesNotContain("Nghịch tiếp — trái ngược", html);
    }

    [Theory]
    [InlineData("/tu-noi?cap=abc")]
    [InlineData("/tu-noi?cap=3")]
    [InlineData("/tu-noi?cap=N1")]
    public async Task List_UnknownLevel_ShowsAll(string url)
    {
        var html = await Get(url);

        Assert.Contains("href=\"/tu-noi/dakara\"", html);
        Assert.Contains("href=\"/tu-noi/shikashi\"", html);
    }

    [Fact]
    public async Task HeaderAndIndex_LinkToConnectors()
    {
        var index = await Get("/");
        var lesson = await Get("/bai/7");

        Assert.Contains("3 từ nối theo chức năng", index);
        Assert.Contains("href=\"/tu-noi\"", lesson);
    }

    [Fact]
    public async Task MissingConnectors_ListShowsHint_IndexStillWorks()
    {
        using var factory = GrammarWebFactory.WithoutConnectors();
        var client = factory.CreateClient();

        var list = await client.GetStringAsync("/tu-noi");
        var index = await client.GetStringAsync("/");

        Assert.Contains("Chưa có dữ liệu từ nối", list);
        Assert.Contains("-- connectors", list);
        Assert.Contains("Thời điểm của sự việc", index);
        Assert.DoesNotContain("từ nối theo chức năng", index);
    }

    [Fact]
    public async Task List_KanjiWord_ShowsReadingOnCard()
    {
        var sample = TestConnectors.Sample();
        var ippou = sample.Groups[1].Connectors[0] with { Id = "ippou", Word = "一方", Reading = "いっぽう", Comparisons = [] };
        using var factory = GrammarWebFactory.WithConnectors(sample with
        {
            Groups = [sample.Groups[0], sample.Groups[1] with { Connectors = [ippou] }],
        });

        var html = await factory.CreateClient().GetStringAsync("/tu-noi");

        Assert.Contains("connector-reading ja\">いっぽう<", html);
    }
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorPagesTests"`
Expected: FAIL — `/tu-noi` trả 404 (`HttpRequestException`).

- [ ] **Step 3: Viết page model**

`src/Japan.Web/Pages/Connectors.cshtml.cs`:

```csharp
using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class ConnectorsModel(ConnectorRepository repo) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "cap")]
    public string? Cap { get; set; }

    public JlptLevel? Level { get; private set; }

    public IReadOnlyList<ConnectorGroup> Groups { get; private set; } = [];

    public bool IsLoaded { get; private set; }

    public string? LoadError => repo.LoadError;

    public void OnGet()
    {
        IsLoaded = repo.IsLoaded;
        if (!IsLoaded) return;
        Level = JlptLevels.TryParse(Cap, out var level) ? level : null;
        Groups = repo.Filter(Level);
    }
}
```

- [ ] **Step 4: Viết view**

`src/Japan.Web/Pages/Connectors.cshtml`:

```cshtml
@page "/tu-noi"
@model ConnectorsModel
@{
    ViewData["Title"] = "Từ nối";
    var shown = Model.Groups.Sum(g => g.Connectors.Count);
}
<nav aria-label="breadcrumb">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a asp-page="/Index">Mục lục</a></li>
        <li class="breadcrumb-item active" aria-current="page">Từ nối</li>
    </ol>
</nav>
<header class="page-head">
    <p class="eyebrow"><span class="ja">接続詞</span> · N5–N2</p>
    <h1 class="page-title">Từ nối</h1>
    <p class="group-desc">Từ đứng đầu câu để nối hai câu hoặc hai đoạn, chia theo chức năng. Khi làm bài, hãy xác định quan hệ giữa hai câu trước rồi mới chọn từ.</p>
</header>
@if (!Model.IsLoaded)
{
    <div class="alert alert-warning">
        <strong>Chưa có dữ liệu từ nối.</strong> <code>@Model.LoadError</code>.
        Hãy chạy <code>dotnet run --project src/Japan.Importer -- connectors</code> trong thư mục repo rồi tải lại trang.
    </div>
}
else
{
    <nav class="level-filter" aria-label="Lọc theo cấp">
        <a class="level-chip @(Model.Level is null ? "active" : null)" asp-page="/Connectors"
           aria-current="@(Model.Level is null ? "page" : null)">Tất cả</a>
        @foreach (var level in JlptLevels.All)
        {
            <a class="level-chip @(Model.Level == level ? "active" : null)" asp-page="/Connectors" asp-route-cap="@level"
               aria-current="@(Model.Level == level ? "page" : null)">@level</a>
        }
        <span class="level-count">@shown từ</span>
    </nav>

    @foreach (var group in Model.Groups)
    {
        <section class="block" id="@group.Id">
            <h2 class="block-title">@group.Title <span class="group-ja ja">@group.JaTitle</span></h2>
            <p class="group-desc">@group.Description</p>
            <div class="connector-grid">
                @foreach (var c in group.Connectors)
                {
                    <a class="connector-card" asp-page="/Connector" asp-route-id="@c.Id">
                        <span class="connector-head">
                            <span class="connector-word ja">@c.Word</span>
                            <span class="level-badge">@c.Level</span>
                        </span>
                        @if (c.Reading != c.Word)
                        {
                            <span class="connector-reading ja">@c.Reading</span>
                        }
                        <span class="connector-meaning">@c.Meaning</span>
                    </a>
                }
            </div>
        </section>
    }
}
```

- [ ] **Step 5: Link trên header, ô trên mục lục, placeholder tìm kiếm**

Trong `src/Japan.Web/Pages/Shared/_Layout.cshtml`, ngay sau thẻ `</a>` đóng của `<a class="brand" …>` thêm:

```cshtml
            <a class="nav-main" asp-page="/Connectors">Từ nối</a>
```

và trong cùng file đổi input của header search thành:

```cshtml
                    <input name="q" type="search" class="form-control" placeholder="Tìm ngữ pháp, từ nối…" aria-label="Tìm kiếm" autocomplete="off" />
```

Thay toàn bộ `src/Japan.Web/Pages/Index.cshtml.cs`:

```csharp
using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class IndexModel(GrammarRepository repo, ConnectorRepository connectors) : PageModel
{
    public IReadOnlyList<Lesson> Lessons => repo.Lessons;

    public int ConnectorCount => connectors.IsLoaded ? connectors.Count : 0;
}
```

Trong `src/Japan.Web/Pages/Index.cshtml`, đổi `placeholder` của `#search-box` thành `"Tìm mẫu ngữ pháp, từ nối, nghĩa tiếng Việt hoặc câu ví dụ…"` và chèn ngay trước `<div id="search-results"></div>`:

```cshtml
@if (Model.ConnectorCount > 0)
{
    <a class="feature-link" asp-page="/Connectors">
        <span class="feature-mark ja" aria-hidden="true">接続詞</span>
        <span class="feature-body">
            <span class="feature-title">Từ nối N5–N2</span>
            <span class="feature-sub">@Model.ConnectorCount từ nối theo chức năng, lọc theo cấp</span>
        </span>
        <span aria-hidden="true">→</span>
    </a>
}
```

- [ ] **Step 6: CSS**

Thêm vào cuối `src/Japan.Web/wwwroot/css/site.css`:

```css
/* ---------- Connectors ---------- */
.nav-main {
  color: var(--ink);
  font-weight: 600;
  white-space: nowrap;
}

.nav-main:hover {
  color: var(--accent);
}

.feature-link {
  display: flex;
  align-items: center;
  gap: 1rem;
  max-width: 48rem;
  margin: 0 auto 2rem;
  padding: 0.9rem 1.2rem;
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: 14px;
  box-shadow: var(--shadow);
  color: var(--ink);
  transition: border-color 0.15s;
}

.feature-link:hover {
  border-color: var(--accent);
}

.feature-mark {
  flex: none;
  padding: 0.35rem 0.6rem;
  border-radius: 10px;
  background: var(--accent-soft);
  color: var(--accent);
  font-weight: 700;
}

.feature-body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.feature-title {
  font-weight: 700;
}

.feature-sub,
.group-desc,
.level-count {
  color: var(--muted);
  font-size: 0.92rem;
}

.level-filter {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.4rem;
  margin-bottom: 2rem;
}

.level-chip {
  padding: 0.25rem 0.85rem;
  border-radius: 999px;
  border: 1px solid var(--line);
  background: var(--surface);
  color: var(--ink);
  font-weight: 600;
  font-size: 0.9rem;
}

.level-chip:hover,
.level-chip.active {
  background: var(--accent);
  border-color: var(--accent);
  color: var(--surface);
}

.level-count {
  margin-left: auto;
}

.group-ja {
  color: var(--muted);
  font-weight: 500;
  font-size: 0.9em;
  margin-left: 0.35rem;
}

.connector-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 14rem), 1fr));
  gap: 0.75rem;
}

.connector-card {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  padding: 0.85rem 1rem;
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: 12px;
  box-shadow: var(--shadow);
  color: var(--ink);
  transition: border-color 0.15s, transform 0.15s;
}

.connector-card:hover {
  border-color: var(--accent);
  transform: translateY(-1px);
}

.connector-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.connector-word {
  font-size: 1.25rem;
  font-weight: 700;
}

.connector-reading {
  color: var(--muted);
  font-size: 0.85rem;
}

.connector-meaning {
  font-size: 0.92rem;
}

.level-badge {
  flex: none;
  font-size: 0.72rem;
  font-weight: 700;
  padding: 0.05rem 0.45rem;
  border-radius: 6px;
  background: var(--accent-soft);
  color: var(--accent);
}
```

- [ ] **Step 7: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS.

- [ ] **Step 8: Xem bằng mắt**

Run: `dotnet run --project src/Japan.Web` rồi mở http://localhost:5126/tu-noi (sẽ báo "Chưa có dữ liệu từ nối" vì chưa có `data/connectors.json` — đúng kỳ vọng ở giai đoạn này). Kiểm tra header có link "Từ nối" ở cả giao diện sáng và tối, và ở bề rộng điện thoại.

- [ ] **Step 9: Commit**

```bash
git add src/Japan.Web/Pages src/Japan.Web/wwwroot/css/site.css tests/Japan.Tests/Web/ConnectorPagesTests.cs
git commit -m "feat: add the connectors list page with level filter"
```

---

### Task 9: Từ nối trong tìm kiếm chung

**Files:**
- Modify: `src/Japan.Web/Pages/Search.cshtml.cs`
- Modify: `src/Japan.Web/Pages/Search.cshtml` (placeholder)
- Modify: `src/Japan.Web/Pages/Shared/_SearchResults.cshtml`
- Modify: `src/Japan.Web/wwwroot/css/site.css` (thêm `.result-group`)
- Test: `tests/Japan.Tests/Web/SearchPageTests.cs` (thêm test)

**Interfaces:**
- Consumes: `ConnectorRepository.Search(string?) : IReadOnlyList<ConnectorHit>` (Task 6), `Highlight.Mark` (đã có).
- Produces: `record SearchResultsViewModel(string? Query, IReadOnlyList<SearchHit> Hits, IReadOnlyList<ConnectorHit> ConnectorHits)`.

- [ ] **Step 1: Thêm test thất bại vào `SearchPageTests`**

```csharp
    [Fact]
    public async Task FullPage_FindsConnectors_InTheirOwnGroup()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?q=cho%20nen");

        Assert.Contains("class=\"result-group\">Từ nối<", html);
        Assert.DoesNotContain("class=\"result-group\">Ngữ pháp<", html);
        Assert.Contains("href=\"/tu-noi/dakara\"", html);
        Assert.Contains("1 kết quả", html);
    }

    [Fact]
    public async Task GrammarOnlyQuery_HasNoConnectorGroup()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?q=tranh%20thu");

        Assert.Contains("class=\"result-group\">Ngữ pháp<", html);
        Assert.DoesNotContain("class=\"result-group\">Từ nối<", html);
    }

    [Fact]
    public async Task Partial_IncludesConnectorHits()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?handler=Partial&q=%E3%81%97%E3%81%8B%E3%81%97");

        Assert.DoesNotContain("<html", html);
        Assert.Contains("href=\"/tu-noi/shikashi\"", html);
        Assert.Contains("<mark>しかし</mark>", html);
    }
```

- [ ] **Step 2: Chạy test, xác nhận fail**

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~SearchPageTests"`
Expected: 3 test mới FAIL (không có `result-group`, không có link `/tu-noi/...`).

- [ ] **Step 3: Page model**

Thay toàn bộ `src/Japan.Web/Pages/Search.cshtml.cs`:

```csharp
using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed record SearchResultsViewModel(string? Query, IReadOnlyList<SearchHit> Hits, IReadOnlyList<ConnectorHit> ConnectorHits);

public sealed class SearchModel(GrammarRepository repo, ConnectorRepository connectors) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public SearchResultsViewModel Results { get; private set; } = new(null, [], []);

    public void OnGet() => Results = new SearchResultsViewModel(Query, repo.Search(Query), connectors.Search(Query));

    public PartialViewResult OnGetPartial()
    {
        OnGet();
        return Partial("_SearchResults", Results);
    }
}
```

- [ ] **Step 4: Partial kết quả**

Thay toàn bộ `src/Japan.Web/Pages/Shared/_SearchResults.cshtml`:

```cshtml
@model SearchResultsViewModel
@{
    var total = Model.Hits.Count + Model.ConnectorHits.Count;
}
@if (total > 0)
{
    <p class="result-count">@total kết quả</p>
    @if (Model.Hits.Count > 0)
    {
        <h2 class="result-group">Ngữ pháp</h2>
        <div class="result-list mb-5">
            @foreach (var hit in Model.Hits)
            {
                <a class="result-card" asp-page="/Grammar" asp-route-id="@hit.Point.Id">
                    <span class="result-meta">
                        <span class="tag">Bài @hit.Point.Lesson</span>
                        <span class="tag tag-field">@hit.Field</span>
                    </span>
                    <span class="result-pattern ja">@Highlight.Mark(hit.Point.Pattern, Model.Query)</span>
                    <span class="result-snippet jp">@Highlight.Mark(hit.Snippet, Model.Query)</span>
                </a>
            }
        </div>
    }
    @if (Model.ConnectorHits.Count > 0)
    {
        <h2 class="result-group">Từ nối</h2>
        <div class="result-list mb-5">
            @foreach (var hit in Model.ConnectorHits)
            {
                <a class="result-card" asp-page="/Connector" asp-route-id="@hit.Connector.Id">
                    <span class="result-meta">
                        <span class="tag">@hit.Connector.Level</span>
                        <span class="tag tag-field">@hit.Field</span>
                    </span>
                    <span class="result-pattern ja">@Highlight.Mark(hit.Connector.Word, Model.Query)</span>
                    <span class="result-snippet jp">@Highlight.Mark(hit.Snippet, Model.Query)</span>
                </a>
            }
        </div>
    }
}
else if (!string.IsNullOrWhiteSpace(Model.Query) && Model.Query.Trim().Trim('～', '~', '〜').Length > 0)
{
    <p class="empty-state">Không tìm thấy kết quả cho "@Model.Query".</p>
}
```

Trong `src/Japan.Web/Pages/Search.cshtml`, đổi `placeholder` thành `"Tìm mẫu ngữ pháp, từ nối, nghĩa tiếng Việt hoặc câu ví dụ…"` và `aria-label` thành `"Tìm kiếm"`.

Thêm vào cuối `src/Japan.Web/wwwroot/css/site.css`:

```css
.result-group {
  font-size: 0.85rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--muted);
  margin: 0 0 0.6rem;
}
```

- [ ] **Step 5: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS (gồm các test tìm kiếm cũ: `1 kết quả` cho `tranh thu`, partial `<mark>間に</mark>`, `EmptyQueries_Return200WithoutResults`).

- [ ] **Step 6: Commit**

```bash
git add src/Japan.Web/Pages/Search.cshtml src/Japan.Web/Pages/Search.cshtml.cs src/Japan.Web/Pages/Shared/_SearchResults.cshtml src/Japan.Web/wwwroot/css/site.css tests/Japan.Tests/Web/SearchPageTests.cs
git commit -m "feat: include connectors in search results"
```

---

## Hướng dẫn viết nội dung (dùng cho Task 10–12)

Mỗi file `content/connectors/<id>.json` có dạng một `ConnectorGroup`. Từ nối trong file viết theo thứ tự trong bảng của task (importer tự sắp lại theo cấp, giữ thứ tự cho từ cùng cấp).

Yêu cầu cho **mỗi** từ nối:
- `meaning`: 2–4 cách dịch ngắn, ngăn cách bằng dấu phẩy.
- `explanation`: 2–4 câu tiếng Việt nói rõ **quan hệ giữa hai câu** mà từ này đòi hỏi (đây là điều đề thi 文章の文法 kiểm tra), sắc thái và văn nói/văn viết. Tách đoạn bằng `\n\n`.
- `usage`: 2–3 ý: vị trí trong câu, kết hợp hay gặp, câu sau thường là gì (ý chí, mệnh lệnh, nhận xét…).
- `pitfalls`: 1–2 lỗi hay gặp, viết theo kiểu `✗「…」 … ✓「…」` như phần Bổ sung hiện có.
- `comparisons`: so với các từ dễ nhầm ghi trong bảng; `targetId` trỏ tới id trong bảng (chỉ trỏ tới từ thuộc task hiện tại hoặc task trước), `null` nếu từ đem so không có trong danh sách.
- `examples`: **3** ví dụ (tối thiểu 2), mỗi ví dụ gồm hai câu được nối bằng chính từ đó, từ vựng hợp cấp, câu tự nhiên; ít nhất một ví dụ mang dáng văn viết nếu từ dùng được trong văn viết.
- Tự viết, không chép câu ví dụ từ trang web hay sách.

Mẫu một mục hoàn chỉnh (dùng đúng nội dung này cho `dakara`):

```json
{
  "id": "dakara",
  "word": "だから",
  "reading": "だから",
  "level": "N4",
  "meaning": "vì vậy, cho nên, thế nên",
  "register": "spoken",
  "explanation": "Câu trước nêu lý do hoặc nguyên nhân, câu sau nêu kết quả, kết luận hay quyết định của người nói. だから mang tính chủ quan: người nói tự rút ra kết luận, nên câu sau có thể là ý chí, yêu cầu hay nhận định.\n\nĐây là từ của văn nói. Trong văn viết trang trọng (báo cáo, luận văn) thường đổi sang したがって hoặc そのため; dạng lịch sự trong hội thoại là ですから.",
  "usage": [
    "Đứng đầu câu sau: 「A。だから、B。」; dạng lịch sự là ですから.",
    "Câu sau được phép là ý chí, mệnh lệnh, lời khuyên: 「明日は試験だ。だから、今日は早く寝なさい。」.",
    "Đứng một mình đầu câu trả lời để nhấn mạnh ý mình đã nói: 「だから、言ったでしょう。」 (đã bảo rồi mà)."
  ],
  "pitfalls": [
    "✗「本研究の結果は以下のとおりである。だから、…」 — văn viết học thuật không dùng だから; ✓ dùng したがって／そのため.",
    "Khi câu sau chỉ là sự việc khách quan xảy ra tiếp theo (không phải kết luận của người nói), それで tự nhiên hơn: ✓「電車が遅れた。それで、遅刻した。」."
  ],
  "comparisons": [
    { "pattern": "それで", "targetId": "sorede", "difference": "それで nối sự việc với kết quả khách quan đã xảy ra, câu sau không dùng ý chí hay mệnh lệnh; だから dùng được cả với ý chí, mệnh lệnh." },
    { "pattern": "したがって", "targetId": "shitagatte", "difference": "したがって là văn viết, kết luận mang tính logic; だから là văn nói, kết luận mang tính chủ quan." }
  ],
  "examples": [
    { "japanese": "明日は朝早く出かける。だから、今夜は早く寝るつもりだ。", "vietnamese": "Sáng mai tôi ra ngoài sớm. Vì vậy tối nay tôi định đi ngủ sớm." },
    { "japanese": "この店は安くておいしい。だから、いつも混んでいる。", "vietnamese": "Quán này rẻ mà ngon. Cho nên lúc nào cũng đông." },
    { "japanese": "道が凍っていて危ないです。ですから、気をつけてください。", "vietnamese": "Đường đóng băng nguy hiểm lắm. Vì thế xin hãy cẩn thận." }
  ]
}
```

Sau mỗi task nội dung:
1. Chạy `dotnet run --project src/Japan.Importer -- connectors`. Sửa đến khi không còn lỗi.
2. Mở `data/connectors.json`, đọc trường `ruby` của các ví dụ vừa viết; chỗ nào furigana đọc sai thì thêm vào `content/furigana-overrides.json` (ví dụ `"一方": "いっぽう"`) và chạy lại importer.
3. Chạy `dotnet test Japan.slnx`.

---

### Task 10: Nội dung nhóm 1–3 (thuận tiếp, nghịch tiếp, bổ sung) + test dữ liệu thật

**Files:**
- Create: `content/connectors/thuan-tiep.json`, `content/connectors/nghich-tiep.json`, `content/connectors/bo-sung.json`
- Create: `data/connectors.json` (sinh bởi importer)
- Modify: `content/furigana-overrides.json` (nếu cần)
- Test: `tests/Japan.Tests/Data/ConnectorDataTests.cs`

**Interfaces:**
- Consumes: `ConnectorLoader.Load` (Task 3), `ConnectorJson`, `ConnectorValidator`, `RepoRoot.Find(string)` (đã có trong Importer).

Nhóm và từ (theo đúng thứ tự):

| File | order | title | jaTitle | description |
|---|---|---|---|---|
| `thuan-tiep` | 1 | Thuận tiếp — kết quả, hệ quả | 順接 | Câu sau là kết quả, hệ quả hoặc hành động tiếp theo do câu trước dẫn tới. |
| `nghich-tiep` | 2 | Nghịch tiếp — trái ngược, ngoài dự đoán | 逆接 | Câu sau trái với điều câu trước khiến người đọc chờ đợi. |
| `bo-sung` | 3 | Bổ sung — thêm vào | 添加 | Câu sau thêm một thông tin cùng chiều với câu trước. |

| Nhóm | id | word | reading | level | register | meaning (gợi ý) | So sánh với (targetId) |
|---|---|---|---|---|---|---|---|
| thuan-tiep | dakara | だから | だから | N4 | spoken | (mẫu ở trên) | sorede, shitagatte |
| | sorede | それで | それで | N4 | spoken | vì thế nên, rồi thì | dakara, sokode |
| | suruto | すると | すると | N4 | both | thế là, ngay sau đó thì | sokode |
| | sokode | そこで | そこで | N3 | both | vì vậy nên (quyết định làm gì) | sorede, suruto |
| | sonotame | そのため | そのため | N3 | written | vì lý do đó, do vậy | shitagatte, dakara |
| | shitagatte | したがって | したがって | N3 | written | do đó, vì thế | dakara, sonotame |
| nghich-tiep | demo | でも | でも | N5 | spoken | nhưng, tuy vậy | shikashi, keredomo |
| | shikashi | しかし | しかし | N5 | both | tuy nhiên, nhưng | demo, daga |
| | keredomo | けれども | けれども | N5 | both | tuy nhiên, nhưng mà | shikashi |
| | soredemo | それでも | それでも | N4 | both | dù vậy vẫn, mặc dù thế | sorenanoni |
| | daga | だが | だが | N3 | written | nhưng, thế nhưng | shikashi |
| | tokoroga | ところが | ところが | N3 | both | thế nhưng (trái dự đoán) | shikashi, sorenanoni |
| | sorenanoni | それなのに | それなのに | N2 | spoken | vậy mà, thế mà | soredemo, tokoroga |
| bo-sung | soreni | それに | それに | N4 | spoken | hơn nữa, với lại | sarani, shikamo |
| | sarani | さらに | さらに | N3 | both | hơn nữa, thêm vào đó | soreni, sonoue |
| | shikamo | しかも | しかも | N3 | both | mà lại còn, hơn thế | soreni, omakeni |
| | sonoue | そのうえ | そのうえ | N2 | written | thêm vào đó, hơn thế nữa | sarani |
| | omakeni | おまけに | おまけに | N2 | spoken | đã thế lại còn (thường là chuyện xấu) | shikamo |

- [ ] **Step 1: Viết test dữ liệu thật (fail vì chưa có file)**

`tests/Japan.Tests/Data/ConnectorDataTests.cs`:

```csharp
using System.Text.Json;
using Japan.Core;
using Japan.Importer;
using Japan.Importer.Assembly;

namespace Japan.Tests.Data;

public class ConnectorDataTests
{
    private static readonly string Root = RepoRoot.Find(AppContext.BaseDirectory);

    private static ConnectorBook Data() =>
        ConnectorJson.Deserialize(File.ReadAllText(Path.Combine(Root, "data", "connectors.json")));

    [Fact]
    public void ConnectorsJson_IsValid_AndEveryExampleHasFurigana()
    {
        var book = Data();

        Assert.Empty(ConnectorValidator.Validate(book.Groups));
        Assert.All(book.Groups.SelectMany(g => g.Connectors).SelectMany(c => c.Examples),
            e => Assert.False(string.IsNullOrEmpty(e.Ruby), e.Japanese));
    }

    [Fact]
    public void ConnectorsJson_MatchesContentFiles()
    {
        var content = ConnectorLoader.Load(Path.Combine(Root, "content", "connectors"));
        Assert.Empty(content.Errors);

        static string Key(Connector c) => JsonSerializer.Serialize(
            c with { Examples = c.Examples.Select(e => e with { Ruby = null }).ToList() }, GrammarJson.Options);
        static string GroupKey(ConnectorGroup g) => $"{g.Id}|{g.Order}|{g.Title}|{g.JaTitle}|{g.Description}";

        var data = Data();
        Assert.Equal(content.Groups.Select(GroupKey).Order(), data.Groups.Select(GroupKey).Order());
        Assert.Equal(
            content.Groups.SelectMany(g => g.Connectors).Select(Key).Order(),
            data.Groups.SelectMany(g => g.Connectors).Select(Key).Order());
    }
}
```

Run: `dotnet test Japan.slnx --filter "FullyQualifiedName~ConnectorDataTests"`
Expected: FAIL — `FileNotFoundException` / `DirectoryNotFound` (chưa có nội dung).

- [ ] **Step 2: Viết `content/connectors/thuan-tiep.json`** — 6 từ theo bảng, `dakara` dùng đúng mẫu ở trên, các từ khác theo "Hướng dẫn viết nội dung".

- [ ] **Step 3: Viết `content/connectors/nghich-tiep.json`** — 7 từ theo bảng.

- [ ] **Step 4: Viết `content/connectors/bo-sung.json`** — 5 từ theo bảng.

- [ ] **Step 5: Sinh dữ liệu và soát furigana**

Run: `dotnet run --project src/Japan.Importer -- connectors`
Expected: `Đã ghi …data\connectors.json: 3 nhóm, 18 từ nối (N5: 3, N4: 5, N3: 7, N2: 3).`

Đọc trường `ruby` trong `data/connectors.json`, sửa cách đọc sai qua `content/furigana-overrides.json`, chạy lại lệnh trên.

- [ ] **Step 6: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS (gồm `ConnectorDataTests`).

- [ ] **Step 7: Xem trên web**

Run: `dotnet run --project src/Japan.Web`, mở http://localhost:5126/tu-noi và vài trang chi tiết; kiểm tra furigana, link so sánh, phím ← / →, lọc `N3`.

- [ ] **Step 8: Commit**

```bash
git add content/connectors content/furigana-overrides.json data/connectors.json tests/Japan.Tests/Data/ConnectorDataTests.cs
git commit -m "feat: add result, contrast and addition connectors"
```

---

### Task 11: Nội dung nhóm 4–6 (song song, đối lập–lựa chọn, giải thích)

**Files:**
- Create: `content/connectors/song-song.json`, `content/connectors/doi-lap-lua-chon.json`, `content/connectors/giai-thich.json`
- Modify: `data/connectors.json` (sinh lại), `content/furigana-overrides.json` (nếu cần)

| File | order | title | jaTitle | description |
|---|---|---|---|---|
| `song-song` | 4 | Song song — liệt kê | 並立 | Đặt hai sự việc ngang hàng cạnh nhau. |
| `doi-lap-lua-chon` | 5 | Đối lập, lựa chọn | 対比・選択 | So sánh hai mặt, hoặc đưa ra các khả năng để chọn. |
| `giai-thich` | 6 | Giải thích, tóm lại | 説明 | Câu sau nói lại, tóm tắt hoặc nêu lý do cho câu trước. |

| Nhóm | id | word | reading | level | register | meaning (gợi ý) | So sánh với (targetId) |
|---|---|---|---|---|---|---|---|
| song-song | mata | また | また | N5 | both | ngoài ra, đồng thời, lại còn | soreni, oyobi |
| | oyobi | および | および | N2 | written | và, cùng với | mata |
| doi-lap-lua-chon | matawa | または | または | N4 | both | hoặc là | aruiwa, soretomo |
| | soretomo | それとも | それとも | N3 | spoken | hay là (trong câu hỏi) | matawa |
| | aruiwa | あるいは | あるいは | N3 | written | hoặc, hay | matawa |
| | ippou | 一方 | いっぽう | N3 | written | mặt khác, trong khi đó | shikashi |
| giai-thich | tsumari | つまり | つまり | N3 | both | tức là, nói cách khác | sunawachi, yousuruni |
| | sunawachi | すなわち | すなわち | N3 | written | tức là, nghĩa là | tsumari |
| | yousuruni | 要するに | ようするに | N3 | both | tóm lại, nói gọn là | tsumari, kekkyoku |
| | nazenara | なぜなら | なぜなら | N3 | written | bởi vì | toiunowa |
| | toiunowa | というのは | というのは | N3 | spoken | là vì, bởi vì | nazenara |
| | kekkyoku | 結局 | けっきょく | N3 | both | rốt cuộc, cuối cùng thì | yousuruni |

Lưu ý riêng: `nazenara` và `toiunowa` thường đi với cuối câu `～からだ／～のだ` — đưa vào `usage` và một `pitfall`.

- [ ] **Step 1: Viết `content/connectors/song-song.json`** — 2 từ.
- [ ] **Step 2: Viết `content/connectors/doi-lap-lua-chon.json`** — 4 từ.
- [ ] **Step 3: Viết `content/connectors/giai-thich.json`** — 6 từ.
- [ ] **Step 4: Sinh dữ liệu và soát furigana**

Run: `dotnet run --project src/Japan.Importer -- connectors`
Expected: `Đã ghi …: 6 nhóm, 30 từ nối (N5: 4, N4: 6, N3: 16, N2: 4).`

- [ ] **Step 5: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add content/connectors content/furigana-overrides.json data/connectors.json
git commit -m "feat: add listing, choice and explanation connectors"
```

---

### Task 12: Nội dung nhóm 7–10 (bổ sung điều kiện, chuyển ý, ví dụ, trình tự)

**Files:**
- Create: `content/connectors/bo-sung-dieu-kien.json`, `content/connectors/chuyen-y.json`, `content/connectors/vi-du.json`, `content/connectors/thu-tu.json`
- Modify: `data/connectors.json` (sinh lại), `content/furigana-overrides.json` (nếu cần)

| File | order | title | jaTitle | description |
|---|---|---|---|---|
| `bo-sung-dieu-kien` | 7 | Bổ sung điều kiện, ngoại lệ | 補足 | Câu sau thêm điều kiện, ngoại lệ hoặc thông tin phụ cho câu trước. |
| `chuyen-y` | 8 | Chuyển ý | 転換 | Chuyển sang chủ đề mới hoặc bước tiếp theo của câu chuyện. |
| `vi-du` | 9 | Đưa ví dụ | 例示 | Câu sau đưa ví dụ cụ thể cho điều vừa nói. |
| `thu-tu` | 10 | Trình tự | 順序 | Sắp xếp các bước hay sự việc theo thứ tự. |

| Nhóm | id | word | reading | level | register | meaning (gợi ý) | So sánh với (targetId) |
|---|---|---|---|---|---|---|---|
| bo-sung-dieu-kien | tada | ただ | ただ | N3 | spoken | chỉ có điều, có điều là | tadashi, shikashi |
| | tadashi | ただし | ただし | N2 | written | tuy nhiên (nêu điều kiện, ngoại lệ) | tada, nao |
| | mottomo | もっとも | もっとも | N3 | both | tuy vậy thì cũng, dĩ nhiên là | tadashi |
| | nao | なお | なお | N2 | written | ngoài ra, xin lưu ý thêm | tadashi, chinamini |
| | chinamini | ちなみに | ちなみに | N2 | spoken | nhân tiện nói thêm | nao, tokorode |
| chuyen-y | dewa | では / じゃあ | では / じゃあ | N5 | both | vậy thì, thế thì | sorenara, sate |
| | tokorode | ところで | ところで | N3 | spoken | nhân tiện, mà này | sate |
| | sate | さて | さて | N3 | both | nào, giờ thì (sang việc tiếp) | tokorode |
| | sorenara | それなら | それなら | N2 | spoken | nếu vậy thì | dewa |
| vi-du | tatoeba | 例えば | たとえば | N4 | both | ví dụ như, chẳng hạn | — |
| thu-tu | sorekara | それから | それから | N5 | both | sau đó, rồi thì | tsugini |
| | mazu | まず | まず | N3 | both | trước hết, đầu tiên | tsugini |
| | tsugini | 次に | つぎに | N4 | both | tiếp theo | sorekara, saigoni |
| | saigoni | 最後に | さいごに | N4 | both | cuối cùng | tsugini |

Lưu ý riêng: `dewa` — ví dụ phải có cả では lẫn じゃあ (một câu trang trọng, một câu hội thoại). `tatoeba` không có từ cùng nhóm để so sánh: để `comparisons` rỗng hoặc so với `つまり` (`targetId: "tsumari"`) — つまり nói lại cùng ý, 例えば đưa trường hợp cụ thể.

- [ ] **Step 1: Viết `content/connectors/bo-sung-dieu-kien.json`** — 5 từ.
- [ ] **Step 2: Viết `content/connectors/chuyen-y.json`** — 4 từ.
- [ ] **Step 3: Viết `content/connectors/vi-du.json`** — 1 từ.
- [ ] **Step 4: Viết `content/connectors/thu-tu.json`** — 4 từ.
- [ ] **Step 5: Sinh dữ liệu và soát furigana**

Run: `dotnet run --project src/Japan.Importer -- connectors`
Expected: `Đã ghi …: 10 nhóm, 44 từ nối (N5: 6, N4: 9, N3: 21, N2: 8).`

- [ ] **Step 6: Chạy toàn bộ test**

Run: `dotnet test Japan.slnx`
Expected: PASS.

- [ ] **Step 7: Soát toàn bộ trên web**

Run: `dotnet run --project src/Japan.Web`, mở http://localhost:5126/tu-noi; lọc lần lượt N5, N4, N3, N2 và đối chiếu số từ với dòng thống kê ở Step 5; đi hết chuỗi trang chi tiết bằng phím → từ `demo`… đến `saigoni`; tìm thử `nhan tien`, `ところで`, `いっぽう`.

- [ ] **Step 8: Commit**

```bash
git add content/connectors content/furigana-overrides.json data/connectors.json
git commit -m "feat: add supplement, topic-change, example and sequence connectors"
```

---

### Task 13: Deploy và README

**Files:**
- Modify: `Dockerfile`, `Dockerfile.vercel`
- Modify: `README.md`

- [ ] **Step 1: Chép `connectors.json` vào image**

Trong **cả** `Dockerfile` và `Dockerfile.vercel`, sau dòng `COPY data/grammar.json ./data/grammar.json` thêm:

```dockerfile
COPY data/connectors.json ./data/connectors.json
```

và sau dòng `ENV GrammarData__Path=data/grammar.json` thêm:

```dockerfile
ENV ConnectorData__Path=data/connectors.json
```

- [ ] **Step 2: README**

Trong `README.md`:
- Dòng mô tả đầu: thêm "và mục **Từ nối** (接続詞) N5–N2: 44 từ chia theo chức năng".
- Mục "Tính năng": thêm gạch đầu dòng `- Mục **Từ nối** ở /tu-noi: chia theo chức năng, lọc theo cấp N5–N2, mỗi từ có giải thích, lỗi hay gặp, so sánh và ví dụ; tìm kiếm chung trả cả ngữ pháp lẫn từ nối.`
- Mục "Cập nhật dữ liệu": thêm đoạn

````markdown
Từ nối nằm trong `content/connectors/<nhóm>.json` (mỗi nhóm chức năng một file). Sửa xong chạy:

```powershell
dotnet run --project src/Japan.Importer -- connectors
```

Lệnh này không cần bộ docx, kiểm tra nội dung (trùng id, cấp, `targetId`, ít nhất 2 ví dụ…), sinh furigana và ghi `data/connectors.json`. Nhớ commit cả `data/connectors.json` — bản deploy chỉ đọc file này.
````

- [ ] **Step 3: Kiểm tra build image (nếu máy có Docker)**

Run: `docker build -f Dockerfile.vercel -t japan-web-test .`
Expected: build thành công. Nếu không có Docker, ghi rõ trong báo cáo là bước này chưa chạy.

- [ ] **Step 4: Chạy toàn bộ test lần cuối**

Run: `dotnet test Japan.slnx`
Expected: PASS, tổng số test ≈ 121 + khoảng 50 test mới, không có test nào fail.

- [ ] **Step 5: Commit**

```bash
git add Dockerfile Dockerfile.vercel README.md
git commit -m "feat: ship connectors.json in the container images"
```
