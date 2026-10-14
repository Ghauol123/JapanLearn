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

    public static string CssClass(this JlptLevel level) => $"level-{level.ToString().ToLowerInvariant()}";
}
