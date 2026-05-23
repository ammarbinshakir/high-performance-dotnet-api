using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace HighPerformanceDotNetApi.Api.Controllers;

[ApiController]
public sealed class BenchmarksController(IConfiguration configuration, IWebHostEnvironment environment) : ControllerBase
{
    private const string SearchFileName = "search-summary.json";
    private const string CacheFileName = "cache-summary.json";

    [HttpGet("api/benchmarks/latest")]
    [ProducesResponseType(typeof(BenchmarkDashboardDto), StatusCodes.Status200OK)]
    public ActionResult<BenchmarkDashboardDto> Latest()
    {
        var resultsPath = ResolveResultsPath();
        var search = ReadSummary(Path.Combine(resultsPath, SearchFileName), "Search comparison");
        var cache = ReadSummary(Path.Combine(resultsPath, CacheFileName), "Cache comparison");

        return new BenchmarkDashboardDto(
            ResultsPath: resultsPath,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Search: search,
            Cache: cache,
            Commands:
            [
                "RATE_LIMITING_ENABLED=false docker compose up -d --build",
                "k6 run --summary-export load-tests/results/search-summary.json load-tests/k6-search-comparison.js",
                "k6 run --summary-export load-tests/results/cache-summary.json load-tests/k6-cache-comparison.js"
            ]);
    }

    [HttpGet("benchmarks")]
    [Produces("text/html")]
    public ContentResult Dashboard()
    {
        return Content(BenchmarkDashboardHtml.Page, "text/html", Encoding.UTF8);
    }

    private BenchmarkSummaryDto ReadSummary(string path, string title)
    {
        if (!System.IO.File.Exists(path))
        {
            return BenchmarkSummaryDto.Missing(title, Path.GetFileName(path));
        }

        using var stream = System.IO.File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var metrics = document.RootElement.GetProperty("metrics");

        return new BenchmarkSummaryDto(
            Title: title,
            FileName: Path.GetFileName(path),
            LastUpdatedUtc: System.IO.File.GetLastWriteTimeUtc(path),
            IsAvailable: true,
            HttpFailureRate: ReadMetricValue(metrics, "http_req_failed", "rate"),
            Series: new[]
            {
                ReadTrend(metrics, "http_req_duration{endpoint:optimized}", "Optimized search"),
                ReadTrend(metrics, "http_req_duration{endpoint:slow}", "Slow search"),
                ReadTrend(metrics, "http_req_duration{endpoint:cached}", "Cached top products"),
                ReadTrend(metrics, "http_req_duration{endpoint:noncached}", "Non-cached top products")
            }.Where(series => series.IsAvailable).ToArray());
    }

    private string ResolveResultsPath()
    {
        var configured = configuration["BenchmarkResults:Path"] ?? "load-tests/results";
        if (Path.IsPathRooted(configured))
        {
            return configured;
        }

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configured)),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "..", configured))
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    private static BenchmarkSeriesDto ReadTrend(JsonElement metrics, string metricName, string label)
    {
        if (!metrics.TryGetProperty(metricName, out var metric))
        {
            return BenchmarkSeriesDto.Missing(label);
        }

        var values = metric.TryGetProperty("values", out var nestedValues) ? nestedValues : metric;

        return new BenchmarkSeriesDto(
            Label: label,
            IsAvailable: true,
            AverageMilliseconds: ReadNumber(values, "avg"),
            MedianMilliseconds: ReadNumber(values, "med"),
            P90Milliseconds: ReadNumber(values, "p(90)"),
            P95Milliseconds: ReadNumber(values, "p(95)"),
            MaxMilliseconds: ReadNumber(values, "max"));
    }

    private static double? ReadMetricValue(JsonElement metrics, string metricName, string valueName)
    {
        if (!metrics.TryGetProperty(metricName, out var metric))
        {
            return null;
        }

        var values = metric.TryGetProperty("values", out var nestedValues) ? nestedValues : metric;
        return ReadNumber(values, valueName);
    }

    private static double? ReadNumber(JsonElement values, string name)
    {
        return values.TryGetProperty(name, out var value) && value.TryGetDouble(out var number)
            ? number
            : null;
    }
}

public sealed record BenchmarkDashboardDto(
    string ResultsPath,
    DateTimeOffset GeneratedAtUtc,
    BenchmarkSummaryDto Search,
    BenchmarkSummaryDto Cache,
    IReadOnlyList<string> Commands);

public sealed record BenchmarkSummaryDto(
    string Title,
    string FileName,
    DateTime? LastUpdatedUtc,
    bool IsAvailable,
    double? HttpFailureRate,
    IReadOnlyList<BenchmarkSeriesDto> Series)
{
    public static BenchmarkSummaryDto Missing(string title, string fileName)
    {
        return new BenchmarkSummaryDto(title, fileName, null, false, null, []);
    }
}

public sealed record BenchmarkSeriesDto(
    string Label,
    bool IsAvailable,
    double? AverageMilliseconds,
    double? MedianMilliseconds,
    double? P90Milliseconds,
    double? P95Milliseconds,
    double? MaxMilliseconds)
{
    public static BenchmarkSeriesDto Missing(string label)
    {
        return new BenchmarkSeriesDto(label, false, null, null, null, null, null);
    }
}

internal static class BenchmarkDashboardHtml
{
    public const string Page = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Benchmark Dashboard</title>
  <style>
    :root { color-scheme: light; font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
    body { margin: 0; background: #f7f8fa; color: #17202a; }
    main { max-width: 1120px; margin: 0 auto; padding: 32px 20px 48px; }
    header { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; margin-bottom: 24px; }
    h1 { margin: 0; font-size: 30px; line-height: 1.1; }
    h2 { margin: 0 0 12px; font-size: 18px; }
    .muted { color: #607083; font-size: 14px; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px; }
    .panel { background: #fff; border: 1px solid #dfe5ec; border-radius: 8px; padding: 18px; box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04); }
    table { width: 100%; border-collapse: collapse; font-size: 14px; }
    th, td { padding: 10px 8px; border-bottom: 1px solid #e8edf3; text-align: right; white-space: nowrap; }
    th:first-child, td:first-child { text-align: left; }
    th { color: #607083; font-weight: 650; }
    code { background: #eef2f6; border-radius: 5px; padding: 2px 5px; }
    pre { overflow: auto; background: #101820; color: #f5f7fa; padding: 14px; border-radius: 8px; font-size: 13px; }
    .status { display: inline-flex; align-items: center; gap: 8px; font-weight: 650; }
    .dot { width: 9px; height: 9px; border-radius: 999px; background: #16a34a; }
    .missing .dot { background: #f59e0b; }
    .missing { color: #8a5a00; }
    @media (max-width: 820px) { .grid { grid-template-columns: 1fr; } header { align-items: flex-start; flex-direction: column; } }
  </style>
</head>
<body>
  <main>
    <header>
      <div>
        <h1>Benchmark Dashboard</h1>
        <div class="muted">Latest k6 summaries rendered from <code>load-tests/results</code>.</div>
      </div>
      <div id="updated" class="muted"></div>
    </header>
    <section id="content" class="grid"></section>
    <section class="panel" style="margin-top:18px">
      <h2>Generate Results</h2>
      <pre id="commands"></pre>
    </section>
  </main>
  <script>
    const fmt = value => value === null || value === undefined ? "-" : `${value.toFixed(value >= 100 ? 0 : 2)} ms`;
    const percent = value => value === null || value === undefined ? "-" : `${(value * 100).toFixed(2)}%`;

    function renderSummary(summary) {
      if (!summary.isAvailable) {
        return `<section class="panel"><h2>${summary.title}</h2><p class="status missing"><span class="dot"></span>${summary.fileName} not found</p><p class="muted">Run the commands below, then refresh this page.</p></section>`;
      }

      const rows = summary.series.map(item => `
        <tr>
          <td>${item.label}</td>
          <td>${fmt(item.medianMilliseconds)}</td>
          <td>${fmt(item.p90Milliseconds)}</td>
          <td>${fmt(item.p95Milliseconds)}</td>
          <td>${fmt(item.maxMilliseconds)}</td>
        </tr>`).join("");

      return `<section class="panel">
        <h2>${summary.title}</h2>
        <p class="status"><span class="dot"></span>${summary.fileName}</p>
        <p class="muted">Failure rate: ${percent(summary.httpFailureRate)} | Updated: ${new Date(summary.lastUpdatedUtc).toLocaleString()}</p>
        <table>
          <thead><tr><th>Endpoint</th><th>Median</th><th>p90</th><th>p95</th><th>Max</th></tr></thead>
          <tbody>${rows}</tbody>
        </table>
      </section>`;
    }

    fetch("/api/benchmarks/latest")
      .then(response => response.json())
      .then(data => {
        document.getElementById("updated").textContent = `Loaded ${new Date(data.generatedAtUtc).toLocaleString()}`;
        document.getElementById("content").innerHTML = renderSummary(data.search) + renderSummary(data.cache);
        document.getElementById("commands").textContent = data.commands.join("\n");
      });
  </script>
</body>
</html>
""";
}
