using Riverside.CompilerPlatform.SourceGenerators;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using Riverside.CompilerPlatform.SourceGenerators.Extensions;
using Riverside.Extensions.Accountability;
using Riverside.CompilerPlatform.Features.Swagger.Helpers;

namespace Riverside.CompilerPlatform.Features.Swagger;

/// <summary>
/// Generates source code from OpenAPI specification files as part of the build process.
/// </summary>
[Generator]
public class SwaggerGenerator : IncrementalGenerator
{
	private const string VersionProperty = "build_property.SwaggerGenerator_Version";
	private const string OptionsProperty = "build_property.SwaggerGenerator_Options";
	private const string LanguageProperty = "build_property.SwaggerGenerator_Language";
	private const string AdditionalPropertiesProperty = "build_property.SwaggerGenerator_AdditionalProperties";

	/// <inheritdoc/>
	protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
	{
		CreateDiagnostic("A", "A", "A", DiagnosticSeverity.Warning);
		var optionsProvider = context.AnalyzerConfigOptions.GlobalOptions;

		// OpenAPI specs
		var specs = context.AdditionalTexts
			.Where(at => at.Path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
					  || at.Path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
					  || at.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			.ToImmutableArray();

		optionsProvider.TryGetValue(VersionProperty, out var version);
		optionsProvider.TryGetValue(OptionsProperty, out var cliOptions);
		optionsProvider.TryGetValue(LanguageProperty, out var language);
		optionsProvider.TryGetValue(AdditionalPropertiesProperty, out var additionalProps);

		version ??= "7.20.0";
		language ??= "csharp";
		additionalProps ??= $"packageName=Riverside.CompilerPlatform.ABI,apiName=GenericHost,targetFramework=netstandard2.0,nullableReferenceTypes=true,useOneOfInterface=true"; // test values

		var jarPath = EnsureJarDownloaded(version, context);

		if (string.IsNullOrWhiteSpace(jarPath))
		{
			IncrementalGenerator.CreateDiagnostic(
				"RS0000",
				"JAR not downloaded",
				"An error occured whilst downloading the JAR executable to generate the OpenAPI spec")
				.Report(context);
		}

		foreach (var spec in specs)
		{
			try
			{
				var specPath = spec.Path;
				if (!File.Exists(specPath))
					continue;

				var tempOut = Path.Combine(Path.GetTempPath(), "Roslyn", "Advanced Compiler Services for .NET", Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(tempOut);

				var argsBuilder = new StringBuilder();
				argsBuilder.Append(" -jar ");
				argsBuilder.Append(SanitizationHelpers.EscapeArg(jarPath!));
				argsBuilder.Append(" generate");
				argsBuilder.Append(" -i ");
				argsBuilder.Append(SanitizationHelpers.EscapeArg(specPath));
				argsBuilder.Append(" -g ");
				argsBuilder.Append(SanitizationHelpers.EscapeArg(language));
				argsBuilder.Append(" -o ");
				argsBuilder.Append(SanitizationHelpers.EscapeArg(tempOut));

				if (!string.IsNullOrWhiteSpace(cliOptions))
				{
					argsBuilder.Append(" ");
					argsBuilder.Append(cliOptions);
				}

				if (!string.IsNullOrWhiteSpace(additionalProps))
				{
					argsBuilder.Append(" --additional-properties=");
					argsBuilder.Append(SanitizationHelpers.EscapeArg(additionalProps!));
				}

				var args = argsBuilder.ToString();

				var runResult = ProcessHelpers.RunProcess("java", args, TimeSpan.FromMinutes(2)).GetAwaiter().GetResult();

				if (runResult.ExitCode != 0)
				{
					CreateDiagnostic("RS0000", "OpenAPI generator failed", $"OpenAPI generator failed for spec '{spec.Path}' with exit code {runResult.ExitCode}: {runResult.StandardError.ReplaceLineEndings(" ")}").Report(context);
					TryDeleteDirectory(tempOut);
					continue;
				}

				var srcDir = Path.Combine(tempOut, "src", "Riverside.CompilerPlatform.ABI");
				var csFiles = Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories).ToArray();
				if (csFiles.Length == 0)
				{
					CreateDiagnostic("RS0000", "No C# files generated", $"OpenAPI generator produced no C# files for spec '{spec.Path}'").Report(context);
					TryDeleteDirectory(tempOut);
					continue;
				}

				foreach (var cs in csFiles)
				{
					try
					{
						var content = File.ReadAllText(cs, Encoding.UTF8);
						var rel = Path.GetRelativePath(tempOut, cs)
							.Replace(Path.DirectorySeparatorChar, '_')
							.Replace(Path.AltDirectorySeparatorChar, '_');

						var specFileName = Path.GetFileNameWithoutExtension(specPath);
						var hintName = $"Swagger_{SanitizationHelpers.Sanitize(specFileName)}_{SanitizationHelpers.Sanitize(rel)}";
						AddSource(hintName, content);
					}
					catch (Exception ex)
					{
						CreateDiagnostic("RS0000", "Failed to add generated file", $"Failed to add generated file '{cs}': {ex.Message}").Report(context);
					}
				}

				TryDeleteDirectory(tempOut);
			}
			catch (Exception ex)
			{
				CreateDiagnostic("RS9999", "OpenAPI generator exception", ex.ToString());
			}
		}
	}

	private static string? EnsureJarDownloaded(string version, GeneratorContext context)
	{
		try
		{
			var baseDir = Path.Combine(Path.GetTempPath(), "Roslyn", "Advanced Compiler Services for .NET", "SwaggerGenerator");
			Directory.CreateDirectory(baseDir);

			var jarPath = Path.Combine(baseDir, $"openapi-generator-cli-{version}.jar");
			if (File.Exists(jarPath))
				return jarPath;

			var url = $"https://repo1.maven.org/maven2/org/openapitools/openapi-generator-cli/{version}/openapi-generator-cli-{version}.jar";

			using var client = new HttpClient();
			var bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult();

			File.WriteAllBytes(jarPath, bytes);

			return jarPath;
		}
		catch (Exception ex)
		{
			CreateDiagnostic("RS0000", $"Failed to download OpenAPI generator JAR", $"Could not download version {version}: {ex.Message}").Report(context);
			return null;
		}
	}

	private static void TryDeleteDirectory(string path)
	{
		try { if (Directory.Exists(path)) Directory.Delete(path, true); }
		catch { }
	}
}
