using System.Diagnostics.CodeAnalysis;

namespace Riverside.CompilerPlatform.Features.Swagger;

/// <summary>
/// Represents the configuration and options for generating code using the Kiota engine.
/// </summary>
public partial class KiotaEngine
{
	/// <summary>
	/// The path or URI to the OpenAPI description file used to generate the code files.
	/// </summary>
	public string? Path { get; }

	/// <summary>
	/// The path or URI to the API manifest file used to generate the code files.
	/// Append #apikey if the target manifest contains multiple API dependencies entries.
	/// </summary>
	public string? Manifest { get; }

	/// <summary>
	/// The output directory path for the generated code files.
	/// </summary>
	public string? Output { get; }

	/// <summary>
	/// The target language for the generated code files.
	/// </summary>
	public required GenerationLanguage Language { get; set; }

	/// <summary>
	/// The class name to use for the core client class.
	/// </summary>
	public string? ClassName { get; }

	/// <summary>
	/// The type access modifier to use for the client types.
	/// </summary>
	public Accessibility? TypeAccessModifier { get; }

	/// <summary>
	/// The namespace to use for the core client class specified with the <see cref="ClassName"/> option.
	/// </summary>
	public string? NamespaceName { get; }

	/// <summary>
	/// The log level to use when logging messages to the main output.
	/// </summary>
	public ConsoleLogLevel? LogLevel { get; }

	/// <summary>
	/// Enables backing store for models.
	/// </summary>
	public bool? BackingStore { get; }

	/// <summary>
	/// Excludes backward compatible and obsolete assets from the generated result.
	/// Should be used for new clients.
	/// </summary>
	public bool? ExcludeBackwardCompatible { get; }

	/// <summary>
	/// Will include the 'AdditionalData' property for models.
	/// </summary>
	public bool? AdditionalData { get; }

	/// <summary>
	/// The fully qualified class names for serialisers.
	/// Use <c>none</c> to generate a client without any serialiser.
	/// </summary>
	public string[]? Serializer { get; }

	/// <summary>
	/// The fully qualified class names for deserialisers. 
	/// Use <c>none</c> to generate a client without any deserialiser.
	/// </summary>
	public string[]? Deserializer { get; }

	/// <summary>
	/// Removes all files from the output directory before generating the code files.
	/// </summary>
	public bool? CleanOutput { get; }

	/// <summary>
	/// The MIME types with optional priorities as defined in RFC9110 Accept header to use for structured data model generation.
	/// </summary>
	public string[]? StructuredMimeTypes { get; }

	/// <summary>
	/// The paths to include in the generation.
	/// Glob patterns accepted.
	/// Append <c>#OPERATION</c> to the pattern to specify the operation to include, e.g. <c>users/*/messages#GET</c>.
	/// </summary>
	public string[]? IncludePath { get; }

	/// <summary>
	/// The paths to exclude from the generation.
	/// Glob patterns accepted.
	/// Append <c>#OPERATION</c> to the pattern to specify the operation to exclude, e.g. <c>users/*/messages#GET</c>.
	/// </summary>
	public string[]? ExcludePath { get; }

	/// <summary>
	/// The OpenAPI description validation rules to disable.
	/// </summary>
	public ValidationRules[]? DisableValidationRules { get; }

	/// <summary>
	/// Clears any cached data for the current command.
	/// </summary>
	public bool? ClearCache { get; }

	/// <summary>
	/// Disables SSL certificate validation.
	/// </summary>
	public bool? DisableSSLValidation { get; }

	/// <summary>
	/// Initialises a new instance of the KiotaEngine class with the specified configuration settings for code generation, serialisation, and validation.
	/// </summary>
	/// <remarks>
	/// All parameters are optional and can be set to null to use default values.
	/// This constructor allows fine-grained control over the code generation process, including language selection, serialisation options, and validation settings.
	/// </remarks>
	/// <param name="d">The path to the input directory or file used for code generation. Can be null to use the default location.</param>
	/// <param name="a">The manifest file path that provides metadata for the generation process. Can be null if not required.</param>
	/// <param name="o">The output directory where generated files will be written. Can be null to use the current directory.</param>
	/// <param name="l">The target programming language for code generation.</param>
	/// <param name="c">The name of the root class to be generated. Can be null to use a default class name.</param>
	/// <param name="tam">The access modifier to apply to generated types. Can be null to use the default accessibility.</param>
	/// <param name="n">The namespace for the generated client class. Can be null to use the default namespace.</param>
	/// <param name="ll">The log level to use for diagnostic output during generation. Can be null to use the default log level.</param>
	/// <param name="b">Indicates whether to use a backing store for generated models. If null, the default behavior is used.</param>
	/// <param name="ebc">Indicates whether to exclude backward compatible code from the output. If null, the default behavior is used.</param>
	/// <param name="ad">Indicates whether to include additional data support in generated models. If null, the default behavior is used.</param>
	/// <param name="s">An array of serializer names to use for serialization. Can be null to use default serializers.</param>
	/// <param name="ds">An array of deserializer names to use for deserialization. Can be null to use default deserializers.</param>
	/// <param name="co">Indicates whether to clean the output directory before generation. If null, the default behavior is used.</param>
	/// <param name="m">An array of structured MIME types to support in generated code. Can be null to use default MIME types.</param>
	/// <param name="i">An array of paths to include in the generation process. Can be null to include all paths.</param>
	/// <param name="e">An array of paths to exclude from the generation process. Can be null to exclude none.</param>
	/// <param name="dvr">An array of validation rules to disable during generation. Can be null to enable all rules.</param>
	/// <param name="cc">Indicates whether to clear the internal cache before generation. If null, the default behavior is used.</param>
	/// <param name="dsv">Indicates whether to disable SSL validation for network operations. If null, the default behavior is used.</param>
	[SetsRequiredMembers]
	public KiotaEngine(string? d, string? a, string? o, GenerationLanguage l, string? c, Accessibility? tam, string? n, ConsoleLogLevel? ll, bool? b, bool? ebc, bool? ad, string[]? s, string[]? ds, bool? co, string[]? m, string[]? i, string[]? e, ValidationRules[]? dvr, bool? cc, bool? dsv)
	{
		Path = d;
		Manifest = a;
		Output = o;
		Language = l;
		ClassName = c;
		TypeAccessModifier = tam;
		NamespaceName = n;
		LogLevel = ll;
		BackingStore = b;
		ExcludeBackwardCompatible = ebc;
		AdditionalData = ad;
		Serializer = s;
		Deserializer = ds;
		CleanOutput = co;
		StructuredMimeTypes = m;
		IncludePath = i;
		ExcludePath = e;
		DisableValidationRules = dvr;
		ClearCache = cc;
		DisableSSLValidation = dsv;
	}
}
