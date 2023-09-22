using System.Text.Json.Nodes;

namespace SurrealDb.Benchmarks.Helpers;

public static class BenchmarkHelper
{
	/// <summary>
	/// Combine multiple benchmark result files into a single file to be used with GitHub Actions.
	/// Original source code from @si618 https://github.com/benchmark-action/github-action-benchmark/issues/69
	/// </summary>
	/// <param name="resultsDirectory">The directory that contains benchmark result files.</param>
	/// <param name="resultsFile">The name of the combined benchmark result file.</param>
	/// <param name="searchPattern">The pattern used to search benchmark result files, in order to avoid unwanted files.</param>
	/// <exception cref="DirectoryNotFoundException"></exception>
	/// <exception cref="FileNotFoundException"></exception>
	public static void CombineBenchmarkResults(
		string resultsDirectory = "./BenchmarkDotNet.Artifacts/results",
		string resultsFile = "Combined.Benchmarks",
		string searchPattern = "*.json"
	)
	{
		var resultsPath = Path.Combine(resultsDirectory, resultsFile + ".json");

		if (!Directory.Exists(resultsDirectory))
			throw new DirectoryNotFoundException($"Directory not found '{resultsDirectory}'");

		if (File.Exists(resultsPath))
			File.Delete(resultsPath);

		var reports = Directory
			.GetFiles(resultsDirectory, searchPattern, SearchOption.TopDirectoryOnly)
			.ToArray();

		if (!reports.Any())
			throw new FileNotFoundException($"Reports not found '{searchPattern}'");

		var combinedReport = JsonNode.Parse(File.ReadAllText(reports.First()))!;
		var title = combinedReport["Title"]!;
		var benchmarks = combinedReport["Benchmarks"]!.AsArray();

		// Rename title whilst keeping original timestamp
		combinedReport["Title"] = $"{resultsFile}{title.GetValue<string>()[^16..]}";

		foreach (var report in reports.Skip(1))
		{
			var array = JsonNode.Parse(File.ReadAllText(report))!["Benchmarks"]!.AsArray();
			foreach (var benchmark in array)
			{
				// Double parse avoids "The node already has a parent" exception
				benchmarks.Add(JsonNode.Parse(benchmark!.ToJsonString())!);
			}
		}

		File.WriteAllText(resultsPath, combinedReport.ToString());
	}
}
