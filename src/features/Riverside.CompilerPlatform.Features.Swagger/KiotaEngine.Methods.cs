using System.Text;

namespace Riverside.CompilerPlatform.Features.Swagger;

partial class KiotaEngine
{
	/// <summary>
	/// Returns a string representation of the command-line arguments for the <c>kiota generate</c> tool based on the current property values.
	/// </summary>
	/// <returns>
	/// A string containing the assembled <c>kiota generate</c> command with all applicable options and arguments.
	/// The string reflects the current state of the object's properties.
	/// </returns>
	public override string ToString()
	{
		var command = new StringBuilder().Append("kiota generate");

		if (!string.IsNullOrWhiteSpace(Path))
		{
			command.Append($" --openapi {Path}");
		}
		if (!string.IsNullOrWhiteSpace(Manifest))
		{
			command.Append($" --manifest {Manifest}");
		}
		if (!string.IsNullOrWhiteSpace(Output))
		{
			command.Append($" --output {Output}");
		}
		command.Append($" --language {Language}");
		if (!string.IsNullOrWhiteSpace(ClassName))
		{
			command.Append($" --class-name {ClassName}");
		}
		if (TypeAccessModifier is not null)
		{
			command.Append($" --type-access-modifier {TypeAccessModifier}");
		}
		if (!string.IsNullOrWhiteSpace(NamespaceName))
		{
			command.Append($" --namespace-name {NamespaceName}");
		}
		if (LogLevel is not null)
		{
			command.Append($" --log-level {LogLevel}");
		}
		if (BackingStore is not null)
		{
			command.Append($" --backing-store {BackingStore}");
		}
		if (ExcludeBackwardCompatible is not null)
		{
			command.Append($" --exclude-backward-compatible {ExcludeBackwardCompatible}");
		}
		if (AdditionalData is not null)
		{
			command.Append($" --additional-data {AdditionalData}");
		}
		if (Serializer is not null)
		{
			var serializers = new StringBuilder().Append(" --serializer ");
			foreach (var serializer in Serializer)
			{
				serializers.Append(serializer + "|");
			}
			serializers.Remove(serializers.Length, 1); // remove final '|' char
			command.Append(serializers.ToString());
		}
		if (Deserializer is not null)
		{
			var deserializers = new StringBuilder().Append(" --deserializer ");
			foreach (var deserializer in Deserializer)
			{
				deserializers.Append(deserializer + "|");
			}
			deserializers.Remove(deserializers.Length, 1); // remove final '|' char
			command.Append(deserializers.ToString());
		}
		if (CleanOutput is not null)
		{
			command.Append($" --clean-output {CleanOutput}");
		}
		if (StructuredMimeTypes is not null)
		{
			var structuredMimeTypes = new StringBuilder().Append(" --structured-mime-types ");
			foreach (var structuredMimeType in StructuredMimeTypes)
			{
				structuredMimeTypes.Append(structuredMimeType + "|");
			}
			structuredMimeTypes.Remove(structuredMimeTypes.Length, 1); // remove final '|' char
			command.Append(structuredMimeTypes.ToString());
		}
		if (IncludePath is not null)
		{
			var includePaths = new StringBuilder().Append(" --include-path ");
			foreach (var includePath in IncludePath)
			{
				includePaths.Append(includePath + "|");
			}
			includePaths.Remove(includePaths.Length, 1); // remove final '|' char
			command.Append(includePaths.ToString());
		}
		if (ExcludePath is not null)
		{
			var excludePaths = new StringBuilder().Append(" --exclude-path ");
			foreach (var excludePath in ExcludePath)
			{
				excludePaths.Append(excludePath + "|");
			}
			excludePaths.Remove(excludePaths.Length, 1); // remove final '|' char
			command.Append(excludePaths.ToString());
		}
		if (DisableValidationRules is not null)
		{
			var disableValidationRules = new StringBuilder().Append(" --disable-validation-rules ");
			foreach (var disableValidationRule in DisableValidationRules)
			{
				disableValidationRules.Append(disableValidationRule + "|");
			}
			disableValidationRules.Remove(disableValidationRules.Length, 1); // remove final '|' char
			command.Append(disableValidationRules.ToString());
		}
		if (ClearCache is not null)
		{
			command.Append(" --clear-cache");
		}
		if (DisableSSLValidation is not null)
		{
			command.Append(" --disable-ssl-validation");
		}

		return command.ToString();
	}
}
