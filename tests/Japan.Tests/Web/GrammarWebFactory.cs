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
