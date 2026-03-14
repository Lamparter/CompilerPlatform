using Riverside.CompilerPlatform.SourceGenerators;
using Riverside.CompilerPlatform.Extensions;
using Riverside.CompilerPlatform.Helpers;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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
		var options = context.AnalyzerConfigOptions.GlobalOptions;

		var specs = context.AdditionalTexts
			.Where(at => at.Path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
					  || at.Path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
					  || at.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			.ToImmutableArray();

		if (specs.IsEmpty)
			return;

		var version = options.GetString(VersionProperty);

		// Kiota engine args
		var language = options.GetNullableEnum<KiotaEngine.GenerationLanguage>(LanguageProperty)
			?? KiotaEngine.GenerationLanguage.CSharp;
		var className = options.GetString(ClassNameProperty);
		var namespaceName = options.GetString(NamespaceNameProperty);
		var typeAccessModifier = options.GetNullableEnum<KiotaEngine.Accessibility>(TypeAccessModifierProperty);
		var logLevel = options.GetNullableEnum<KiotaEngine.ConsoleLogLevel>(LogLevelProperty);
		var backingStore = options.GetNullableBool(BackingStoreProperty);
		var excludeBackwardCompatible = options.GetNullableBool(ExcludeBackwardCompatibleProperty);
		var additionalData = options.GetNullableBool(AdditionalDataProperty);
		var serializers = options.GetPipeSeparatedArray(SerializerProperty);
		var deserializers = options.GetPipeSeparatedArray(DeserializerProperty);
		var cleanOutput = options.GetNullableBool(CleanOutputProperty);
		var structuredMimeTypes = options.GetPipeSeparatedArray(StructuredMimeTypesProperty);
		var includePaths = options.GetPipeSeparatedArray(IncludePathProperty);
		var excludePaths = options.GetPipeSeparatedArray(ExcludePathProperty);
		var disableValidationRules = options.GetPipeSeparatedEnumArray<KiotaEngine.ValidationRules>(DisableValidationRulesProperty);
		var clearCache = options.GetNullableBool(ClearCacheProperty);
		var disableSSLValidation = options.GetNullableBool(DisableSSLValidationProperty);

		string toolExecutable;
		try
		{
			var (installed, installError) = NETCoreToolHelpers
				.EnsureToolAsync("Microsoft.OpenApi.Kiota", ToolDirectory, version)
				.GetAwaiter().GetResult();

			if (!installed)
			{
				CreateDiagnostic(
					"KG0000",
					"Microsoft Kiota installation failed",
					installError ?? "Failed to install or locate the Microsoft Kiota tool.").Report(context);
				return;
			}

			toolExecutable = NETCoreToolHelpers.GetExecutablePath(ToolDirectory, "kiota");
		}
		catch (Exception ex)
		{
			CreateDiagnostic("KG0000", "Microsoft Kiota installation failed", ex.Message).Report(context);
			return;
		}

		foreach (var spec in specs)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var specPath = spec.Path;
			if (!File.Exists(specPath))
				continue;

			var specFileName = Path.GetFileNameWithoutExtension(specPath);
			var effectiveNamespace = namespaceName ?? SanitizationHelpers.Sanitize(specFileName);

			var tempOut = DirectoryHelpers.CreateTemporary(
				Path.Combine(Path.GetTempPath(), "Roslyn", "Advanced Compiler Services for .NET"));

			try
			{
				var engine = new KiotaEngine(
					d: specPath,
					a: null,
					o: tempOut,
					l: language,
					c: className,
					n: effectiveNamespace,
					tam: typeAccessModifier,
					ll: logLevel,
					b: backingStore,
					ebc: excludeBackwardCompatible,
					ad: additionalData,
					s: serializers,
					ds: deserializers,
					co: cleanOutput,
					m: structuredMimeTypes,
					i: includePaths,
					e: excludePaths,
					dvr: disableValidationRules,
					cc: clearCache,
					dsv: disableSSLValidation);

				var runResult = ProcessHelpers
					.RunProcess(toolExecutable, engine.ToString(), TimeSpan.FromMinutes(2))
					.GetAwaiter().GetResult();

				if (runResult.ExitCode != 0)
				{
					CreateDiagnostic(
						"KG0001",
						"OpenAPI generation failed",
						$"Microsoft Kiota failed for spec '{specPath}' with exit code {runResult.ExitCode}: {runResult.StandardError.ReplaceLineEndings(" ")}").Report(context);
					DirectoryHelpers.TryDelete(tempOut);
					continue;
				}

				var csFiles = Directory.EnumerateFiles(tempOut, "*.cs", SearchOption.AllDirectories).ToArray();
				if (csFiles.Length == 0)
				{
					CreateDiagnostic(
						"KG0002",
						"No C# files generated",
						$"Microsoft Kiota produced no C# files for spec '{specPath}'").Report(context);
					DirectoryHelpers.TryDelete(tempOut);
					continue;
				}

				foreach (var cs in csFiles)
				{
					try
					{
						var content = File.ReadAllText(cs, Encoding.UTF8);
						var rel = Path.GetRelativePath(tempOut, cs)
							.Replace(Path.DirectorySeparatorChar, '.')
							.Replace(Path.AltDirectorySeparatorChar, '.');
						var hintName = $"{SanitizationHelpers.Sanitize(engine.NamespaceName!)}.{SanitizationHelpers.Sanitize(rel)}";
						AddSource(hintName, content);
					}
					catch (Exception ex)
					{
						CreateDiagnostic(
							"KG0003",
							"Failed to add generated file",
							$"Failed to add '{cs}': {ex.Message}").Report(context);
					}
				}

				DirectoryHelpers.TryDelete(tempOut);
			}
			catch (Exception ex)
			{
				CreateDiagnostic("KG9999", "OpenAPI generator exception", ex.ToString()).Report(context);
				DirectoryHelpers.TryDelete(tempOut);
			}
		}
	}
}
