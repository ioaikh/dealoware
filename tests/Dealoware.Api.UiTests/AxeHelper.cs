using System.Text.Json;
using Microsoft.Playwright;

namespace Dealoware.Api.UiTests;

internal static class AxeHelper
{
    public static async Task ScanAsync(IPage page, string caseId)
    {
        var axePath = Path.Combine(AppContext.BaseDirectory, "assets", "axe.min.js");
        Assert.True(File.Exists(axePath), "Vendored axe-core is required; SKIP is not a pass.");
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = axePath });
        var json = await page.EvaluateAsync<string>(@"async () => {
            const results = await window.axe.run({
                runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] }
            });
            return JSON.stringify(results);
        }");

        var dir = Path.Combine(AdminUiServerFixture.FindRepoRoot(), "test-results", "axe");
        Directory.CreateDirectory(dir);
        var artifact = Path.Combine(dir, caseId + ".json");
        await File.WriteAllTextAsync(artifact, json);

        using var doc = JsonDocument.Parse(json);
        var violations = doc.RootElement.GetProperty("violations");
        var ids = violations.EnumerateArray().Select(v => v.GetProperty("id").GetString()).ToArray();
        Assert.True(ids.Length == 0, $"axe failed for {caseId} ({string.Join(", ", ids)}). Artifact: {artifact}");
    }
}
