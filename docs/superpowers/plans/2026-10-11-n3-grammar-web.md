# Web tra cứu ngữ pháp N3 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Web chạy local (ASP.NET Core Razor Pages) để tra cứu 106 điểm ngữ pháp N3, dữ liệu import từ file Word/Markdown của người dùng, kèm phần mở rộng do AI viết và furigana bật/tắt được.

**Architecture:** `Japan.Importer` (console) đọc `.docx` (OpenXML) / `.md` (Markdig) qua một `DocumentBuilder` chung, ghép với `content/enrichment/*.json`, sinh furigana bằng NMeCab rồi ghi một file `data/grammar.json`. `Japan.Web` chỉ nạp file JSON đó vào bộ nhớ (tự nạp lại khi file đổi) và render bằng Razor Pages. `Japan.Core` chứa mô hình dữ liệu, chuẩn hoá chuỗi, định dạng ruby và tìm kiếm — dùng chung cho cả hai.

**Tech Stack:** .NET 10, C#, ASP.NET Core Razor Pages, Bootstrap 5 (local từ template), DocumentFormat.OpenXml, Markdig, LibNMeCab + IPADIC, xUnit, Microsoft.AspNetCore.Mvc.Testing.

**Spec:** `docs/superpowers/specs/2026-10-11-n3-grammar-web-design.md`

## Global Constraints

- Target `net10.0`; `Nullable` = enable; `TreatWarningsAsErrors` = true; `ImplicitUsings` = enable.
- Package versions: `DocumentFormat.OpenXml` 3.5.1, `Markdig` 1.4.0, `LibNMeCab` 0.10.2, `LibNMeCab.IpaDicBin` 0.10.0, `Microsoft.AspNetCore.Mvc.Testing` 10.0.12.
- Web không gọi mạng ngoài: không CDN, không font ngoài (dùng `"Yu Gothic UI", "Yu Gothic", Meiryo, sans-serif`), không gọi API AI.
- Id điểm ngữ pháp: `b{số bài}-{số thứ tự điểm trong bài}` (vd `b7-1`).
- Định dạng ruby lưu trong JSON: `[漢字|かんじ]`, phần không có kanji để nguyên.
- Nguồn: chỉ file `NguPhap_N3_File{n}_*.docx|md`; cùng số file thì ưu tiên `.md`. Tổng mong đợi: **106 điểm, Bài 1–30**.
- Thư mục nguồn mặc định: `C:\Users\Admin\Downloads\Tiếng Nhật - N3\Ngữ pháp`.
- Toàn bộ chữ trên giao diện và thông báo lỗi bằng tiếng Việt.
- JSON: camelCase, UTF-8 không BOM, không escape ký tự Nhật/Việt.
- Lệnh chạy từ thư mục gốc repo `D:\Code\.Net\Japan` (PowerShell).

## Review Focus

1. **File Word đang mở** (File01, File08 lúc thiết kế) — importer phải đọc được nếu Word cho chia sẻ đọc; nếu bị khoá thật thì báo "Hãy đóng Word: <tên file>" và exit ≠ 0, không ghi đè `grammar.json`. → Task 5 (test khoá file), Task 8.
2. **Chưa có `data/grammar.json`** khi mở web lần đầu — trang chủ vẫn trả 200 và hướng dẫn chạy importer, không văng exception. → Task 10 (test thiếu dữ liệu).
3. **Ô tìm kiếm rỗng, chỉ có khoảng trắng hoặc chỉ có `～`** — trả về không kết quả, trang 200. → Task 9 và Task 11.
4. **File enrichment hỏng** (JSON sai cú pháp, tên file sai, id không tồn tại, `targetId` trỏ sai) — importer báo lỗi kèm tên file và không ghi `grammar.json`. → Task 6, Task 8.
5. **Heading lệch chuẩn** (số toàn độ rộng `３．`, khoảng trắng thừa, chữ Việt dạng tổ hợp NFD) — vẫn nhận đúng bài/điểm. → Task 3.

---

## File Structure

```
Japan.slnx
Directory.Build.props
.gitignore
src/Japan.Core/
  Models.cs                 GrammarBook, Lesson, GrammarPoint, Section, Table, Example
  Enrichment.cs             GrammarEnrichment, FormationRule, Comparison
  GrammarJson.cs            JsonSerializerOptions + Serialize/Deserialize
  Kana.cs                   katakana → hiragana, IsKanji
  TextNormalizer.cs         chuẩn hoá cho tìm kiếm
  RubyText.cs               parse/format "[漢字|かんじ]"
  GrammarSearch.cs          tìm kiếm trong bộ nhớ
src/Japan.Importer/
  Parsing/SourceDocument.cs SourceDocument, PointRange, RawLesson, RawPoint
  Parsing/DocumentBuilder.cs máy trạng thái chung cho docx + md
  Parsing/MarkdownGrammarReader.cs
  Parsing/DocxGrammarReader.cs
  Parsing/SourceFileLockedException.cs
  Assembly/EnrichmentLoader.cs
  Assembly/BookAssembler.cs
  Furigana/IFuriganaGenerator.cs
  Furigana/NMeCabFuriganaGenerator.cs
  Furigana/FuriganaOverrides.cs
  Furigana/FuriganaApplier.cs
  SourceDiscovery.cs
  RepoRoot.cs
  ImportPipeline.cs
  Program.cs
  appsettings.json
src/Japan.Web/
  Program.cs
  appsettings.json
  Services/GrammarDataOptions.cs
  Services/GrammarRepository.cs
  Rendering/RubyHtml.cs
  Rendering/Highlight.cs
  Pages/_ViewImports.cshtml
  Pages/Shared/_Layout.cshtml
  Pages/Shared/_Section.cshtml, _Table.cshtml, _Examples.cshtml, _SearchResults.cshtml
  Pages/Index.cshtml(.cs), Lesson.cshtml(.cs), Grammar.cshtml(.cs), Search.cshtml(.cs)
  wwwroot/css/site.css, wwwroot/js/site.js
tests/Japan.Tests/
  TestBooks.cs, DocxFixture.cs
  Core/GrammarJsonTests.cs, TextNormalizerTests.cs, RubyTextTests.cs, GrammarSearchTests.cs
  Importer/DocumentBuilderTests.cs, MarkdownGrammarReaderTests.cs, DocxGrammarReaderTests.cs,
           BookAssemblerTests.cs, EnrichmentLoaderTests.cs, FuriganaTests.cs, ImportPipelineTests.cs
  Web/GrammarWebFactory.cs, PagesTests.cs, SearchPageTests.cs, RenderingTests.cs
content/
  enrichment/b{n}-{i}.json
  furigana-overrides.json
data/
  grammar.json
```

---

### Task 1: Solution, Core models, JSON

**Files:**
- Create: `Japan.slnx`, `Directory.Build.props`, `.gitignore`, 4 project, `src/Japan.Core/Models.cs`, `src/Japan.Core/Enrichment.cs`, `src/Japan.Core/GrammarJson.cs`
- Test: `tests/Japan.Tests/TestBooks.cs`, `tests/Japan.Tests/Core/GrammarJsonTests.cs`

**Interfaces:**
- Consumes: —
- Produces:
  - `record GrammarBook(IReadOnlyList<Lesson> Lessons, DateTimeOffset GeneratedAt)`
  - `record Lesson(int Number, string Title, IReadOnlyList<GrammarPoint> Points, IReadOnlyList<Table> QuickComparisons)`
  - `record GrammarPoint(string Id, int Lesson, int Index, string Pattern, Section Meaning, Section Structure, Section? Note, IReadOnlyList<Example> Examples, GrammarEnrichment? Enrichment, string SourceFile)`
  - `record Section(IReadOnlyList<string> Paragraphs, IReadOnlyList<Table> Tables)` với `Section.Empty`, `bool IsEmpty`
  - `record Table(IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<string>> Rows)`
  - `record Example(string Japanese, string Vietnamese, string? Ruby = null)`
  - `record GrammarEnrichment(string Explanation, IReadOnlyList<FormationRule> Formation, IReadOnlyList<string> Usage, IReadOnlyList<string> Pitfalls, IReadOnlyList<Comparison> Comparisons, IReadOnlyList<Example> Examples)`
  - `record FormationRule(string WordType, string Rule, IReadOnlyList<string> Samples)`
  - `record Comparison(string Pattern, string? TargetId, string Difference)`
  - `static class GrammarJson { JsonSerializerOptions Options; string Serialize(GrammarBook); GrammarBook Deserialize(string) }`
  - `static class TestBooks { GrammarBook Sample(); GrammarPoint Point(...) }` (test helper)

- [ ] **Step 1: Scaffold solution và project**

```powershell
dotnet new sln -n Japan
dotnet new gitignore
dotnet new classlib -n Japan.Core -o src/Japan.Core -f net10.0
dotnet new console -n Japan.Importer -o src/Japan.Importer -f net10.0
dotnet new webapp -n Japan.Web -o src/Japan.Web -f net10.0
dotnet new xunit -n Japan.Tests -o tests/Japan.Tests -f net10.0
dotnet sln add src/Japan.Core src/Japan.Importer src/Japan.Web tests/Japan.Tests
dotnet add src/Japan.Importer reference src/Japan.Core
dotnet add src/Japan.Web reference src/Japan.Core
dotnet add tests/Japan.Tests reference src/Japan.Core src/Japan.Importer src/Japan.Web
dotnet add src/Japan.Importer package DocumentFormat.OpenXml --version 3.5.1
dotnet add src/Japan.Importer package Markdig --version 1.4.0
dotnet add src/Japan.Importer package LibNMeCab --version 0.10.2
dotnet add src/Japan.Importer package LibNMeCab.IpaDicBin --version 0.10.0
dotnet add tests/Japan.Tests package DocumentFormat.OpenXml --version 3.5.1
dotnet add tests/Japan.Tests package Microsoft.AspNetCore.Mvc.Testing --version 10.0.12
Remove-Item src/Japan.Core/Class1.cs, tests/Japan.Tests/UnitTest1.cs
Remove-Item src/Japan.Web/Pages/Privacy.cshtml, src/Japan.Web/Pages/Privacy.cshtml.cs
```

Nếu `dotnet new sln` sinh `Japan.sln` thay vì `Japan.slnx` (SDK cũ), chạy lại với `--format slnx`.

- [ ] **Step 2: Tạo `Directory.Build.props` ở gốc repo**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Viết test helper `tests/Japan.Tests/TestBooks.cs`**

```csharp
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
```

- [ ] **Step 4: Viết test thất bại `tests/Japan.Tests/Core/GrammarJsonTests.cs`**

```csharp
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
```

- [ ] **Step 5: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~GrammarJsonTests"`
Expected: build FAIL — `GrammarBook`, `GrammarJson`… chưa tồn tại.

- [ ] **Step 6: Viết `src/Japan.Core/Models.cs`**

```csharp
using System.Text.Json.Serialization;

namespace Japan.Core;

public sealed record GrammarBook(IReadOnlyList<Lesson> Lessons, DateTimeOffset GeneratedAt);

public sealed record Lesson(
    int Number,
    string Title,
    IReadOnlyList<GrammarPoint> Points,
    IReadOnlyList<Table> QuickComparisons);

public sealed record GrammarPoint(
    string Id,
    int Lesson,
    int Index,
    string Pattern,
    Section Meaning,
    Section Structure,
    Section? Note,
    IReadOnlyList<Example> Examples,
    GrammarEnrichment? Enrichment,
    string SourceFile);

public sealed record Section(IReadOnlyList<string> Paragraphs, IReadOnlyList<Table> Tables)
{
    public static Section Empty { get; } = new([], []);

    [JsonIgnore]
    public bool IsEmpty => Paragraphs.Count == 0 && Tables.Count == 0;
}

public sealed record Table(IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record Example(string Japanese, string Vietnamese, string? Ruby = null);
```

- [ ] **Step 7: Viết `src/Japan.Core/Enrichment.cs`**

```csharp
namespace Japan.Core;

public sealed record GrammarEnrichment(
    string Explanation,
    IReadOnlyList<FormationRule> Formation,
    IReadOnlyList<string> Usage,
    IReadOnlyList<string> Pitfalls,
    IReadOnlyList<Comparison> Comparisons,
    IReadOnlyList<Example> Examples);

public sealed record FormationRule(string WordType, string Rule, IReadOnlyList<string> Samples);

public sealed record Comparison(string Pattern, string? TargetId, string Difference);
```

- [ ] **Step 8: Viết `src/Japan.Core/GrammarJson.cs`**

```csharp
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Japan.Core;

public static class GrammarJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(GrammarBook book) => JsonSerializer.Serialize(book, Options);

    public static GrammarBook Deserialize(string json) =>
        JsonSerializer.Deserialize<GrammarBook>(json, Options)
        ?? throw new InvalidDataException("grammar.json rỗng.");
}
```

- [ ] **Step 9: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~GrammarJsonTests"`
Expected: PASS (2 tests). Build toàn solution không có warning.

- [ ] **Step 10: Commit**

```powershell
git add -A
git commit -m "feat: scaffold solution and core grammar models"
```

---

### Task 2: Kana, chuẩn hoá chuỗi, định dạng ruby

**Files:**
- Create: `src/Japan.Core/Kana.cs`, `src/Japan.Core/TextNormalizer.cs`, `src/Japan.Core/RubyText.cs`
- Test: `tests/Japan.Tests/Core/TextNormalizerTests.cs`, `tests/Japan.Tests/Core/RubyTextTests.cs`

**Interfaces:**
- Consumes: —
- Produces:
  - `static class Kana { char ToHiragana(char); string ToHiragana(string); bool IsKanji(char); bool ContainsKanji(string) }`
  - `static class TextNormalizer { string Normalize(string?) }`
  - `readonly record struct RubySegment(string Text, string? Reading)`
  - `static class RubyText { IReadOnlyList<RubySegment> Parse(string?); string Format(IEnumerable<RubySegment>); string PlainText(string?) }`

- [ ] **Step 1: Viết test thất bại `tests/Japan.Tests/Core/TextNormalizerTests.cs`**

```csharp
using System.Text;
using Japan.Core;

namespace Japan.Tests.Core;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("～うちに", "うちに")]
    [InlineData("〜ウチニ", "うちに")]
    [InlineData("~うちに", "うちに")]
    [InlineData("Tranh Thủ", "tranh thu")]
    [InlineData("Đã định", "da dinh")]
    [InlineData("Ưu tiên", "uu tien")]
    [InlineData("がる", "がる")]
    [InlineData("ガル", "がる")]
    [InlineData("（１）", "(1)")]
    [InlineData("  nhiều   khoảng  trắng ", "nhieu khoang trang")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("～", "")]
    public void Normalize_FoldsWidthKanaDiacriticsAndWaveDash(string input, string expected) =>
        Assert.Equal(expected, TextNormalizer.Normalize(input));

    [Fact]
    public void Normalize_TreatsDecomposedVietnameseLikeComposed() =>
        Assert.Equal(TextNormalizer.Normalize("nghĩa"), TextNormalizer.Normalize("nghĩa".Normalize(NormalizationForm.FormD)));

    [Fact]
    public void Normalize_Null_ReturnsEmpty() => Assert.Equal("", TextNormalizer.Normalize(null));
}
```

- [ ] **Step 2: Viết test thất bại `tests/Japan.Tests/Core/RubyTextTests.cs`**

```csharp
using Japan.Core;

namespace Japan.Tests.Core;

public class RubyTextTests
{
    [Fact]
    public void Parse_SplitsPlainAndRubySegments()
    {
        var segments = RubyText.Parse("[会社|かいしゃ]へ[行|い]かずに");

        Assert.Equal(
            [new RubySegment("会社", "かいしゃ"), new RubySegment("へ", null), new RubySegment("行", "い"), new RubySegment("かずに", null)],
            segments);
    }

    [Fact]
    public void Parse_PlainText_ReturnsSingleSegment() =>
        Assert.Equal([new RubySegment("いいえ、そのつもりはありません。", null)], RubyText.Parse("いいえ、そのつもりはありません。"));

    [Fact]
    public void Parse_NullOrEmpty_ReturnsNoSegments()
    {
        Assert.Empty(RubyText.Parse(null));
        Assert.Empty(RubyText.Parse(""));
    }

    [Fact]
    public void Format_IsInverseOfParse()
    {
        const string ruby = "[熱|あつ]いうちに[食|た]べてください。";
        Assert.Equal(ruby, RubyText.Format(RubyText.Parse(ruby)));
    }

    [Fact]
    public void PlainText_StripsReadings() =>
        Assert.Equal("会社へ行かずに", RubyText.PlainText("[会社|かいしゃ]へ[行|い]かずに"));
}
```

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~TextNormalizerTests|FullyQualifiedName~RubyTextTests"`
Expected: build FAIL — `TextNormalizer`, `RubyText`, `RubySegment` chưa tồn tại.

- [ ] **Step 4: Viết `src/Japan.Core/Kana.cs`**

```csharp
namespace Japan.Core;

public static class Kana
{
    public static char ToHiragana(char ch) => ch is >= 'ァ' and <= 'ヶ' ? (char)(ch - 0x60) : ch;

    public static string ToHiragana(string text) => string.Create(text.Length, text, static (span, source) =>
    {
        for (var i = 0; i < source.Length; i++) span[i] = ToHiragana(source[i]);
    });

    public static bool IsKanji(char ch) =>
        ch is >= '\u4E00' and <= '\u9FFF' or >= '\u3400' and <= '\u4DBF' or '々' or '〆' or 'ヶ';

    public static bool ContainsKanji(string text) => text.Any(IsKanji);
}
```

- [ ] **Step 5: Viết `src/Japan.Core/TextNormalizer.cs`**

```csharp
using System.Globalization;
using System.Text;

namespace Japan.Core;

public static class TextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var previousBase = '\0';
        var lastWasSpace = true;

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                // Chỉ bỏ dấu đứng sau chữ Latin; dấu (゛゜) của kana phải giữ.
                if (previousBase is >= 'a' and <= 'z') continue;
                sb.Append(ch);
                continue;
            }

            previousBase = ch;
            if (ch is '~' or '〜') continue;

            var c = ch == 'đ' ? 'd' : Kana.ToHiragana(ch);
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace) sb.Append(' ');
                lastWasSpace = true;
                continue;
            }

            sb.Append(c);
            lastWasSpace = false;
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
    }
}
```

- [ ] **Step 6: Viết `src/Japan.Core/RubyText.cs`**

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace Japan.Core;

public readonly record struct RubySegment(string Text, string? Reading);

public static partial class RubyText
{
    [GeneratedRegex(@"\[([^\[\]|]+)\|([^\[\]|]+)\]")]
    private static partial Regex TokenRegex();

    public static IReadOnlyList<RubySegment> Parse(string? ruby)
    {
        var result = new List<RubySegment>();
        if (string.IsNullOrEmpty(ruby)) return result;

        var pos = 0;
        foreach (Match m in TokenRegex().Matches(ruby))
        {
            if (m.Index > pos) result.Add(new RubySegment(ruby[pos..m.Index], null));
            result.Add(new RubySegment(m.Groups[1].Value, m.Groups[2].Value));
            pos = m.Index + m.Length;
        }

        if (pos < ruby.Length) result.Add(new RubySegment(ruby[pos..], null));
        return result;
    }

    public static string Format(IEnumerable<RubySegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var s in segments)
        {
            if (s.Reading is null) sb.Append(s.Text);
            else sb.Append('[').Append(s.Text).Append('|').Append(s.Reading).Append(']');
        }

        return sb.ToString();
    }

    public static string PlainText(string? ruby) => string.Concat(Parse(ruby).Select(s => s.Text));
}
```

- [ ] **Step 7: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~TextNormalizerTests|FullyQualifiedName~RubyTextTests"`
Expected: PASS (tất cả).

- [ ] **Step 8: Commit**

```powershell
git add -A
git commit -m "feat: add kana helpers, search normalizer and ruby text format"
```

---

### Task 3: DocumentBuilder — máy trạng thái tách bài/điểm

**Files:**
- Create: `src/Japan.Importer/Parsing/SourceDocument.cs`, `src/Japan.Importer/Parsing/DocumentBuilder.cs`
- Test: `tests/Japan.Tests/Importer/DocumentBuilderTests.cs`

**Interfaces:**
- Consumes: `Section`, `Table`, `Example` (Task 1)
- Produces:
  - `record SourceDocument(string FileName, PointRange? Range, IReadOnlyList<RawLesson> Lessons, IReadOnlyList<string> Warnings)`
  - `readonly record struct PointRange(int First, int Last) { int Count }`
  - `record RawLesson(int Number, string Title, IReadOnlyList<RawPoint> Points, IReadOnlyList<Table> QuickComparisons)`
  - `record RawPoint(int Index, string Pattern, Section Meaning, Section Structure, Section? Note, IReadOnlyList<Example> Examples)`
  - `sealed class DocumentBuilder(string fileName) { void LessonHeading(string); void PointHeading(string); void Paragraph(string); void Table(Table); SourceDocument Build() }`

Quy tắc (theo spec mục 2): `LessonHeading` khớp `BÀI n: Tên (phần k/2)`; `PointHeading` bắt đầu bằng số → điểm, không bắt đầu bằng số → chế độ "bảng so sánh nhanh" của bài; `Paragraph` xử lý dòng `điểm X–Y/106` (lấy lần đầu), chân file `File n/12`, nhãn `Ý nghĩa:`/`Cấu trúc:`/`Lưu ý:`/`Ví dụ:`, ví dụ `n. câu` + `→ dịch` (hoặc cả hai trên một dòng).

- [ ] **Step 1: Viết test thất bại `tests/Japan.Tests/Importer/DocumentBuilderTests.cs`**

```csharp
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
```

- [ ] **Step 2: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~DocumentBuilderTests"`
Expected: build FAIL — `DocumentBuilder`, `SourceDocument` chưa tồn tại.

- [ ] **Step 3: Viết `src/Japan.Importer/Parsing/SourceDocument.cs`**

```csharp
using Japan.Core;

namespace Japan.Importer.Parsing;

public sealed record SourceDocument(
    string FileName,
    PointRange? Range,
    IReadOnlyList<RawLesson> Lessons,
    IReadOnlyList<string> Warnings);

public readonly record struct PointRange(int First, int Last)
{
    public int Count => Last - First + 1;
}

public sealed record RawLesson(int Number, string Title, IReadOnlyList<RawPoint> Points, IReadOnlyList<Table> QuickComparisons);

public sealed record RawPoint(
    int Index,
    string Pattern,
    Section Meaning,
    Section Structure,
    Section? Note,
    IReadOnlyList<Example> Examples);
```

- [ ] **Step 4: Viết `src/Japan.Importer/Parsing/DocumentBuilder.cs`**

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Japan.Core;

namespace Japan.Importer.Parsing;

public sealed partial class DocumentBuilder(string fileName)
{
    private enum Part { None, Meaning, Structure, Note, Examples, QuickComparison }

    private sealed class SectionBuilder
    {
        public List<string> Paragraphs { get; } = [];
        public List<Table> Tables { get; } = [];
        public bool IsEmpty => Paragraphs.Count == 0 && Tables.Count == 0;
        public Section Build() => new([.. Paragraphs], [.. Tables]);
    }

    private sealed class PointBuilder(int index, string pattern)
    {
        public string Pattern { get; } = pattern;
        public SectionBuilder Meaning { get; } = new();
        public SectionBuilder Structure { get; } = new();
        public SectionBuilder Note { get; } = new();
        public List<Example> Examples { get; } = [];
        public string? PendingJapanese { get; set; }

        public RawPoint Build() =>
            new(index, Pattern, Meaning.Build(), Structure.Build(), Note.IsEmpty ? null : Note.Build(), [.. Examples]);
    }

    private sealed class LessonBuilder(int number, string title)
    {
        public int Number { get; } = number;
        public List<RawPoint> Points { get; } = [];
        public List<Table> QuickComparisons { get; } = [];
        public RawLesson Build() => new(Number, title, [.. Points], [.. QuickComparisons]);
    }

    [GeneratedRegex(@"^B[ÀA]I\s*([0-9０-９]+)\s*[:：]\s*(.+?)\s*(?:[(（]\s*phần[^)）]*[)）])?$", RegexOptions.IgnoreCase)]
    private static partial Regex LessonRegex();

    [GeneratedRegex(@"^([0-9０-９]+)\s*[.．]\s*(.+)$")]
    private static partial Regex NumberedRegex();

    [GeneratedRegex(@"^(Ý nghĩa|Cấu trúc|Lưu ý|Ví dụ)\s*[:：]\s*(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"điểm\s*([0-9]+)\s*[–—-]\s*([0-9]+)\s*/\s*106", RegexOptions.IgnoreCase)]
    private static partial Regex RangeRegex();

    [GeneratedRegex(@"^File\s*\d+\s*/\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex FooterRegex();

    private readonly List<LessonBuilder> _lessons = [];
    private readonly List<string> _warnings = [];
    private LessonBuilder? _lesson;
    private PointBuilder? _point;
    private Part _part;
    private PointRange? _range;

    public void LessonHeading(string text)
    {
        text = Clean(text);
        var m = LessonRegex().Match(text);
        if (!m.Success)
        {
            Paragraph(text);
            return;
        }

        ClosePoint();
        var number = ParseNumber(m.Groups[1].Value);
        _lesson = _lessons.Find(l => l.Number == number);
        if (_lesson is null)
        {
            _lesson = new LessonBuilder(number, m.Groups[2].Value.Trim());
            _lessons.Add(_lesson);
        }

        _part = Part.None;
    }

    public void PointHeading(string text)
    {
        text = Clean(text);
        ClosePoint();
        _part = Part.None;

        if (_lesson is null)
        {
            _warnings.Add($"{fileName}: tiêu đề nằm ngoài bài: \"{text}\"");
            return;
        }

        var m = NumberedRegex().Match(text);
        if (!m.Success)
        {
            _part = Part.QuickComparison;
            return;
        }

        _point = new PointBuilder(ParseNumber(m.Groups[1].Value), m.Groups[2].Value.Trim());
    }

    public void Paragraph(string text)
    {
        text = Clean(text);
        if (text.Length == 0 || FooterRegex().IsMatch(text)) return;

        if (_range is null && RangeRegex().Match(text) is { Success: true } r)
        {
            _range = new PointRange(ParseNumber(r.Groups[1].Value), ParseNumber(r.Groups[2].Value));
            return;
        }

        if (_lesson is null) return;

        if (_point is null)
        {
            _warnings.Add($"{fileName}: [Bài {_lesson.Number}] đoạn nằm ngoài điểm ngữ pháp: \"{text}\"");
            return;
        }

        var label = LabelRegex().Match(text);
        if (label.Success)
        {
            _part = label.Groups[1].Value.ToLowerInvariant() switch
            {
                "ý nghĩa" => Part.Meaning,
                "cấu trúc" => Part.Structure,
                "lưu ý" => Part.Note,
                _ => Part.Examples,
            };
            var rest = label.Groups[2].Value.Trim();
            if (rest.Length > 0) AddToPart(rest);
            return;
        }

        AddToPart(text);
    }

    public void Table(Table table)
    {
        if (_part == Part.QuickComparison && _lesson is not null)
        {
            _lesson.QuickComparisons.Add(table);
            return;
        }

        var target = _part switch
        {
            Part.Meaning => _point?.Meaning,
            Part.Structure => _point?.Structure,
            Part.Note => _point?.Note,
            _ => null,
        };

        if (target is null)
        {
            _warnings.Add($"{fileName}: bảng không thuộc mục nào (tiêu đề: {string.Join(" | ", table.Header)})");
            return;
        }

        target.Tables.Add(table);
    }

    public SourceDocument Build()
    {
        ClosePoint();
        return new SourceDocument(fileName, _range, [.. _lessons.Select(l => l.Build())], [.. _warnings]);
    }

    private void AddToPart(string text)
    {
        var point = _point!;
        switch (_part)
        {
            case Part.Meaning: point.Meaning.Paragraphs.Add(text); break;
            case Part.Structure: point.Structure.Paragraphs.Add(text); break;
            case Part.Note: point.Note.Paragraphs.Add(text); break;
            case Part.Examples: AddExampleLine(point, text); break;
            default: _warnings.Add($"{fileName}: [{point.Pattern}] đoạn không thuộc mục nào: \"{text}\""); break;
        }
    }

    private void AddExampleLine(PointBuilder point, string text)
    {
        if (text.StartsWith('→'))
        {
            var vietnamese = text[1..].Trim();
            if (point.PendingJapanese is null)
            {
                _warnings.Add($"{fileName}: [{point.Pattern}] câu dịch không có câu tiếng Nhật: \"{text}\"");
                return;
            }

            point.Examples.Add(new Example(point.PendingJapanese, vietnamese));
            point.PendingJapanese = null;
            return;
        }

        FlushPendingExample(point);
        var m = NumberedRegex().Match(text);
        var body = m.Success ? m.Groups[2].Value.Trim() : text;
        var arrow = body.IndexOf('→');
        if (arrow >= 0)
        {
            point.Examples.Add(new Example(body[..arrow].Trim(), body[(arrow + 1)..].Trim()));
            return;
        }

        point.PendingJapanese = body;
    }

    private static void FlushPendingExample(PointBuilder point)
    {
        if (point.PendingJapanese is null) return;
        point.Examples.Add(new Example(point.PendingJapanese, ""));
        point.PendingJapanese = null;
    }

    private void ClosePoint()
    {
        if (_point is null) return;
        FlushPendingExample(_point);
        _lesson!.Points.Add(_point.Build());
        _point = null;
    }

    private static string Clean(string text) => text.Normalize(NormalizationForm.FormC).Trim();

    private static int ParseNumber(string digits) =>
        int.Parse(digits.Normalize(NormalizationForm.FormKC), CultureInfo.InvariantCulture);
}
```

- [ ] **Step 5: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~DocumentBuilderTests"`
Expected: PASS (tất cả).

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat(importer): add document builder state machine for grammar sources"
```

---

### Task 4: MarkdownGrammarReader

**Files:**
- Create: `src/Japan.Importer/Parsing/MarkdownGrammarReader.cs`
- Test: `tests/Japan.Tests/Importer/MarkdownGrammarReaderTests.cs`

**Interfaces:**
- Consumes: `DocumentBuilder`, `SourceDocument` (Task 3)
- Produces: `static class MarkdownGrammarReader { SourceDocument Read(string fileName, string markdown); SourceDocument ReadFile(string path) }`

Ánh xạ: `#` → `Paragraph` (để bắt dòng `điểm X–Y/106`), `##` → `LessonHeading`, `###` → `PointHeading`, đoạn văn → mỗi dòng một `Paragraph`, list có thứ tự → `"{n}. {dòng đầu}"` rồi các dòng sau, list không thứ tự → từng dòng, bảng pipe → `Table`, `---` bỏ qua. Trước khi parse, chèn dòng trống trước bảng nếu bảng dính ngay sau một dòng văn bản (`**Ý nghĩa:**` rồi xuống dòng `| … |`).

- [ ] **Step 1: Viết test thất bại `tests/Japan.Tests/Importer/MarkdownGrammarReaderTests.cs`**

```csharp
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
```

- [ ] **Step 2: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~MarkdownGrammarReaderTests"`
Expected: build FAIL — `MarkdownGrammarReader` chưa tồn tại.

- [ ] **Step 3: Viết `src/Japan.Importer/Parsing/MarkdownGrammarReader.cs`**

```csharp
using System.Text;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using CoreTable = Japan.Core.Table;
using MdTable = Markdig.Extensions.Tables.Table;

namespace Japan.Importer.Parsing;

public static class MarkdownGrammarReader
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();

    public static SourceDocument ReadFile(string path) => Read(Path.GetFileName(path), File.ReadAllText(path));

    public static SourceDocument Read(string fileName, string markdown)
    {
        var builder = new DocumentBuilder(fileName);
        foreach (var block in Markdown.Parse(EnsureBlankLineBeforeTables(markdown), Pipeline))
        {
            Visit(block, builder);
        }

        return builder.Build();
    }

    private static void Visit(Block block, DocumentBuilder builder)
    {
        switch (block)
        {
            case HeadingBlock { Level: 2 } h:
                builder.LessonHeading(Text(h.Inline));
                break;
            case HeadingBlock { Level: 3 } h:
                builder.PointHeading(Text(h.Inline));
                break;
            case HeadingBlock h:
                builder.Paragraph(Text(h.Inline));
                break;
            case ParagraphBlock p:
                foreach (var line in Lines(p.Inline)) builder.Paragraph(line);
                break;
            case ListBlock list:
                var number = 0;
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    number++;
                    var lines = item.OfType<ParagraphBlock>().SelectMany(p => Lines(p.Inline)).ToList();
                    if (lines.Count == 0) continue;
                    builder.Paragraph(list.IsOrdered ? $"{number}. {lines[0]}" : lines[0]);
                    foreach (var line in lines.Skip(1)) builder.Paragraph(line);
                }

                break;
            case MdTable table:
                builder.Table(ToTable(table));
                break;
        }
    }

    private static CoreTable ToTable(MdTable table)
    {
        var rows = table.OfType<TableRow>()
            .Select(r => (r.IsHeader, Cells: (IReadOnlyList<string>)r.OfType<TableCell>()
                .Select(c => string.Join(" ", c.OfType<ParagraphBlock>().Select(p => Text(p.Inline).Trim())))
                .ToList()))
            .ToList();
        var header = rows.Where(r => r.IsHeader).Select(r => r.Cells).FirstOrDefault() ?? [];
        return new CoreTable(header, rows.Where(r => !r.IsHeader).Select(r => r.Cells).ToList());
    }

    private static IEnumerable<string> Lines(ContainerInline? inline) =>
        Text(inline).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string Text(ContainerInline? inline)
    {
        var sb = new StringBuilder();
        if (inline is not null) Append(inline, sb);
        return sb.ToString();
    }

    private static void Append(Inline inline, StringBuilder sb)
    {
        switch (inline)
        {
            case LiteralInline l: sb.Append(l.Content.ToString()); break;
            case CodeInline c: sb.Append(c.Content); break;
            case LineBreakInline: sb.Append('\n'); break;
            case HtmlEntityInline e: sb.Append(e.Transcoded.ToString()); break;
            case ContainerInline c:
                foreach (var child in c) Append(child, sb);
                break;
        }
    }

    private static string EnsureBlankLineBeforeTables(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(markdown.Length + 16);
        for (var i = 0; i < lines.Length; i++)
        {
            var isTableLine = lines[i].TrimStart().StartsWith('|');
            if (isTableLine && i > 0 && lines[i - 1].Trim().Length > 0 && !lines[i - 1].TrimStart().StartsWith('|'))
                sb.Append('\n');
            sb.Append(lines[i]).Append('\n');
        }

        return sb.ToString();
    }
}
```

- [ ] **Step 4: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~MarkdownGrammarReaderTests"`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(importer): read grammar from markdown sources"
```

---

### Task 5: DocxGrammarReader

**Files:**
- Create: `src/Japan.Importer/Parsing/DocxGrammarReader.cs`, `src/Japan.Importer/Parsing/SourceFileLockedException.cs`
- Test: `tests/Japan.Tests/DocxFixture.cs`, `tests/Japan.Tests/Importer/DocxGrammarReaderTests.cs`

**Interfaces:**
- Consumes: `DocumentBuilder`, `SourceDocument` (Task 3)
- Produces:
  - `static class DocxGrammarReader { SourceDocument Read(string fileName, Stream stream); SourceDocument ReadFile(string path) }`
  - `sealed class SourceFileLockedException : IOException { string FilePath }`
  - Test helper `static class DocxFixture { MemoryStream Create(params OpenXmlElement[] body); void Save(string path, params OpenXmlElement[] body); Paragraph P(string text, string? style = null); Table T(params string[][] rows) }`

Cấp heading lấy từ tên style (`heading 1`, `heading 2`, tra trong `StyleDefinitionsPart`), nếu không có thì từ style id (`Heading1`). Mở file với `FileShare.ReadWrite | FileShare.Delete` để đọc được cả khi Word đang mở (đọc bản đã lưu gần nhất).

- [ ] **Step 1: Viết `tests/Japan.Tests/DocxFixture.cs`**

```csharp
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Japan.Tests;

internal static class DocxFixture
{
    public static MemoryStream Create(params OpenXmlElement[] body)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            var styles = main.AddNewPart<StyleDefinitionsPart>();
            styles.Styles = new Styles(
                new Style(new StyleName { Val = "heading 1" }) { Type = StyleValues.Paragraph, StyleId = "Heading1" },
                new Style(new StyleName { Val = "heading 2" }) { Type = StyleValues.Paragraph, StyleId = "Heading2" },
                new Style(new StyleName { Val = "heading 2" }) { Type = StyleValues.Paragraph, StyleId = "a2" });
            main.Document = new Document(new Body(body));
        }

        ms.Position = 0;
        return ms;
    }

    public static void Save(string path, params OpenXmlElement[] body)
    {
        using var ms = Create(body);
        File.WriteAllBytes(path, ms.ToArray());
    }

    public static Paragraph P(string text, string? style = null)
    {
        var p = new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        if (style is not null) p.PrependChild(new ParagraphProperties(new ParagraphStyleId { Val = style }));
        return p;
    }

    public static Table T(params string[][] rows) =>
        new(rows.Select(r => new TableRow(r.Select(c => new TableCell(P(c))))));
}
```

- [ ] **Step 2: Viết test thất bại `tests/Japan.Tests/Importer/DocxGrammarReaderTests.cs`**

```csharp
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
```

`ReadFile_FileOpenWithReadSharing_IsReadable`: nếu test này fail vì `FileShare.ReadWrite` của reader xung đột với quyền ghi của tiến trình kia, đổi cách đọc trong `ReadFile` thành copy file sang `Path.GetTempFileName()` bằng `File.Copy` rồi đọc bản copy; giữ nguyên cả hai test.

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~DocxGrammarReaderTests"`
Expected: build FAIL — `DocxGrammarReader`, `SourceFileLockedException` chưa tồn tại.

- [ ] **Step 4: Viết `src/Japan.Importer/Parsing/SourceFileLockedException.cs`**

```csharp
namespace Japan.Importer.Parsing;

public sealed class SourceFileLockedException(string path, Exception inner)
    : IOException($"Không mở được \"{Path.GetFileName(path)}\" vì file đang bị chương trình khác khoá. Hãy đóng Word rồi chạy lại.", inner)
{
    public string FilePath { get; } = path;
}
```

- [ ] **Step 5: Viết `src/Japan.Importer/Parsing/DocxGrammarReader.cs`**

```csharp
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using CoreTable = Japan.Core.Table;
using WordTable = DocumentFormat.OpenXml.Wordprocessing.Table;

namespace Japan.Importer.Parsing;

public static partial class DocxGrammarReader
{
    [GeneratedRegex(@"heading\s*([1-9])", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingRegex();

    public static SourceDocument ReadFile(string path)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch (IOException ex)
        {
            throw new SourceFileLockedException(path, ex);
        }

        using (stream)
        {
            return Read(Path.GetFileName(path), stream);
        }
    }

    public static SourceDocument Read(string fileName, Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var main = doc.MainDocumentPart ?? throw new InvalidDataException($"{fileName}: không có nội dung.");
        var body = main.Document?.Body ?? throw new InvalidDataException($"{fileName}: không có nội dung.");
        var styleNames = StyleNames(main);
        var builder = new DocumentBuilder(fileName);

        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case Paragraph p:
                    var text = p.InnerText;
                    switch (HeadingLevel(p, styleNames))
                    {
                        case 1: builder.LessonHeading(text); break;
                        case 2: builder.PointHeading(text); break;
                        default: builder.Paragraph(text); break;
                    }

                    break;
                case WordTable t:
                    builder.Table(ToTable(t));
                    break;
            }
        }

        return builder.Build();
    }

    private static Dictionary<string, string> StyleNames(MainDocumentPart main)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var style in main.StyleDefinitionsPart?.Styles?.Elements<Style>() ?? [])
        {
            if (style.StyleId?.Value is { } id) names.TryAdd(id, style.StyleName?.Val?.Value ?? id);
        }

        return names;
    }

    private static int HeadingLevel(Paragraph p, Dictionary<string, string> styleNames)
    {
        var id = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (id is null) return 0;

        var m = HeadingRegex().Match(styleNames.GetValueOrDefault(id, id));
        if (!m.Success) m = HeadingRegex().Match(id);
        return m.Success ? m.Groups[1].Value[0] - '0' : 0;
    }

    private static CoreTable ToTable(WordTable table)
    {
        var rows = table.Elements<TableRow>()
            .Select(r => (IReadOnlyList<string>)r.Elements<TableCell>()
                .Select(c => string.Join("\n", c.Elements<Paragraph>().Select(p => p.InnerText.Trim()).Where(s => s.Length > 0)))
                .ToList())
            .ToList();
        return rows.Count == 0 ? new CoreTable([], []) : new CoreTable(rows[0], rows.Skip(1).ToList());
    }
}
```

- [ ] **Step 6: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~DocxGrammarReaderTests"`
Expected: PASS (5 tests).

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(importer): read grammar from docx sources"
```

---

### Task 6: EnrichmentLoader và BookAssembler

**Files:**
- Create: `src/Japan.Importer/Assembly/EnrichmentLoader.cs`, `src/Japan.Importer/Assembly/BookAssembler.cs`
- Test: `tests/Japan.Tests/Importer/EnrichmentLoaderTests.cs`, `tests/Japan.Tests/Importer/BookAssemblerTests.cs`

**Interfaces:**
- Consumes: `SourceDocument`, `RawLesson`, `RawPoint`, `PointRange` (Task 3); `GrammarEnrichment`, `GrammarJson.Options` (Task 1)
- Produces:
  - `record EnrichmentLoadResult(IReadOnlyDictionary<string, GrammarEnrichment> Items, IReadOnlyList<string> Errors)`
  - `static class EnrichmentLoader { EnrichmentLoadResult Load(string directory) }`
  - `record AssemblyResult(GrammarBook? Book, IReadOnlyList<string> Errors)`
  - `static class BookAssembler { const int ExpectedPointCount = 106; AssemblyResult Assemble(IReadOnlyList<SourceDocument> documents, IReadOnlyDictionary<string, GrammarEnrichment> enrichment, DateTimeOffset generatedAt, int expectedPointCount = 106) }`

- [ ] **Step 1: Viết test thất bại `tests/Japan.Tests/Importer/EnrichmentLoaderTests.cs`**

```csharp
using Japan.Importer.Assembly;

namespace Japan.Tests.Importer;

public sealed class EnrichmentLoaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("enrich-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Load_ReadsValidFile_AndDefaultsMissingLists()
    {
        File.WriteAllText(Path.Combine(_dir, "b6-3.json"), """
            {
              "explanation": "Dạng văn viết của ～ないで.",
              "formation": [ { "wordType": "Động từ nhóm 3", "rule": "する → せずに", "samples": ["勉強する → 勉強せずに"] } ],
              "examples": [ { "japanese": "朝ご飯を食べずに学校へ行った。", "vietnamese": "Tôi đến trường mà không ăn sáng." } ]
            }
            """);

        var result = EnrichmentLoader.Load(_dir);

        Assert.Empty(result.Errors);
        var e = result.Items["b6-3"];
        Assert.Equal("Dạng văn viết của ～ないで.", e.Explanation);
        Assert.Equal("する → せずに", e.Formation[0].Rule);
        Assert.Empty(e.Usage);
        Assert.Empty(e.Comparisons);
        Assert.Null(e.Examples[0].Ruby);
    }

    [Fact]
    public void Load_ReportsBadJsonBadNameAndMissingExplanation_WithFileNames()
    {
        File.WriteAllText(Path.Combine(_dir, "b1-1.json"), "{ \"explanation\": ");
        File.WriteAllText(Path.Combine(_dir, "zuni.json"), "{ \"explanation\": \"x\" }");
        File.WriteAllText(Path.Combine(_dir, "b1-2.json"), "{ \"usage\": [] }");

        var result = EnrichmentLoader.Load(_dir);

        Assert.Empty(result.Items);
        Assert.Contains(result.Errors, e => e.StartsWith("b1-1.json") && e.Contains("JSON"));
        Assert.Contains(result.Errors, e => e.StartsWith("zuni.json"));
        Assert.Contains(result.Errors, e => e.StartsWith("b1-2.json") && e.Contains("explanation"));
    }

    [Fact]
    public void Load_MissingDirectory_ReturnsEmpty()
    {
        var result = EnrichmentLoader.Load(Path.Combine(_dir, "khong-co"));

        Assert.Empty(result.Items);
        Assert.Empty(result.Errors);
    }
}
```

- [ ] **Step 2: Viết test thất bại `tests/Japan.Tests/Importer/BookAssemblerTests.cs`**

```csharp
using Japan.Core;
using Japan.Importer.Assembly;
using Japan.Importer.Parsing;

namespace Japan.Tests.Importer;

public class BookAssemblerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Dictionary<string, GrammarEnrichment> NoEnrichment = new();

    private static RawPoint Raw(int index, string pattern = "～x", string vietnamese = "Dịch.") =>
        new(index, pattern, new Section(["Nghĩa."], []), new Section(["Vる + x"], []), null, [new Example("例文。", vietnamese)]);

    private static SourceDocument Doc(string file, PointRange? range, params RawLesson[] lessons) => new(file, range, lessons, []);

    private static RawLesson L(int number, params RawPoint[] points) => new(number, $"Bài {number}", points, []);

    [Fact]
    public void Assemble_MergesLessonSplitAcrossFiles_InIndexOrder()
    {
        var result = BookAssembler.Assemble(
            [Doc("File01.docx", new PointRange(1, 2), L(1, Raw(1), Raw(2))),
             Doc("File02.md", new PointRange(3, 4), L(1, Raw(3)), L(2, Raw(1)))],
            NoEnrichment, Now, expectedPointCount: 4);

        Assert.Empty(result.Errors);
        var book = result.Book!;
        Assert.Equal(["b1-1", "b1-2", "b1-3"], book.Lessons[0].Points.Select(p => p.Id));
        Assert.Equal("File02.md", book.Lessons[0].Points[2].SourceFile);
        Assert.Equal("b2-1", book.Lessons[1].Points.Single().Id);
        Assert.Equal(Now, book.GeneratedAt);
    }

    [Fact]
    public void Assemble_RangeMismatch_IsError()
    {
        var result = BookAssembler.Assemble([Doc("File03.docx", new PointRange(19, 27), L(1, Raw(1)))], NoEnrichment, Now, 1);

        Assert.Null(result.Book);
        Assert.Contains(result.Errors, e => e.Contains("File03.docx") && e.Contains("19–27") && e.Contains("đọc được 1"));
    }

    [Fact]
    public void Assemble_TotalMissingLessonDuplicateAndIncompletePoint_AreErrors()
    {
        var result = BookAssembler.Assemble(
            [Doc("a.docx", null, L(1, Raw(1), Raw(1)), L(3, Raw(1, vietnamese: "")))],
            NoEnrichment, Now, expectedPointCount: 106);

        Assert.Null(result.Book);
        Assert.Contains(result.Errors, e => e.Contains("trùng điểm b1-1"));
        Assert.Contains(result.Errors, e => e.Contains("Thiếu bài: 2"));
        Assert.Contains(result.Errors, e => e.Contains("mong đợi 106"));
        Assert.Contains(result.Errors, e => e.Contains("b3-1") && e.Contains("ví dụ 1"));
    }

    [Fact]
    public void Assemble_AttachesEnrichment_AndRejectsUnknownIds()
    {
        var enrichment = new Dictionary<string, GrammarEnrichment>
        {
            ["b1-1"] = new("Giải thích.", [], [], [], [new Comparison("～y", "b9-9", "Khác.")], []),
            ["b5-5"] = new("Không có điểm này.", [], [], [], [], []),
        };

        var result = BookAssembler.Assemble([Doc("a.docx", null, L(1, Raw(1)))], enrichment, Now, 1);

        Assert.Contains(result.Errors, e => e.Contains("b5-5.json") && e.Contains("không có điểm"));
        Assert.Contains(result.Errors, e => e.Contains("b1-1.json") && e.Contains("b9-9"));
    }

    [Fact]
    public void Assemble_ValidEnrichment_IsAttached()
    {
        var enrichment = new Dictionary<string, GrammarEnrichment> { ["b1-1"] = new("Giải thích.", [], [], [], [], []) };

        var result = BookAssembler.Assemble([Doc("a.docx", null, L(1, Raw(1)))], enrichment, Now, 1);

        Assert.Equal("Giải thích.", result.Book!.Lessons[0].Points[0].Enrichment!.Explanation);
    }
}
```

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~EnrichmentLoaderTests|FullyQualifiedName~BookAssemblerTests"`
Expected: build FAIL — `EnrichmentLoader`, `BookAssembler` chưa tồn tại.

- [ ] **Step 4: Viết `src/Japan.Importer/Assembly/EnrichmentLoader.cs`**

```csharp
using System.Text.Json;
using System.Text.RegularExpressions;
using Japan.Core;

namespace Japan.Importer.Assembly;

public sealed record EnrichmentLoadResult(IReadOnlyDictionary<string, GrammarEnrichment> Items, IReadOnlyList<string> Errors);

public static partial class EnrichmentLoader
{
    [GeneratedRegex(@"^b\d+-\d+$")]
    private static partial Regex IdRegex();

    public static EnrichmentLoadResult Load(string directory)
    {
        var items = new Dictionary<string, GrammarEnrichment>(StringComparer.Ordinal);
        var errors = new List<string>();
        if (!Directory.Exists(directory)) return new EnrichmentLoadResult(items, errors);

        foreach (var path in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(path);
            var id = Path.GetFileNameWithoutExtension(path);
            if (!IdRegex().IsMatch(id))
            {
                errors.Add($"{name}: tên file phải có dạng b<bài>-<điểm>.json");
                continue;
            }

            GrammarEnrichment? enrichment;
            try
            {
                enrichment = JsonSerializer.Deserialize<GrammarEnrichment>(File.ReadAllText(path), GrammarJson.Options);
            }
            catch (JsonException ex)
            {
                errors.Add($"{name}: JSON không hợp lệ — {ex.Message}");
                continue;
            }

            if (enrichment is null || string.IsNullOrWhiteSpace(enrichment.Explanation))
            {
                errors.Add($"{name}: thiếu \"explanation\"");
                continue;
            }

            items[id] = enrichment with
            {
                Formation = enrichment.Formation ?? [],
                Usage = enrichment.Usage ?? [],
                Pitfalls = enrichment.Pitfalls ?? [],
                Comparisons = enrichment.Comparisons ?? [],
                Examples = enrichment.Examples ?? [],
            };
        }

        return new EnrichmentLoadResult(items, errors);
    }
}
```

- [ ] **Step 5: Viết `src/Japan.Importer/Assembly/BookAssembler.cs`**

```csharp
using Japan.Core;
using Japan.Importer.Parsing;

namespace Japan.Importer.Assembly;

public sealed record AssemblyResult(GrammarBook? Book, IReadOnlyList<string> Errors);

public static class BookAssembler
{
    public const int ExpectedPointCount = 106;

    private sealed record LessonParts(string Title, List<GrammarPoint> Points, List<Table> QuickComparisons);

    public static AssemblyResult Assemble(
        IReadOnlyList<SourceDocument> documents,
        IReadOnlyDictionary<string, GrammarEnrichment> enrichment,
        DateTimeOffset generatedAt,
        int expectedPointCount = ExpectedPointCount)
    {
        var errors = new List<string>();
        var lessons = new SortedDictionary<int, LessonParts>();

        foreach (var doc in documents)
        {
            var count = doc.Lessons.Sum(l => l.Points.Count);
            if (doc.Range is { } range && range.Count != count)
                errors.Add($"{doc.FileName}: đầu file ghi điểm {range.First}–{range.Last} ({range.Count} điểm) nhưng đọc được {count} điểm");

            foreach (var raw in doc.Lessons)
            {
                if (!lessons.TryGetValue(raw.Number, out var lesson))
                {
                    lesson = new LessonParts(raw.Title, [], []);
                    lessons[raw.Number] = lesson;
                }

                lesson.QuickComparisons.AddRange(raw.QuickComparisons);
                foreach (var p in raw.Points)
                {
                    var id = $"b{raw.Number}-{p.Index}";
                    if (lesson.Points.Any(x => x.Index == p.Index))
                    {
                        errors.Add($"{doc.FileName}: trùng điểm {id} ({p.Pattern})");
                        continue;
                    }

                    errors.AddRange(Validate(doc.FileName, id, p));
                    lesson.Points.Add(new GrammarPoint(
                        id, raw.Number, p.Index, p.Pattern, p.Meaning, p.Structure, p.Note, p.Examples,
                        enrichment.GetValueOrDefault(id), doc.FileName));
                }
            }
        }

        var total = lessons.Values.Sum(l => l.Points.Count);
        if (total != expectedPointCount)
            errors.Add($"Tổng cộng đọc được {total} điểm, mong đợi {expectedPointCount}");

        if (lessons.Count > 0)
        {
            var missing = Enumerable.Range(1, lessons.Keys.Max()).Where(n => !lessons.ContainsKey(n)).ToList();
            if (missing.Count > 0) errors.Add($"Thiếu bài: {string.Join(", ", missing)}");
        }

        var ids = lessons.Values.SelectMany(l => l.Points).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var (id, e) in enrichment.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (!ids.Contains(id)) errors.Add($"content/enrichment/{id}.json: không có điểm ngữ pháp {id}");
            foreach (var c in e.Comparisons)
            {
                if (c.TargetId is { } target && !ids.Contains(target))
                    errors.Add($"content/enrichment/{id}.json: so sánh \"{c.Pattern}\" trỏ tới {target} không tồn tại");
            }
        }

        if (errors.Count > 0) return new AssemblyResult(null, errors);

        var book = new GrammarBook(
            lessons.Select(kv => new Lesson(kv.Key, kv.Value.Title, kv.Value.Points.OrderBy(p => p.Index).ToList(), kv.Value.QuickComparisons)).ToList(),
            generatedAt);
        return new AssemblyResult(book, errors);
    }

    private static IEnumerable<string> Validate(string file, string id, RawPoint p)
    {
        var where = $"{file}: {id} ({p.Pattern})";
        if (p.Meaning.IsEmpty) yield return $"{where}: thiếu \"Ý nghĩa\"";
        if (p.Structure.IsEmpty) yield return $"{where}: thiếu \"Cấu trúc\"";
        if (p.Examples.Count == 0) yield return $"{where}: không có ví dụ";
        for (var i = 0; i < p.Examples.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(p.Examples[i].Japanese) || string.IsNullOrWhiteSpace(p.Examples[i].Vietnamese))
                yield return $"{where}: ví dụ {i + 1} thiếu câu tiếng Nhật hoặc câu dịch";
        }
    }
}
```

- [ ] **Step 6: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~EnrichmentLoaderTests|FullyQualifiedName~BookAssemblerTests"`
Expected: PASS (tất cả).

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat(importer): assemble book from sources and load enrichment"
```

---

### Task 7: Furigana (NMeCab)

**Files:**
- Create: `src/Japan.Importer/Furigana/IFuriganaGenerator.cs`, `NMeCabFuriganaGenerator.cs`, `FuriganaOverrides.cs`, `FuriganaApplier.cs`
- Test: `tests/Japan.Tests/Importer/FuriganaTests.cs`

**Interfaces:**
- Consumes: `Kana`, `RubyText`, `RubySegment` (Task 2); `GrammarBook`, `Example` (Task 1)
- Produces:
  - `interface IFuriganaGenerator { string ToRuby(string japanese); }`
  - `sealed class NMeCabFuriganaGenerator(IReadOnlyDictionary<string, string>? overrides = null) : IFuriganaGenerator, IDisposable` với `static IReadOnlyList<RubySegment> Split(string surface, string? reading)`
  - `static class FuriganaOverrides { IReadOnlyDictionary<string, string> Load(string path) }` — ném `InvalidDataException` khi JSON hỏng
  - `static class FuriganaApplier { GrammarBook Apply(GrammarBook book, IFuriganaGenerator generator) }`

- [ ] **Step 1: Viết test thất bại `tests/Japan.Tests/Importer/FuriganaTests.cs`**

```csharp
using Japan.Core;
using Japan.Importer.Furigana;

namespace Japan.Tests.Importer;

public sealed class FuriganaTests : IDisposable
{
    private readonly NMeCabFuriganaGenerator _generator = new();

    public void Dispose() => _generator.Dispose();

    [Theory]
    [InlineData("行か", "イカ", "[行|い]か")]
    [InlineData("お茶", "オチャ", "お[茶|ちゃ]")]
    [InlineData("会社", "カイシャ", "[会社|かいしゃ]")]
    [InlineData("一ヶ月", "イッカゲツ", "[一ヶ月|いっかげつ]")]
    [InlineData("パソコン", "パソコン", "パソコン")]
    [InlineData("東京", null, "東京")]
    public void Split_PutsReadingOnKanjiOnly(string surface, string? reading, string expected) =>
        Assert.Equal(expected, RubyText.Format(NMeCabFuriganaGenerator.Split(surface, reading)));

    [Fact]
    public void ToRuby_AnnotatesSentence() =>
        Assert.Equal("[会社|かいしゃ]へ[行|い]かずに", _generator.ToRuby("会社へ行かずに"));

    [Theory]
    [InlineData("Ａ：いいえ、そのつもりはありません。")]
    [InlineData("日本語 と 英語")]
    [InlineData("最近、結婚したがらない若者が増えています。")]
    [InlineData("１週間ぐらい休みを取りますか。")]
    public void ToRuby_PreservesOriginalText(string sentence) =>
        Assert.Equal(sentence, RubyText.PlainText(_generator.ToRuby(sentence)));

    [Fact]
    public void ToRuby_AppliesOverrides()
    {
        using var generator = new NMeCabFuriganaGenerator(new Dictionary<string, string> { ["金閣寺"] = "きんかくじ", ["一度"] = "いちど" });

        var ruby = generator.ToRuby("京都にいるうちに一度金閣寺に行ってみたい。");

        Assert.Contains("[金閣寺|きんかくじ]", ruby);
        Assert.Contains("[一度|いちど]", ruby);
        Assert.Equal("京都にいるうちに一度金閣寺に行ってみたい。", RubyText.PlainText(ruby));
    }

    [Fact]
    public void Apply_FillsOriginalAndEnrichmentExamples()
    {
        var book = FuriganaApplier.Apply(TestBooks.Sample(), new FakeGenerator());

        var point = book.Lessons[1].Points[0];
        Assert.Equal("R:熱いうちに食べてください。", point.Examples[0].Ruby);
        Assert.Equal("R:若いうちに旅行したい。", point.Enrichment!.Examples[0].Ruby);
        Assert.Equal("R:夏休みの間に、富士山に登ります。", book.Lessons[1].Points[1].Examples[0].Ruby);
    }

    [Fact]
    public void Overrides_Load_MissingFileIsEmpty_BadJsonNamesFile()
    {
        var dir = Directory.CreateTempSubdirectory("furi-").FullName;
        try
        {
            Assert.Empty(FuriganaOverrides.Load(Path.Combine(dir, "furigana-overrides.json")));

            var bad = Path.Combine(dir, "furigana-overrides.json");
            File.WriteAllText(bad, "{ \"金閣寺\": ");
            var ex = Assert.Throws<InvalidDataException>(() => FuriganaOverrides.Load(bad));
            Assert.Contains("furigana-overrides.json", ex.Message);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class FakeGenerator : IFuriganaGenerator
    {
        public string ToRuby(string japanese) => "R:" + japanese;
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~FuriganaTests"`
Expected: build FAIL — các kiểu furigana chưa tồn tại.

- [ ] **Step 3: Viết `src/Japan.Importer/Furigana/IFuriganaGenerator.cs`**

```csharp
namespace Japan.Importer.Furigana;

public interface IFuriganaGenerator
{
    string ToRuby(string japanese);
}
```

- [ ] **Step 4: Viết `src/Japan.Importer/Furigana/NMeCabFuriganaGenerator.cs`**

```csharp
using Japan.Core;
using NMeCab.Specialized;

namespace Japan.Importer.Furigana;

public sealed class NMeCabFuriganaGenerator : IFuriganaGenerator, IDisposable
{
    private readonly MeCabIpaDicTagger _tagger = MeCabIpaDicTagger.Create();
    private readonly IReadOnlyList<KeyValuePair<string, string>> _overrides;

    public NMeCabFuriganaGenerator(IReadOnlyDictionary<string, string>? overrides = null)
    {
        _overrides = (overrides ?? new Dictionary<string, string>())
            .OrderByDescending(kv => kv.Key.Length)
            .ToList();
    }

    public string ToRuby(string japanese)
    {
        var segments = new List<RubySegment>();
        AppendWithOverrides(japanese, segments);
        return RubyText.Format(segments);
    }

    public static IReadOnlyList<RubySegment> Split(string surface, string? reading)
    {
        if (!Kana.ContainsKanji(surface) || string.IsNullOrEmpty(reading) || reading == "*")
            return [new RubySegment(surface, null)];

        var kana = Kana.ToHiragana(reading);
        var folded = Kana.ToHiragana(surface);

        var prefix = 0;
        while (prefix < folded.Length && prefix < kana.Length && folded[prefix] == kana[prefix] && !Kana.IsKanji(surface[prefix]))
            prefix++;

        var suffix = 0;
        while (suffix < folded.Length - prefix && suffix < kana.Length - prefix
               && folded[^(suffix + 1)] == kana[^(suffix + 1)] && !Kana.IsKanji(surface[^(suffix + 1)]))
            suffix++;

        if (kana.Length - prefix - suffix <= 0) return [new RubySegment(surface, null)];

        var result = new List<RubySegment>(3);
        if (prefix > 0) result.Add(new RubySegment(surface[..prefix], null));
        result.Add(new RubySegment(surface[prefix..^suffix], kana[prefix..^suffix]));
        if (suffix > 0) result.Add(new RubySegment(surface[^suffix..], null));
        return result;
    }

    public void Dispose() => _tagger.Dispose();

    private void AppendWithOverrides(string text, List<RubySegment> segments)
    {
        if (text.Length == 0) return;

        foreach (var (word, reading) in _overrides)
        {
            var at = text.IndexOf(word, StringComparison.Ordinal);
            if (at < 0) continue;
            AppendWithOverrides(text[..at], segments);
            segments.AddRange(Split(word, reading));
            AppendWithOverrides(text[(at + word.Length)..], segments);
            return;
        }

        // MeCab bỏ qua khoảng trắng, nên dò lại vị trí từng token để không làm mất ký tự gốc.
        var pos = 0;
        foreach (var node in _tagger.Parse(text))
        {
            var at = text.IndexOf(node.Surface, pos, StringComparison.Ordinal);
            if (at < 0) continue;
            if (at > pos) segments.Add(new RubySegment(text[pos..at], null));
            segments.AddRange(Split(node.Surface, node.Reading));
            pos = at + node.Surface.Length;
        }

        if (pos < text.Length) segments.Add(new RubySegment(text[pos..], null));
    }
}
```

- [ ] **Step 5: Viết `src/Japan.Importer/Furigana/FuriganaOverrides.cs`**

```csharp
using System.Text.Json;

namespace Japan.Importer.Furigana;

public static class FuriganaOverrides
{
    public static IReadOnlyDictionary<string, string> Load(string path)
    {
        if (!File.Exists(path)) return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)}: JSON không hợp lệ — {ex.Message}", ex);
        }
    }
}
```

- [ ] **Step 6: Viết `src/Japan.Importer/Furigana/FuriganaApplier.cs`**

```csharp
using Japan.Core;

namespace Japan.Importer.Furigana;

public static class FuriganaApplier
{
    public static GrammarBook Apply(GrammarBook book, IFuriganaGenerator generator)
    {
        Example Fill(Example e) => e with { Ruby = generator.ToRuby(e.Japanese) };

        return book with
        {
            Lessons = book.Lessons.Select(l => l with
            {
                Points = l.Points.Select(p => p with
                {
                    Examples = p.Examples.Select(Fill).ToList(),
                    Enrichment = p.Enrichment is null
                        ? null
                        : p.Enrichment with { Examples = p.Enrichment.Examples.Select(Fill).ToList() },
                }).ToList(),
            }).ToList(),
        };
    }
}
```

- [ ] **Step 7: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~FuriganaTests"`
Expected: PASS. Nếu một ca `Split` với cách đọc thật của IPADIC khác kỳ vọng (vd `お茶` bị tách thành 2 token), chỉ sửa ca đó trong `ToRuby_*`; các ca `Split_*` dùng cách đọc cố định nên phải pass nguyên trạng.

- [ ] **Step 8: Commit**

```powershell
git add -A
git commit -m "feat(importer): generate furigana with NMeCab and overrides"
```

---

### Task 8: Importer pipeline, Program, chạy trên dữ liệu thật

**Files:**
- Create: `src/Japan.Importer/SourceDiscovery.cs`, `src/Japan.Importer/RepoRoot.cs`, `src/Japan.Importer/ImportPipeline.cs`, `src/Japan.Importer/appsettings.json`, `content/furigana-overrides.json`, `content/enrichment/.gitkeep`
- Modify: `src/Japan.Importer/Program.cs` (thay toàn bộ), `src/Japan.Importer/Japan.Importer.csproj` (copy appsettings)
- Test: `tests/Japan.Tests/Importer/ImportPipelineTests.cs`
- Output: `data/grammar.json`

**Interfaces:**
- Consumes: `MarkdownGrammarReader`, `DocxGrammarReader`, `SourceFileLockedException` (Task 4–5); `EnrichmentLoader`, `BookAssembler` (Task 6); `IFuriganaGenerator`, `NMeCabFuriganaGenerator`, `FuriganaOverrides`, `FuriganaApplier` (Task 7); `GrammarJson` (Task 1)
- Produces:
  - `static class SourceDiscovery { IReadOnlyList<string> Find(string directory) }`
  - `static class RepoRoot { string Find(string startDirectory) }`
  - `record ImportSettings(string SourceDirectory, string ContentDirectory, string OutputPath, int ExpectedPointCount = 106)`
  - `static class ImportPipeline { int Run(ImportSettings settings, TextWriter output, IFuriganaGenerator generator, TimeProvider? clock = null) }` — trả `0` thành công, `1` lỗi dữ liệu/khoá file, `2` không có thư mục nguồn

- [ ] **Step 1: Viết test thất bại `tests/Japan.Tests/Importer/ImportPipelineTests.cs`**

```csharp
using Japan.Core;
using Japan.Importer;
using Japan.Importer.Furigana;
using static Japan.Tests.DocxFixture;

namespace Japan.Tests.Importer;

public sealed class ImportPipelineTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("import-").FullName;
    private string Source => Path.Combine(_root, "source");
    private string Content => Path.Combine(_root, "content");
    private string Output => Path.Combine(_root, "data", "grammar.json");

    public ImportPipelineTests()
    {
        Directory.CreateDirectory(Source);
        Directory.CreateDirectory(Path.Combine(Content, "enrichment"));
        Save(Path.Combine(Source, "NguPhap_N3_File01_Bai1.docx"),
            P("FILE 1 — điểm 1–1/106"),
            P("BÀI 1: Mở đầu", "Heading1"),
            P("1. ～ば", "Heading2"),
            P("Ý nghĩa: Nếu."), P("Cấu trúc: Vば"), P("Ví dụ:"),
            P("1. 雨が降れば、行かない。"), P("→ Nếu mưa thì không đi."));
        File.WriteAllText(Path.Combine(Source, "NguPhap_N3_File02_Bai2.md"), """
            # FILE 2 (điểm 2–2/106)
            ## BÀI 2: Tiếp theo
            ### 1. ～たら
            **Ý nghĩa:** Sau khi.

            **Cấu trúc:** Vたら

            **Ví dụ:**
            1. 家に帰ったら、電話します。
               → Về nhà rồi tôi sẽ gọi điện.
            """);
        Save(Path.Combine(Source, "NguPhap_N3_File02_Bai2.docx"), P("bản Word lỗi, phải bị bỏ qua"));
        Save(Path.Combine(Source, "Ngữ pháp N3_1.docx"), P("không phải nguồn"));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private ImportSettings Settings => new(Source, Content, Output, ExpectedPointCount: 2);

    [Fact]
    public void Find_PrefersMarkdownAndIgnoresOtherFiles()
    {
        var files = SourceDiscovery.Find(Source).Select(Path.GetFileName);

        Assert.Equal(["NguPhap_N3_File01_Bai1.docx", "NguPhap_N3_File02_Bai2.md"], files);
    }

    [Fact]
    public void Run_WritesGrammarJsonWithFurigana()
    {
        var output = new StringWriter();

        var exit = ImportPipeline.Run(Settings, output, new FakeGenerator());

        Assert.Equal(0, exit);
        var book = GrammarJson.Deserialize(File.ReadAllText(Output));
        Assert.Equal(["b1-1", "b2-1"], book.Lessons.SelectMany(l => l.Points).Select(p => p.Id));
        Assert.Equal("R:家に帰ったら、電話します。", book.Lessons[1].Points[0].Examples[0].Ruby);
        Assert.Contains("2 điểm", output.ToString());
        Assert.Contains("Chưa có phần bổ sung: b1-1, b2-1", output.ToString());
    }

    [Fact]
    public void Run_BadEnrichment_FailsWithoutOverwritingOutput()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Output)!);
        File.WriteAllText(Output, "cũ");
        File.WriteAllText(Path.Combine(Content, "enrichment", "b1-1.json"), "{ hỏng");
        var output = new StringWriter();

        var exit = ImportPipeline.Run(Settings, output, new FakeGenerator());

        Assert.Equal(1, exit);
        Assert.Equal("cũ", File.ReadAllText(Output));
        Assert.Contains("b1-1.json", output.ToString());
    }

    [Fact]
    public void Run_LockedSourceFile_FailsWithHint()
    {
        using var _ = new FileStream(Path.Combine(Source, "NguPhap_N3_File01_Bai1.docx"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var output = new StringWriter();

        var exit = ImportPipeline.Run(Settings, output, new FakeGenerator());

        Assert.Equal(1, exit);
        Assert.Contains("đóng Word", output.ToString());
        Assert.False(File.Exists(Output));
    }

    [Fact]
    public void Run_MissingSourceDirectory_Returns2()
    {
        var exit = ImportPipeline.Run(Settings with { SourceDirectory = Path.Combine(_root, "khong-co") }, new StringWriter(), new FakeGenerator());

        Assert.Equal(2, exit);
    }

    private sealed class FakeGenerator : IFuriganaGenerator
    {
        public string ToRuby(string japanese) => "R:" + japanese;
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~ImportPipelineTests"`
Expected: build FAIL — `ImportPipeline`, `SourceDiscovery`, `ImportSettings` chưa tồn tại.

- [ ] **Step 3: Viết `src/Japan.Importer/SourceDiscovery.cs`**

```csharp
using System.Globalization;
using System.Text.RegularExpressions;

namespace Japan.Importer;

public static partial class SourceDiscovery
{
    [GeneratedRegex(@"^NguPhap_N3_File(\d+)_.+\.(docx|md)$", RegexOptions.IgnoreCase)]
    private static partial Regex FileRegex();

    public static IReadOnlyList<string> Find(string directory) =>
        Directory.EnumerateFiles(directory)
            .Select(file => (File: file, Match: FileRegex().Match(Path.GetFileName(file))))
            .Where(x => x.Match.Success)
            .GroupBy(x => int.Parse(x.Match.Groups[1].Value, CultureInfo.InvariantCulture))
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(x => x.Match.Groups[2].Value.Equals("md", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().File)
            .ToList();
}
```

- [ ] **Step 4: Viết `src/Japan.Importer/RepoRoot.cs`**

```csharp
namespace Japan.Importer;

public static class RepoRoot
{
    public static string Find(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Japan.slnx"))) return dir.FullName;
        }

        throw new DirectoryNotFoundException($"Không tìm thấy Japan.slnx từ {startDirectory}. Hãy chạy lệnh trong thư mục repo.");
    }
}
```

- [ ] **Step 5: Viết `src/Japan.Importer/ImportPipeline.cs`**

```csharp
using System.Text;
using Japan.Core;
using Japan.Importer.Assembly;
using Japan.Importer.Furigana;
using Japan.Importer.Parsing;

namespace Japan.Importer;

public sealed record ImportSettings(
    string SourceDirectory,
    string ContentDirectory,
    string OutputPath,
    int ExpectedPointCount = BookAssembler.ExpectedPointCount);

public static class ImportPipeline
{
    public static int Run(ImportSettings settings, TextWriter output, IFuriganaGenerator generator, TimeProvider? clock = null)
    {
        if (!Directory.Exists(settings.SourceDirectory))
        {
            output.WriteLine($"Không tìm thấy thư mục nguồn: {settings.SourceDirectory}");
            return 2;
        }

        var files = SourceDiscovery.Find(settings.SourceDirectory);
        output.WriteLine($"Đọc {files.Count} file từ {settings.SourceDirectory}");

        var documents = new List<SourceDocument>();
        foreach (var file in files)
        {
            SourceDocument doc;
            try
            {
                doc = file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    ? MarkdownGrammarReader.ReadFile(file)
                    : DocxGrammarReader.ReadFile(file);
            }
            catch (SourceFileLockedException ex)
            {
                output.WriteLine($"LỖI: {ex.Message}");
                return 1;
            }

            documents.Add(doc);
            output.WriteLine($"  {doc.FileName}: {doc.Lessons.Count} bài, {doc.Lessons.Sum(l => l.Points.Count)} điểm");
            foreach (var warning in doc.Warnings) output.WriteLine($"    cảnh báo: {warning}");
        }

        var enrichment = EnrichmentLoader.Load(Path.Combine(settings.ContentDirectory, "enrichment"));
        var assembled = BookAssembler.Assemble(
            documents, enrichment.Items, (clock ?? TimeProvider.System).GetUtcNow(), settings.ExpectedPointCount);
        var errors = enrichment.Errors.Concat(assembled.Errors).ToList();
        if (errors.Count > 0 || assembled.Book is null)
        {
            output.WriteLine($"LỖI ({errors.Count}) — không ghi {settings.OutputPath}:");
            foreach (var error in errors) output.WriteLine($"  - {error}");
            return 1;
        }

        var book = FuriganaApplier.Apply(assembled.Book, generator);
        var fullOutput = Path.GetFullPath(settings.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutput)!);
        var temp = fullOutput + ".tmp";
        File.WriteAllText(temp, GrammarJson.Serialize(book), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(temp, fullOutput, overwrite: true);

        var points = book.Lessons.SelectMany(l => l.Points).ToList();
        output.WriteLine($"Đã ghi {settings.OutputPath}: {book.Lessons.Count} bài, {points.Count} điểm, {points.Count(p => p.Enrichment is not null)} điểm có phần bổ sung.");
        var missing = points.Where(p => p.Enrichment is null).Select(p => p.Id).ToList();
        if (missing.Count > 0) output.WriteLine($"Chưa có phần bổ sung: {string.Join(", ", missing)}");
        return 0;
    }
}
```

- [ ] **Step 6: Thay `src/Japan.Importer/Program.cs`**

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
    var config = JsonSerializer.Deserialize<ImporterConfig>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")), GrammarJson.Options)
        ?? throw new InvalidDataException("appsettings.json rỗng.");
    var settings = new ImportSettings(
        SourceDirectory: args.Length > 0 ? args[0] : config.SourceDirectory,
        ContentDirectory: Path.Combine(repoRoot, "content"),
        OutputPath: Path.Combine(repoRoot, "data", "grammar.json"));

    var overrides = FuriganaOverrides.Load(Path.Combine(settings.ContentDirectory, "furigana-overrides.json"));
    using var generator = new NMeCabFuriganaGenerator(overrides);
    return ImportPipeline.Run(settings, Console.Out, generator);
}
catch (Exception ex) when (ex is InvalidDataException or DirectoryNotFoundException)
{
    Console.Error.WriteLine($"LỖI: {ex.Message}");
    return 1;
}

internal sealed record ImporterConfig(string SourceDirectory);
```

- [ ] **Step 7: Tạo `src/Japan.Importer/appsettings.json` và cấu hình copy**

```json
{
  "sourceDirectory": "C:\\Users\\Admin\\Downloads\\Tiếng Nhật - N3\\Ngữ pháp"
}
```

Thêm vào `src/Japan.Importer/Japan.Importer.csproj` (trong `<Project>`):

```xml
  <ItemGroup>
    <None Update="appsettings.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

- [ ] **Step 8: Tạo `content/furigana-overrides.json` và `content/enrichment/.gitkeep`**

```json
{
}
```

`content/enrichment/.gitkeep` là file rỗng.

- [ ] **Step 9: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~ImportPipelineTests"`
Expected: PASS (5 tests).

- [ ] **Step 10: Commit code**

```powershell
git add -A
git commit -m "feat(importer): add import pipeline and command line entry point"
```

- [ ] **Step 11: Chạy importer trên dữ liệu thật**

Run: `dotnet run --project src/Japan.Importer`
Expected: `Đọc 12 file …`, mỗi file in số điểm khớp dòng đầu file, kết thúc bằng `Đã ghi …\data\grammar.json: 30 bài, 106 điểm, 0 điểm có phần bổ sung.` và exit code 0.

Nếu có lỗi hoặc cảnh báo: với mỗi khuôn dạng lạ, **trước tiên** thêm một test tái hiện đúng đoạn văn đó vào `DocumentBuilderTests` (hoặc `DocxGrammarReaderTests`), chạy để thấy fail, sửa `DocumentBuilder`/reader, chạy lại toàn bộ `dotnet test`, rồi chạy lại importer. Lặp tới khi 106 điểm, 0 lỗi. Cảnh báo còn lại phải được đọc và xác nhận là đoạn văn không cần (vd câu giới thiệu), nếu không thì xử lý như lỗi.

- [ ] **Step 12: Kiểm tra ngẫu nhiên nội dung**

Mở `data/grammar.json`, so 3 điểm bất kỳ (gợi ý `b7-3` có bảng cấu trúc, `b5-2` có bảng ý nghĩa từ File02.md, một điểm của File08) với file Word gốc: mẫu, ý nghĩa, cấu trúc, số ví dụ, câu dịch phải khớp; `ruby` có furigana hợp lý. Từ đọc sai → thêm vào `content/furigana-overrides.json`, chạy lại importer.

- [ ] **Step 13: Commit dữ liệu**

```powershell
git add data/grammar.json content/furigana-overrides.json
git commit -m "data: import 106 N3 grammar points"
```

---

### Task 9: GrammarSearch

**Files:**
- Create: `src/Japan.Core/GrammarSearch.cs`
- Test: `tests/Japan.Tests/Core/GrammarSearchTests.cs`

**Interfaces:**
- Consumes: `GrammarBook`, `GrammarPoint`, `Section` (Task 1); `TextNormalizer` (Task 2)
- Produces:
  - `record SearchHit(GrammarPoint Point, string Field, string Snippet)` — `Field` ∈ `"Mẫu" | "Ý nghĩa" | "Cấu trúc" | "Lưu ý" | "Bổ sung" | "So sánh" | "Ví dụ"`
  - `sealed class GrammarSearch(GrammarBook book) { IReadOnlyList<SearchHit> Search(string? query, int limit = 50) }`

Trọng số: Mẫu 100 > Ý nghĩa 50 > Cấu trúc/Lưu ý/Bổ sung/So sánh 20 > Ví dụ 10; mỗi điểm lấy trường khớp có trọng số cao nhất; cùng trọng số thì theo thứ tự trong sách. Khớp = chuỗi con sau chuẩn hoá.

- [ ] **Step 1: Viết test thất bại `tests/Japan.Tests/Core/GrammarSearchTests.cs`**

```csharp
using Japan.Core;

namespace Japan.Tests.Core;

public class GrammarSearchTests
{
    private readonly GrammarSearch _search = new(TestBooks.Sample());

    [Theory]
    [InlineData("うちに")]
    [InlineData("～うちに")]
    [InlineData("ウチニ")]
    public void Search_PatternMatchesInKanaVariants(string query)
    {
        var hit = _search.Search(query)[0];

        Assert.Equal("b7-1", hit.Point.Id);
        Assert.Equal("Mẫu", hit.Field);
    }

    [Theory]
    [InlineData("tranh thủ")]
    [InlineData("tranh thu")]
    [InlineData("TRANH THU")]
    public void Search_VietnameseIgnoresDiacriticsAndCase(string query)
    {
        var hit = Assert.Single(_search.Search(query));

        Assert.Equal("b7-1", hit.Point.Id);
        Assert.Equal("Ý nghĩa", hit.Field);
    }

    [Fact]
    public void Search_RanksPatternAboveComparison()
    {
        var hits = _search.Search("間に");

        Assert.Equal(["b7-3", "b7-1"], hits.Select(h => h.Point.Id));
        Assert.Equal("So sánh", hits[1].Field);
    }

    [Fact]
    public void Search_MatchesExampleTranslation()
    {
        var hit = Assert.Single(_search.Search("phu si"));

        Assert.Equal("b7-3", hit.Point.Id);
        Assert.Equal("Ví dụ", hit.Field);
        Assert.Contains("富士山", hit.Snippet);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("～")]
    public void Search_EmptyQuery_ReturnsNothing(string? query) => Assert.Empty(_search.Search(query));

    [Fact]
    public void Search_RespectsLimit()
    {
        // TestBooks.Point đặt Cấu trúc "Vる + …" cho mọi điểm nên "vる" khớp cả 3.
        Assert.True(_search.Search("vる").Count >= 2);
        Assert.Single(_search.Search("vる", limit: 1));
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~GrammarSearchTests"`
Expected: build FAIL — `GrammarSearch` chưa tồn tại.

- [ ] **Step 3: Viết `src/Japan.Core/GrammarSearch.cs`**

```csharp
namespace Japan.Core;

public sealed record SearchHit(GrammarPoint Point, string Field, string Snippet);

public sealed class GrammarSearch
{
    private const int SnippetLength = 160;

    private readonly record struct Field(int Weight, string Name, string Raw, string Normalized);

    private readonly IReadOnlyList<(GrammarPoint Point, IReadOnlyList<Field> Fields)> _entries;

    public GrammarSearch(GrammarBook book)
    {
        _entries = book.Lessons
            .SelectMany(l => l.Points)
            .Select(p => (p, (IReadOnlyList<Field>)Fields(p).ToList()))
            .ToList();
    }

    public IReadOnlyList<SearchHit> Search(string? query, int limit = 50)
    {
        var q = TextNormalizer.Normalize(query);
        if (q.Length == 0) return [];

        return _entries
            .Select((entry, order) => (entry.Point, Order: order,
                Best: entry.Fields.Where(f => f.Normalized.Contains(q, StringComparison.Ordinal))
                                  .OrderByDescending(f => f.Weight)
                                  .FirstOrDefault()))
            .Where(x => x.Best.Weight > 0)
            .OrderByDescending(x => x.Best.Weight)
            .ThenBy(x => x.Order)
            .Take(limit)
            .Select(x => new SearchHit(x.Point, x.Best.Name, Snippet(x.Best.Raw)))
            .ToList();
    }

    private static IEnumerable<Field> Fields(GrammarPoint p)
    {
        Field F(int weight, string name, string raw) => new(weight, name, raw, TextNormalizer.Normalize(raw));

        yield return F(100, "Mẫu", p.Pattern);
        foreach (var s in Texts(p.Meaning)) yield return F(50, "Ý nghĩa", s);
        foreach (var s in Texts(p.Structure)) yield return F(20, "Cấu trúc", s);
        if (p.Note is { } note)
        {
            foreach (var s in Texts(note)) yield return F(20, "Lưu ý", s);
        }

        if (p.Enrichment is { } e)
        {
            yield return F(20, "Bổ sung", e.Explanation);
            foreach (var s in e.Usage.Concat(e.Pitfalls)) yield return F(20, "Bổ sung", s);
            foreach (var c in e.Comparisons) yield return F(20, "So sánh", $"{c.Pattern}: {c.Difference}");
        }

        foreach (var ex in p.Examples.Concat(p.Enrichment?.Examples ?? []))
            yield return F(10, "Ví dụ", $"{ex.Japanese} — {ex.Vietnamese}");
    }

    private static IEnumerable<string> Texts(Section s) =>
        s.Paragraphs.Concat(s.Tables.SelectMany(t => t.Header.Concat(t.Rows.SelectMany(r => r))));

    private static string Snippet(string raw) => raw.Length <= SnippetLength ? raw : raw[..(SnippetLength - 1)] + "…";
}
```

- [ ] **Step 4: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~GrammarSearchTests"`
Expected: PASS (tất cả).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat(core): add in-memory grammar search"
```

---

### Task 10: Web — repository, trang mục lục / bài / chi tiết, furigana

**Files:**
- Create: `src/Japan.Web/Services/GrammarDataOptions.cs`, `src/Japan.Web/Services/GrammarRepository.cs`, `src/Japan.Web/Rendering/RubyHtml.cs`, `src/Japan.Web/Pages/Lesson.cshtml(.cs)`, `src/Japan.Web/Pages/Grammar.cshtml(.cs)`, `src/Japan.Web/Pages/Shared/_Section.cshtml`, `_Table.cshtml`, `_Examples.cshtml`
- Modify (thay toàn bộ): `src/Japan.Web/Program.cs`, `src/Japan.Web/appsettings.json`, `src/Japan.Web/Pages/_ViewImports.cshtml`, `src/Japan.Web/Pages/Shared/_Layout.cshtml`, `src/Japan.Web/Pages/Index.cshtml(.cs)`, `src/Japan.Web/wwwroot/css/site.css`, `src/Japan.Web/wwwroot/js/site.js`
- Test: `tests/Japan.Tests/Web/GrammarWebFactory.cs`, `tests/Japan.Tests/Web/PagesTests.cs`, `tests/Japan.Tests/Web/RenderingTests.cs`

**Interfaces:**
- Consumes: `GrammarBook`, `GrammarJson`, `RubyText`, `GrammarSearch`, `SearchHit` (Task 1, 2, 9)
- Produces:
  - `sealed class GrammarDataOptions { const string Section = "GrammarData"; string Path }`
  - `sealed class GrammarRepository : IDisposable { bool IsLoaded; string DataPath; IReadOnlyList<Lesson> Lessons; Lesson? FindLesson(int); GrammarPoint? Find(string); (GrammarPoint? Previous, GrammarPoint? Next) Neighbors(string); IReadOnlyList<SearchHit> Search(string?); void Reload() }`
  - `static class RubyHtml { IHtmlContent Ruby(this IHtmlHelper, Example); IHtmlContent Render(string) }`
  - `GrammarWebFactory(string dataPath) : WebApplicationFactory<Program>` (test)
  - Route: `/`, `/bai/{n:int}`, `/nguphap/{id}`

- [ ] **Step 1: Viết `tests/Japan.Tests/Web/GrammarWebFactory.cs`**

```csharp
using Japan.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Japan.Tests.Web;

public sealed class GrammarWebFactory(string dataPath) : WebApplicationFactory<Program>
{
    public static GrammarWebFactory WithSampleData()
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory("web-").FullName, "grammar.json");
        File.WriteAllText(path, GrammarJson.Serialize(TestBooks.Sample()));
        return new GrammarWebFactory(path);
    }

    public string DataPath => dataPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("GrammarData:Path", dataPath);
}
```

- [ ] **Step 2: Viết test thất bại `tests/Japan.Tests/Web/PagesTests.cs`**

```csharp
using System.Net;
using Japan.Core;

namespace Japan.Tests.Web;

public sealed class PagesTests : IDisposable
{
    private readonly GrammarWebFactory _factory = GrammarWebFactory.WithSampleData();

    public void Dispose() => _factory.Dispose();

    private Task<string> Get(string url) => _factory.CreateClient().GetStringAsync(url);

    [Fact]
    public async Task Index_ListsLessonsAndPatterns()
    {
        var html = await Get("/");

        Assert.Contains("Bài 7: Thời điểm của sự việc", html);
        Assert.Contains("～うちに（１）", html);
        Assert.Contains("href=\"/nguphap/b7-1\"", html);
    }

    [Fact]
    public async Task Lesson_ShowsPointsAndQuickComparison()
    {
        var html = await Get("/bai/7");

        Assert.Contains("～間・～間に", html);
        Assert.Contains("Tranh thủ khi còn...", html);
    }

    [Fact]
    public async Task Grammar_ShowsOriginalEnrichmentRubyAndNavigation()
    {
        var html = await Get("/nguphap/b7-1");

        Assert.Contains("<ruby>熱<rt>あつ</rt></ruby>", html);
        Assert.Contains("Bổ sung", html);
        Assert.Contains("Nhấn mạnh việc tranh thủ.", html);
        Assert.Contains("熱い → 熱いうちに", html);
        Assert.Contains("<ruby>若<rt>わか</rt></ruby>", html);
        Assert.Contains("href=\"/nguphap/b7-3\"", html);
        Assert.Contains("href=\"/nguphap/b6-3\"", html);
    }

    [Fact]
    public async Task Grammar_WithoutEnrichment_HasNoEnrichmentSection()
    {
        var html = await Get("/nguphap/b7-3");

        Assert.Contains("夏休みの間に", html);
        Assert.DoesNotContain("class=\"card mb-4 enrichment\"", html);
    }

    [Theory]
    [InlineData("/bai/99")]
    [InlineData("/nguphap/b99-1")]
    public async Task UnknownLessonOrPoint_Returns404(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MissingDataFile_ShowsImportHint()
    {
        using var factory = new GrammarWebFactory(Path.Combine(Path.GetTempPath(), $"none-{Guid.NewGuid():N}", "grammar.json"));

        var response = await factory.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Chưa có dữ liệu", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DataFileChange_IsReloaded()
    {
        var client = _factory.CreateClient();
        await client.GetStringAsync("/");
        var book = TestBooks.Sample();
        var renamed = book with { Lessons = [book.Lessons[0] with { Title = "Tiêu đề mới" }, book.Lessons[1]] };
        File.WriteAllText(_factory.DataPath, GrammarJson.Serialize(renamed));

        var html = "";
        for (var i = 0; i < 50 && !html.Contains("Tiêu đề mới"); i++)
        {
            await Task.Delay(100);
            html = await client.GetStringAsync("/");
        }

        Assert.Contains("Bài 6: Tiêu đề mới", html);
    }
}
```

- [ ] **Step 3: Viết test thất bại `tests/Japan.Tests/Web/RenderingTests.cs`**

```csharp
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Japan.Web.Rendering;
using Microsoft.AspNetCore.Html;

namespace Japan.Tests.Web;

public class RenderingTests
{
    internal static string Html(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Create(UnicodeRanges.All));
        return writer.ToString();
    }

    [Fact]
    public void Render_WrapsReadingsInRubyAndEncodesText() =>
        Assert.Equal("<ruby>漢字<rt>かんじ</rt></ruby>&lt;b&gt;", Html(RubyHtml.Render("[漢字|かんじ]<b>")));

    [Fact]
    public void Render_PlainText_HasNoRuby() =>
        Assert.Equal("いいえ。", Html(RubyHtml.Render("いいえ。")));
}
```

- [ ] **Step 4: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~Japan.Tests.Web"`
Expected: build FAIL — `RubyHtml`, `Program` có thể truy cập nhưng trang `/bai`, `/nguphap` chưa có.

- [ ] **Step 5: Viết `src/Japan.Web/Services/GrammarDataOptions.cs`**

```csharp
namespace Japan.Web.Services;

public sealed class GrammarDataOptions
{
    public const string Section = "GrammarData";

    public string Path { get; set; } = "../../data/grammar.json";
}
```

- [ ] **Step 6: Viết `src/Japan.Web/Services/GrammarRepository.cs`**

```csharp
using System.Text.Json;
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

    private readonly ILogger<GrammarRepository> _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly Timer _debounce;
    private volatile Snapshot? _snapshot;

    public GrammarRepository(IOptions<GrammarDataOptions> options, IWebHostEnvironment env, ILogger<GrammarRepository> logger)
    {
        _logger = logger;
        DataPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.Path));
        _debounce = new Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);
        Reload();

        var directory = Path.GetDirectoryName(DataPath)!;
        if (!Directory.Exists(directory)) return;

        _watcher = new FileSystemWatcher(directory, Path.GetFileName(DataPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += (_, _) => ScheduleReload();
        _watcher.Created += (_, _) => ScheduleReload();
        _watcher.Renamed += (_, _) => ScheduleReload();
        _watcher.EnableRaisingEvents = true;
    }

    public string DataPath { get; }

    public bool IsLoaded => _snapshot is not null;

    public IReadOnlyList<Lesson> Lessons => _snapshot?.Book.Lessons ?? [];

    public Lesson? FindLesson(int number) => Lessons.FirstOrDefault(l => l.Number == number);

    public GrammarPoint? Find(string id) =>
        _snapshot is { } s && s.IndexById.TryGetValue(id, out var i) ? s.Ordered[i] : null;

    public (GrammarPoint? Previous, GrammarPoint? Next) Neighbors(string id)
    {
        if (_snapshot is not { } s || !s.IndexById.TryGetValue(id, out var i)) return (null, null);
        return (i > 0 ? s.Ordered[i - 1] : null, i < s.Ordered.Count - 1 ? s.Ordered[i + 1] : null);
    }

    public IReadOnlyList<SearchHit> Search(string? query) => _snapshot?.Search.Search(query) ?? [];

    public void Reload()
    {
        try
        {
            if (!File.Exists(DataPath))
            {
                _logger.LogWarning("Không tìm thấy {Path}", DataPath);
                return;
            }

            var book = GrammarJson.Deserialize(File.ReadAllText(DataPath));
            var ordered = book.Lessons.SelectMany(l => l.Points).ToList();
            var index = ordered.Select((p, i) => (p.Id, i)).ToDictionary(x => x.Id, x => x.i, StringComparer.Ordinal);
            _snapshot = new Snapshot(book, ordered, index, new GrammarSearch(book));
            _logger.LogInformation("Đã nạp {Count} điểm ngữ pháp từ {Path}", ordered.Count, DataPath);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
        {
            _logger.LogError(ex, "Không đọc được {Path}, giữ dữ liệu cũ", DataPath);
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounce.Dispose();
    }

    private void ScheduleReload() => _debounce.Change(300, Timeout.Infinite);
}
```

- [ ] **Step 7: Viết `src/Japan.Web/Rendering/RubyHtml.cs`**

```csharp
using Japan.Core;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Japan.Web.Rendering;

public static class RubyHtml
{
    public static IHtmlContent Ruby(this IHtmlHelper _, Example example) => Render(example.Ruby ?? example.Japanese);

    public static IHtmlContent Render(string ruby)
    {
        var builder = new HtmlContentBuilder();
        foreach (var segment in RubyText.Parse(ruby))
        {
            if (segment.Reading is null)
            {
                builder.Append(segment.Text);
                continue;
            }

            builder.AppendHtml("<ruby>").Append(segment.Text)
                   .AppendHtml("<rt>").Append(segment.Reading).AppendHtml("</rt></ruby>");
        }

        return builder;
    }
}
```

- [ ] **Step 8: Thay `src/Japan.Web/Program.cs`**

```csharp
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Japan.Web.Services;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.Configure<GrammarDataOptions>(builder.Configuration.GetSection(GrammarDataOptions.Section));
builder.Services.AddSingleton<GrammarRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

public partial class Program
{
}
```

- [ ] **Step 9: Thay `src/Japan.Web/appsettings.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GrammarData": {
    "Path": "../../data/grammar.json"
  }
}
```

Trong `src/Japan.Web/Properties/launchSettings.json`, xoá profile `https` để chỉ còn `http` (chạy local, không cần chứng chỉ).

- [ ] **Step 10: Thay `src/Japan.Web/Pages/_ViewImports.cshtml`**

```cshtml
@using Japan.Core
@using Japan.Web
@using Japan.Web.Pages
@using Japan.Web.Rendering
@using Japan.Web.Services
@namespace Japan.Web.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

- [ ] **Step 11: Thay `src/Japan.Web/Pages/Shared/_Layout.cshtml`**

```cshtml
@inject GrammarRepository Repo
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] — Ngữ pháp N3</title>
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
    <header>
        <nav class="navbar navbar-expand bg-body-tertiary border-bottom mb-4">
            <div class="container">
                <a class="navbar-brand" asp-page="/Index">Ngữ pháp N3</a>
                <button id="furigana-toggle" class="btn btn-outline-secondary btn-sm active" type="button" aria-pressed="true">
                    Furigana
                </button>
            </div>
        </nav>
    </header>
    <main class="container pb-5">
        @if (!Repo.IsLoaded)
        {
            <div class="alert alert-warning">
                <strong>Chưa có dữ liệu.</strong> Không tìm thấy <code>@Repo.DataPath</code>.
                Hãy chạy <code>dotnet run --project src/Japan.Importer</code> trong thư mục repo rồi tải lại trang.
            </div>
        }
        @RenderBody()
    </main>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

- [ ] **Step 12: Viết partial `src/Japan.Web/Pages/Shared/_Table.cshtml`, `_Section.cshtml`, `_Examples.cshtml`**

`_Table.cshtml`:

```cshtml
@model Japan.Core.Table
<div class="table-responsive">
    <table class="table table-sm table-bordered align-middle">
        @if (Model.Header.Count > 0)
        {
            <thead class="table-light"><tr>@foreach (var h in Model.Header) { <th>@h</th> }</tr></thead>
        }
        <tbody>
            @foreach (var row in Model.Rows)
            {
                <tr>@foreach (var cell in row) { <td class="jp">@cell</td> }</tr>
            }
        </tbody>
    </table>
</div>
```

`_Section.cshtml`:

```cshtml
@model Japan.Core.Section
@foreach (var paragraph in Model.Paragraphs)
{
    <p class="jp">@paragraph</p>
}
@foreach (var table in Model.Tables)
{
    <partial name="_Table" model="table" />
}
```

`_Examples.cshtml`:

```cshtml
@model IReadOnlyList<Example>
<ol class="examples">
    @foreach (var e in Model)
    {
        <li>
            <div class="jp">@Html.Ruby(e)</div>
            <div class="text-body-secondary">→ @e.Vietnamese</div>
        </li>
    }
</ol>
```

- [ ] **Step 13: Thay `src/Japan.Web/Pages/Index.cshtml` và `Index.cshtml.cs`**

`Index.cshtml.cs`:

```csharp
using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class IndexModel(GrammarRepository repo) : PageModel
{
    public IReadOnlyList<Lesson> Lessons => repo.Lessons;
}
```

`Index.cshtml`:

```cshtml
@page
@model IndexModel
@{
    ViewData["Title"] = "Mục lục";
}
<form method="get" action="/timkiem" class="mb-4" role="search">
    <input id="search-box" name="q" type="search" class="form-control form-control-lg"
           placeholder="Tìm mẫu ngữ pháp, nghĩa tiếng Việt hoặc câu ví dụ…" autocomplete="off"
           data-search-url="@Url.Page("/Search", "Partial")" />
</form>
<div id="search-results"></div>
<div id="lesson-list" class="row row-cols-1 row-cols-md-2 g-3">
    @foreach (var lesson in Model.Lessons)
    {
        <div class="col">
            <div class="card h-100">
                <div class="card-body">
                    <h2 class="h5 card-title"><a asp-page="/Lesson" asp-route-n="@lesson.Number">Bài @lesson.Number: @lesson.Title</a></h2>
                    <ul class="list-unstyled mb-0">
                        @foreach (var p in lesson.Points)
                        {
                            <li><a asp-page="/Grammar" asp-route-id="@p.Id" class="jp">@p.Pattern</a></li>
                        }
                    </ul>
                </div>
            </div>
        </div>
    }
</div>
```

`Search` page được tạo ở Task 11; trong Task 10, `Url.Page("/Search", "Partial")` trả `null` và thuộc tính bị bỏ qua — không lỗi.

- [ ] **Step 14: Viết `src/Japan.Web/Pages/Lesson.cshtml.cs` và `Lesson.cshtml`**

```csharp
using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class LessonModel(GrammarRepository repo) : PageModel
{
    public Lesson Lesson { get; private set; } = null!;

    public IActionResult OnGet(int n)
    {
        var lesson = repo.FindLesson(n);
        if (lesson is null) return NotFound();
        Lesson = lesson;
        return Page();
    }
}
```

```cshtml
@page "/bai/{n:int}"
@model LessonModel
@{
    ViewData["Title"] = $"Bài {Model.Lesson.Number}";
}
<nav aria-label="breadcrumb">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a asp-page="/Index">Mục lục</a></li>
        <li class="breadcrumb-item active" aria-current="page">Bài @Model.Lesson.Number</li>
    </ol>
</nav>
<h1 class="h3 mb-3">Bài @Model.Lesson.Number: @Model.Lesson.Title</h1>
<div class="list-group mb-4">
    @foreach (var p in Model.Lesson.Points)
    {
        <a class="list-group-item list-group-item-action" asp-page="/Grammar" asp-route-id="@p.Id">
            <div class="fw-bold jp">@p.Index. @p.Pattern</div>
            <div class="small text-body-secondary">@string.Join(" ", p.Meaning.Paragraphs)</div>
        </a>
    }
</div>
@foreach (var table in Model.Lesson.QuickComparisons)
{
    <h2 class="h5">Bảng so sánh nhanh</h2>
    <partial name="_Table" model="table" />
}
```

- [ ] **Step 15: Viết `src/Japan.Web/Pages/Grammar.cshtml.cs` và `Grammar.cshtml`**

```csharp
using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed class GrammarModel(GrammarRepository repo) : PageModel
{
    public GrammarPoint Point { get; private set; } = null!;
    public GrammarPoint? Previous { get; private set; }
    public GrammarPoint? Next { get; private set; }

    public IActionResult OnGet(string id)
    {
        var point = repo.Find(id);
        if (point is null) return NotFound();
        Point = point;
        (Previous, Next) = repo.Neighbors(id);
        return Page();
    }
}
```

```cshtml
@page "/nguphap/{id}"
@model GrammarModel
@{
    var p = Model.Point;
    var e = p.Enrichment;
    ViewData["Title"] = p.Pattern;
}
<nav aria-label="breadcrumb">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a asp-page="/Index">Mục lục</a></li>
        <li class="breadcrumb-item"><a asp-page="/Lesson" asp-route-n="@p.Lesson">Bài @p.Lesson</a></li>
        <li class="breadcrumb-item active jp" aria-current="page">@p.Pattern</li>
    </ol>
</nav>
<h1 class="jp mb-4">@p.Pattern</h1>

<section class="card mb-4">
    <div class="card-body">
        <h2 class="h6 text-uppercase text-body-secondary">Giáo trình</h2>
        <h3 class="h5">Ý nghĩa</h3>
        <partial name="_Section" model="p.Meaning" />
        <h3 class="h5">Cấu trúc</h3>
        <partial name="_Section" model="p.Structure" />
        @if (p.Note is { } note)
        {
            <h3 class="h5">Lưu ý</h3>
            <partial name="_Section" model="note" />
        }
        <h3 class="h5">Ví dụ</h3>
        <partial name="_Examples" model="p.Examples" />
    </div>
</section>

@if (e is not null)
{
    <section class="card mb-4 enrichment">
        <div class="card-body">
            <h2 class="h6 text-uppercase text-info-emphasis">Bổ sung <span class="badge text-bg-info">không có trong giáo trình</span></h2>

            <h3 class="h5">Giải thích chi tiết</h3>
            @foreach (var para in e.Explanation.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                <p class="jp">@para</p>
            }

            @if (e.Formation.Count > 0)
            {
                <h3 class="h5">Công thức theo loại từ</h3>
                <div class="table-responsive">
                    <table class="table table-sm table-bordered align-middle">
                        <thead class="table-light"><tr><th>Loại từ</th><th>Cách ghép</th><th>Ví dụ</th></tr></thead>
                        <tbody>
                            @foreach (var f in e.Formation)
                            {
                                <tr><td>@f.WordType</td><td class="jp">@f.Rule</td><td class="jp">@string.Join("、", f.Samples)</td></tr>
                            }
                        </tbody>
                    </table>
                </div>
            }

            @if (e.Usage.Count > 0)
            {
                <h3 class="h5">Cách dùng</h3>
                <ul>@foreach (var u in e.Usage) { <li class="jp">@u</li> }</ul>
            }

            @if (e.Pitfalls.Count > 0)
            {
                <h3 class="h5">Lưu ý &amp; lỗi hay gặp</h3>
                <ul>@foreach (var u in e.Pitfalls) { <li class="jp">@u</li> }</ul>
            }

            @if (e.Comparisons.Count > 0)
            {
                <h3 class="h5">So sánh</h3>
                <ul>
                    @foreach (var c in e.Comparisons)
                    {
                        <li class="jp">
                            @if (c.TargetId is not null)
                            {
                                <a asp-page="/Grammar" asp-route-id="@c.TargetId">@c.Pattern</a>
                            }
                            else
                            {
                                <strong>@c.Pattern</strong>
                            }
                            : @c.Difference
                        </li>
                    }
                </ul>
            }

            @if (e.Examples.Count > 0)
            {
                <h3 class="h5">Ví dụ thêm</h3>
                <partial name="_Examples" model="e.Examples" />
            }
        </div>
    </section>
}

<nav class="d-flex justify-content-between">
    @if (Model.Previous is { } prev)
    {
        <a class="btn btn-outline-primary" asp-page="/Grammar" asp-route-id="@prev.Id">← <span class="jp">@prev.Pattern</span></a>
    }
    else
    {
        <span></span>
    }
    @if (Model.Next is { } next)
    {
        <a class="btn btn-outline-primary" asp-page="/Grammar" asp-route-id="@next.Id"><span class="jp">@next.Pattern</span> →</a>
    }
</nav>
```

- [ ] **Step 16: Thay `src/Japan.Web/wwwroot/css/site.css` và `src/Japan.Web/wwwroot/js/site.js`**

`site.css`:

```css
.jp {
  font-family: "Yu Gothic UI", "Yu Gothic", Meiryo, sans-serif;
}

.examples > li {
  margin-bottom: 0.75rem;
}

.examples .jp {
  font-size: 1.15rem;
  line-height: 2.2;
}

rt {
  font-size: 0.55em;
}

body.hide-furigana rt {
  visibility: hidden;
}

.enrichment {
  border-left: 4px solid var(--bs-info);
}

mark {
  padding: 0;
}
```

`site.js`:

```js
(() => {
  const key = 'furigana';
  const button = document.getElementById('furigana-toggle');
  let on = localStorage.getItem(key) !== 'off';

  const apply = () => {
    document.body.classList.toggle('hide-furigana', !on);
    if (button) {
      button.classList.toggle('active', on);
      button.setAttribute('aria-pressed', String(on));
    }
  };

  apply();
  button?.addEventListener('click', () => {
    on = !on;
    localStorage.setItem(key, on ? 'on' : 'off');
    apply();
  });
})();
```

- [ ] **Step 17: Chạy test, xác nhận pass**

Run: `dotnet test --filter "FullyQualifiedName~Japan.Tests.Web"`
Expected: PASS (tất cả test trong `PagesTests`, `RenderingTests`).

- [ ] **Step 18: Kiểm tra thủ công**

Run: `dotnet run --project src/Japan.Web` rồi mở URL `http://localhost:…` in ra console.
Expected: mục lục 30 bài; mở một điểm có ví dụ kanji thấy furigana; bấm nút "Furigana" ẩn/hiện được, tải lại trang vẫn giữ lựa chọn.

- [ ] **Step 19: Commit**

```powershell
git add -A
git commit -m "feat(web): add lesson index, lesson and grammar detail pages with furigana"
```

---

### Task 11: Web — tìm kiếm

**Files:**
- Create: `src/Japan.Web/Rendering/Highlight.cs`, `src/Japan.Web/Pages/Search.cshtml(.cs)`, `src/Japan.Web/Pages/Shared/_SearchResults.cshtml`
- Modify: `src/Japan.Web/wwwroot/js/site.js` (thêm khối tìm-khi-gõ)
- Test: `tests/Japan.Tests/Web/SearchPageTests.cs`, bổ sung `tests/Japan.Tests/Web/RenderingTests.cs`

**Interfaces:**
- Consumes: `GrammarRepository.Search` (Task 10), `SearchHit` (Task 9), `RenderingTests.Html` (Task 10)
- Produces:
  - `static class Highlight { IHtmlContent Mark(string text, string? query) }` — tô `<mark>` các chỗ khớp nguyên văn (không phân biệt hoa thường); truy vấn không dấu thì vẫn ra kết quả nhưng không tô
  - `record SearchResultsViewModel(string? Query, IReadOnlyList<SearchHit> Hits)`
  - Route `/timkiem?q=…` (trang đầy đủ), `/timkiem?handler=Partial&q=…` (chỉ HTML kết quả)

- [ ] **Step 1: Viết test thất bại `tests/Japan.Tests/Web/SearchPageTests.cs`**

```csharp
using System.Net;

namespace Japan.Tests.Web;

public sealed class SearchPageTests : IDisposable
{
    private readonly GrammarWebFactory _factory = GrammarWebFactory.WithSampleData();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task FullPage_FindsByVietnameseWithoutDiacritics()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?q=tranh%20thu");

        Assert.Contains("<html", html);
        Assert.Contains("～うちに（１）", html);
        Assert.Contains("1 kết quả", html);
    }

    [Fact]
    public async Task Partial_ReturnsOnlyResults_WithHighlight()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?handler=Partial&q=%E9%96%93%E3%81%AB");

        Assert.DoesNotContain("<html", html);
        Assert.Contains("<mark>間に</mark>", html);
        Assert.Contains("href=\"/nguphap/b7-3\"", html);
    }

    [Fact]
    public async Task NoMatch_ShowsMessage()
    {
        var html = await _factory.CreateClient().GetStringAsync("/timkiem?q=xyzxyz");

        Assert.Contains("Không tìm thấy kết quả", html);
    }

    [Theory]
    [InlineData("/timkiem")]
    [InlineData("/timkiem?q=%20%20")]
    [InlineData("/timkiem?q=%EF%BD%9E")]
    [InlineData("/timkiem?handler=Partial&q=")]
    public async Task EmptyQueries_Return200WithoutResults(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("kết quả", await response.Content.ReadAsStringAsync());
    }
}
```

- [ ] **Step 2: Bổ sung test thất bại vào `tests/Japan.Tests/Web/RenderingTests.cs`**

```csharp
    [Fact]
    public void Mark_HighlightsAllLiteralMatchesIgnoringCaseAndWaveDash() =>
        Assert.Equal("<mark>Tranh</mark> thủ, <mark>tranh</mark> &lt;x&gt;",
            Html(Highlight.Mark("Tranh thủ, tranh <x>", "～tranh")));

    [Fact]
    public void Mark_NoQuery_ReturnsEncodedText() =>
        Assert.Equal("&lt;x&gt;", Html(Highlight.Mark("<x>", "  ")));
```

- [ ] **Step 3: Chạy test, xác nhận thất bại**

Run: `dotnet test --filter "FullyQualifiedName~SearchPageTests|FullyQualifiedName~RenderingTests"`
Expected: build FAIL — `Highlight` chưa tồn tại; route `/timkiem` 404.

- [ ] **Step 4: Viết `src/Japan.Web/Rendering/Highlight.cs`**

```csharp
using Microsoft.AspNetCore.Html;

namespace Japan.Web.Rendering;

public static class Highlight
{
    public static IHtmlContent Mark(string text, string? query)
    {
        var builder = new HtmlContentBuilder();
        var q = query?.Trim().TrimStart('～', '~', '〜').Trim();
        if (string.IsNullOrEmpty(q)) return builder.Append(text);

        var pos = 0;
        while (true)
        {
            var at = text.IndexOf(q, pos, StringComparison.OrdinalIgnoreCase);
            if (at < 0) break;
            builder.Append(text[pos..at]).AppendHtml("<mark>").Append(text.Substring(at, q.Length)).AppendHtml("</mark>");
            pos = at + q.Length;
        }

        return builder.Append(text[pos..]);
    }
}
```

- [ ] **Step 5: Viết `src/Japan.Web/Pages/Search.cshtml.cs`**

```csharp
using Japan.Core;
using Japan.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Japan.Web.Pages;

public sealed record SearchResultsViewModel(string? Query, IReadOnlyList<SearchHit> Hits);

public sealed class SearchModel(GrammarRepository repo) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public SearchResultsViewModel Results { get; private set; } = new(null, []);

    public void OnGet() => Results = new SearchResultsViewModel(Query, repo.Search(Query));

    public PartialViewResult OnGetPartial()
    {
        OnGet();
        return Partial("_SearchResults", Results);
    }
}
```

- [ ] **Step 6: Viết `src/Japan.Web/Pages/Search.cshtml`**

```cshtml
@page "/timkiem"
@model SearchModel
@{
    ViewData["Title"] = "Tìm kiếm";
}
<form method="get" class="mb-4" role="search">
    <input name="q" value="@Model.Query" type="search" class="form-control form-control-lg"
           placeholder="Tìm mẫu ngữ pháp, nghĩa tiếng Việt hoặc câu ví dụ…" autofocus />
</form>
<partial name="_SearchResults" model="Model.Results" />
```

- [ ] **Step 7: Viết `src/Japan.Web/Pages/Shared/_SearchResults.cshtml`**

```cshtml
@model SearchResultsViewModel
@if (Model.Hits.Count > 0)
{
    <p class="small text-body-secondary">@Model.Hits.Count kết quả</p>
    <div class="list-group mb-4">
        @foreach (var hit in Model.Hits)
        {
            <a class="list-group-item list-group-item-action" asp-page="/Grammar" asp-route-id="@hit.Point.Id">
                <div>
                    <span class="fw-bold jp">@Highlight.Mark(hit.Point.Pattern, Model.Query)</span>
                    <span class="badge text-bg-light">Bài @hit.Point.Lesson</span>
                    <span class="badge text-bg-secondary">@hit.Field</span>
                </div>
                <div class="small jp">@Highlight.Mark(hit.Snippet, Model.Query)</div>
            </a>
        }
    </div>
}
else if (!string.IsNullOrWhiteSpace(Model.Query) && Model.Query.Trim().Trim('～', '~', '〜').Length > 0)
{
    <p class="text-body-secondary">Không tìm thấy kết quả cho "@Model.Query".</p>
}
```

- [ ] **Step 8: Thêm khối tìm-khi-gõ vào cuối `src/Japan.Web/wwwroot/js/site.js`**

```js
(() => {
  const box = document.getElementById('search-box');
  if (!box || !box.dataset.searchUrl) return;
  const results = document.getElementById('search-results');
  const list = document.getElementById('lesson-list');
  let timer;
  let controller;

  box.addEventListener('input', () => {
    clearTimeout(timer);
    timer = setTimeout(async () => {
      const q = box.value.trim();
      if (!q) {
        results.innerHTML = '';
        list.hidden = false;
        return;
      }
      controller?.abort();
      controller = new AbortController();
      try {
        const res = await fetch(`${box.dataset.searchUrl}&q=${encodeURIComponent(q)}`, { signal: controller.signal });
        results.innerHTML = await res.text();
        list.hidden = true;
      } catch (e) {
        if (e.name !== 'AbortError') throw e;
      }
    }, 250);
  });
})();
```

- [ ] **Step 9: Chạy toàn bộ test**

Run: `dotnet test`
Expected: PASS toàn bộ, build không warning.

- [ ] **Step 10: Kiểm tra thủ công**

Run: `dotnet run --project src/Japan.Web`
Expected: gõ `うちに` ở trang chủ → danh sách bài ẩn, kết quả hiện ngay, `うちに` được tô; gõ `tranh thu` → có ～うちに; xoá ô → danh sách bài hiện lại; Enter → sang `/timkiem?q=…`.

- [ ] **Step 11: Commit**

```powershell
git add -A
git commit -m "feat(web): add search page with live results and highlighting"
```

---

### Task 12: Viết nội dung mở rộng cho 106 điểm

**Files:**
- Create: `content/enrichment/b{n}-{i}.json` (106 file)
- Output: `data/grammar.json`

**Interfaces:**
- Consumes: `data/grammar.json` từ Task 8 (danh sách id, mẫu, ý nghĩa, ví dụ gốc); importer (Task 8) để kiểm tra
- Produces: 106 file enrichment hợp lệ theo schema `GrammarEnrichment` (camelCase)

Tiêu chí mỗi file (spec mục 7):
- `explanation`: 1–3 đoạn, phân cách bằng dòng trống (`\n\n`); nêu nghĩa sâu hơn, sắc thái, hoàn cảnh dùng.
- `formation`: một dòng cho mỗi loại từ áp dụng được (động từ nhóm 1/2/3, tính từ い, tính từ な, danh từ), mỗi dòng 1–3 `samples` dạng `từ gốc → dạng ghép`.
- `usage`: 2–5 ý (văn nói/văn viết, mức trang trọng, chủ ngữ, thì).
- `pitfalls`: 1–4 lỗi hay gặp / trường hợp không dùng được, có thể dùng ✗/✓.
- `comparisons`: mẫu gần nghĩa; nếu mẫu đó có trong sách thì `targetId` là id của nó (tra trong `data/grammar.json`), nếu không thì `null`.
- `examples`: 3–5 câu mới, **không trùng** ví dụ gốc, độ khó N3, mỗi câu có `japanese` + `vietnamese`; không tự ghi `ruby`.
- Văn phong tiếng Việt thống nhất với giáo trình (Vる, Vない, Aい, Aな, Nの…).

Mẫu tham chiếu — `content/enrichment/b6-3.json`:

```json
{
  "explanation": "～ずに là dạng văn viết của ～ないで: diễn tả hành động chính được thực hiện trong trạng thái KHÔNG làm một việc khác (\"mà không…\", \"thay vì…\"). ず là dạng phủ định cổ tương đương ない, nên ～ずに nghe trang trọng, cứng hơn và hay gặp trong văn bản, thông báo, tin tức.\n\nTrong hội thoại hằng ngày người Nhật dùng ～ないで nhiều hơn; dùng ～ずに khi nói tạo cảm giác nghiêm túc, chững chạc.",
  "formation": [
    { "wordType": "Động từ nhóm 1", "rule": "Vない bỏ ない + ずに", "samples": ["書く → 書かずに", "使う → 使わずに"] },
    { "wordType": "Động từ nhóm 2", "rule": "Vない bỏ ない + ずに", "samples": ["食べる → 食べずに", "見る → 見ずに"] },
    { "wordType": "Động từ nhóm 3", "rule": "する → せずに; 来る → 来ずに (こずに)", "samples": ["勉強する → 勉強せずに", "来る → 来ずに"] }
  ],
  "usage": [
    "Hai hành động có cùng chủ ngữ: hành động sau được thực hiện mà không kèm hành động trước.",
    "Hay dùng trong văn viết, thông báo, hướng dẫn: 「遠慮せずに言ってください」.",
    "Bỏ に (～ず) khi nối câu thì nghĩa thường chuyển sang \"vì không… nên…\" (xem ～ず)."
  ],
  "pitfalls": [
    "✗ しずに → ✓ せずに: する luôn chuyển thành せず.",
    "Không dùng ～ずに để yêu cầu: ✗「食べずにください」→ ✓「食べないでください」."
  ],
  "comparisons": [
    { "pattern": "～ないで", "targetId": null, "difference": "Cùng nghĩa \"mà không…\", nhưng ～ないで dùng tự nhiên trong văn nói và có dạng yêu cầu ～ないでください; ～ずに trang trọng hơn và không dùng để yêu cầu." },
    { "pattern": "～ず", "targetId": "b6-4", "difference": "～ず (không có に) thường nối hai vế với nghĩa nguyên nhân \"vì không… nên…\" hoặc liệt kê phủ định." }
  ],
  "examples": [
    { "japanese": "朝ご飯を食べずに学校へ行った。", "vietnamese": "Tôi đã đến trường mà không ăn sáng." },
    { "japanese": "彼は何も言わずに部屋を出ていった。", "vietnamese": "Anh ấy rời khỏi phòng mà không nói gì." },
    { "japanese": "傘を持たずに出かけたら、雨に降られた。", "vietnamese": "Ra ngoài mà không mang ô nên tôi bị mắc mưa." }
  ]
}
```

- [ ] **Step 1: Bài 1–5** — đọc các điểm `b1-*` … `b5-*` trong `data/grammar.json`, viết file enrichment cho từng điểm theo tiêu chí trên, chạy `dotnet run --project src/Japan.Importer`. Expected: exit 0, dòng "Chưa có phần bổ sung" không còn id nào của Bài 1–5. Tự rà: câu ví dụ tự nhiên, đúng ngữ pháp đang học, dịch khớp; `targetId` đúng mẫu. Commit: `git add content/enrichment data/grammar.json; git commit -m "content: enrichment for lessons 1-5"`.
- [ ] **Step 2: Bài 6–10** — như Step 1 cho `b6-*` … `b10-*`. Commit `content: enrichment for lessons 6-10`.
- [ ] **Step 3: Bài 11–15** — như Step 1. Commit `content: enrichment for lessons 11-15`.
- [ ] **Step 4: Bài 16–20** — như Step 1. Commit `content: enrichment for lessons 16-20`.
- [ ] **Step 5: Bài 21–25** — như Step 1. Commit `content: enrichment for lessons 21-25`.
- [ ] **Step 6: Bài 26–30** — như Step 1. Expected cuối: `Đã ghi …: 30 bài, 106 điểm, 106 điểm có phần bổ sung.` và không còn dòng "Chưa có phần bổ sung". Commit `content: enrichment for lessons 26-30`.
- [ ] **Step 7: Rà furigana phần bổ sung** — mở vài trang chi tiết trên web, kiểm tra furigana câu ví dụ mới; từ đọc sai → thêm vào `content/furigana-overrides.json`, chạy lại importer, commit `data: fix furigana overrides`.
