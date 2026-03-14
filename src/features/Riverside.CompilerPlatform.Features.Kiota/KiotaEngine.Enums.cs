namespace Riverside.CompilerPlatform.Features.Kiota;

partial class KiotaEngine
{
	public enum Accessibility
	{
		Internal,
		Private,
		Protected,
		Public,
	}

	public enum GenerationLanguage
	{
		CSharp,
		Dart,
		Go,
		HTTP,
		Java,
		PHP,
		Python,
		Ruby,
		TypeScript,
	}

	public enum ConsoleLogLevel
	{
		Critical,
		Debug,
		Error,
		Information,
		None,
		Trace,
		Warning,
	}

	public enum ValidationRules
	{
		DivergentResponseSchema,
		GetWithBody,
		InconsistentTypeFormatPair,
		KnownAndNotSupportedFormats,
		MissingDiscriminator,
		MultipleServerEntries,
		NoContentWithBody,
		NoServerEntry,
		UrlForm,
		EncodedComplex,
		ValidationRuleSetExtensions,
	}
}
