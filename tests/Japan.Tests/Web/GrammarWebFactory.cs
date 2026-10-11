using Japan.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Japan.Tests.Web;

public sealed class GrammarWebFactory(string dataPath) : WebApplicationFactory<Program>
{
    public static GrammarWebFactory WithSampleData() => WithBook(TestBooks.Sample());

    public static GrammarWebFactory WithBook(GrammarBook book)
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory("web-").FullName, "grammar.json");
        File.WriteAllText(path, GrammarJson.Serialize(book));
        return new GrammarWebFactory(path);
    }

    public string DataPath => dataPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("GrammarData:Path", dataPath);
}
