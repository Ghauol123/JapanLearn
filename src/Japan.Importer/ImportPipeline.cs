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
        OutputFile.Write(settings.OutputPath, GrammarJson.Serialize(book));

        var points = book.Lessons.SelectMany(l => l.Points).ToList();
        output.WriteLine($"Đã ghi {settings.OutputPath}: {book.Lessons.Count} bài, {points.Count} điểm, {points.Count(p => p.Enrichment is not null)} điểm có phần bổ sung.");
        var missing = points.Where(p => p.Enrichment is null).Select(p => p.Id).ToList();
        if (missing.Count > 0) output.WriteLine($"Chưa có phần bổ sung: {string.Join(", ", missing)}");
        return 0;
    }
}
