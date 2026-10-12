using System.Text.Encodings.Web;
using System.Text.Unicode;
using Japan.Web.Services;
using Microsoft.Extensions.WebEncoders;

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_HTTP_PORTS", port);
}

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
