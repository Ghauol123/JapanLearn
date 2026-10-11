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
