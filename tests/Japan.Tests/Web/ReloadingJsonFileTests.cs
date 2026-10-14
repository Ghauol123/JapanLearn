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
