using Riverside.CompilerPlatform.SourceGenerators;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Riverside.CompilerPlatform.Extensions;
using Riverside.CompilerPlatform.Helpers;

namespace Riverside.CompilerPlatform.Features.Swagger;

/// <summary>
/// Generates source code from OpenAPI specification files as part of the build process.
/// </summary>
[Generator]
public partial class KiotaGenerator : IncrementalGenerator
{
	private const string VersionProperty = "build_property.Kiota_Version";
	private const string LanguageProperty = "build_property.KiotaGenerator_Language";
	private const string ClassNameProperty = "build_property.KiotaGenerator_ClassName";
	private const string NamespaceNameProperty = "build_property.KiotaGenerator_NamespaceName";
	private const string TypeAccessModifierProperty = "build_property.KiotaGenerator_TypeAccessModifier";
	private const string LogLevelProperty = "build_property.KiotaGenerator_LogLevel";
	private const string BackingStoreProperty = "build_property.KiotaGenerator_BackingStore";
	private const string ExcludeBackwardCompatibleProperty = "build_property.KiotaGenerator_ExcludeBackwardCompatible";
	private const string AdditionalDataProperty = "build_property.KiotaGenerator_AdditionalData";
	private const string SerializerProperty = "build_property.KiotaGenerator_Serializer";
	private const string DeserializerProperty = "build_property.KiotaGenerator_Deserializer";
	private const string CleanOutputProperty = "build_property.KiotaGenerator_CleanOutput";
	private const string StructuredMimeTypesProperty = "build_property.KiotaGenerator_StructuredMimeTypes";
	private const string IncludePathProperty = "build_property.KiotaGenerator_IncludePath";
	private const string ExcludePathProperty = "build_property.KiotaGenerator_ExcludePath";
	private const string DisableValidationRulesProperty = "build_property.KiotaGenerator_DisableValidationRules";
	private const string ClearCacheProperty = "build_property.KiotaGenerator_ClearCache";
	private const string DisableSSLValidationProperty = "build_property.KiotaGenerator_DisableSSLValidation";

	private static readonly string ToolDirectory = Path.Combine(
		Path.GetTempPath(), "Roslyn", "Advanced Compiler Services for .NET", "KiotaGenerator");

	/// <inheritdoc/>
	protected override void OnBeforeGeneration(GeneratorContext context, CancellationToken cancellationToken)
	{
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

		version ??= string.Empty;
		language ??= "csharp";

		var jarPath = EnsureToolInstallation(version, context);

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
				var specNamespace = SanitizationHelpers.Sanitize(Path.GetFileNameWithoutExtension(Path.GetFileName(spec.Path)));

				var specPath = spec.Path;
				if (!File.Exists(specPath))
					continue;

				var specFileName = Path.GetFileNameWithoutExtension(specPath);

				var tempOut = Path.Combine(Path.GetTempPath(), "Roslyn", "Advanced Compiler Services for .NET", Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(tempOut);

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

				var srcDir = Path.Combine(tempOut, "src", specNamespace);
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

						var hintName = $"{SanitizationHelpers.Sanitize(specFileName)}_{SanitizationHelpers.Sanitize(rel)}";
						AddSource(hintName, content);
					}
					catch (Exception ex)
					{
						CreateDiagnostic("RS0000", "Failed to add generated file", $"Failed to add generated file '{cs}': {ex.Message}").Report(context);
					}
				}

				TryDeleteDirectory(tempOut);

				AddSource($"{SanitizationHelpers.Sanitize(specFileName)}_AnyOf", AnyOf_Polyfill(specNamespace + ".Model"));
			}
			catch (Exception ex)
			{
				CreateDiagnostic("RS9999", "OpenAPI generator exception", ex.ToString());
			}
		}
	}

	private static string? EnsureToolInstallation(string version, GeneratorContext context)
	{
		try
		{
			var baseDir = Path.Combine(Path.GetTempPath(), "Roslyn", "Advanced Compiler Services for .NET", "KiotaGenerator");
			Directory.CreateDirectory(baseDir);

			var jarPath = Path.Combine(baseDir, $"openapi-generator-cli-{version}.jar");
			if (File.Exists(jarPath))
				return jarPath;

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
