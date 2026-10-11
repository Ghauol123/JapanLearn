namespace Japan.Importer.Parsing;

public sealed class SourceFileLockedException(string path, Exception inner)
    : IOException($"Không mở được \"{Path.GetFileName(path)}\" vì file đang bị chương trình khác khoá. Hãy đóng Word rồi chạy lại.", inner)
{
    public string FilePath { get; } = path;
}
