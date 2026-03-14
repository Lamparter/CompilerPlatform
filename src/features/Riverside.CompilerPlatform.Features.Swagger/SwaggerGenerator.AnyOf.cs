using System.Text;

namespace Riverside.CompilerPlatform.Features.Swagger;

partial class SwaggerGenerator
{
	private const string AnyOf_Imports =
		"""
		using System;
		using System.Text.Json;
		using System.Text.Json.Serialization;
		""";

	private const string AnyOf_BaseClass =
		"""
			public abstract class AnyOf
			{
				public abstract object Value { get; }
			}
		""";

	private const string AnyOf_GenericClass =
		"""
			public sealed class AnyOf<T1, T2> : AnyOf
			{
				private readonly object _value;

				public AnyOf(T1 value) => _value = value!;
				public AnyOf(T2 value) => _value = value!;

				public bool IsT1 => _value is T1;
				public bool IsT2 => _value is T2;

				public T1 AsT1 => _value is T1 v ? v : throw new InvalidOperationException("Value is not T1");
				public T2 AsT2 => _value is T2 v ? v : throw new InvalidOperationException("Value is not T2");

				public override object Value => _value;

				public static implicit operator AnyOf<T1, T2>(T1 value) => new AnyOf<T1, T2>(value);
				public static implicit operator AnyOf<T1, T2>(T2 value) => new AnyOf<T1, T2>(value);

				public override string ToString() => _value?.ToString() ?? "";
			}
		""";

	private const string AnyOf_JsonConverter =
		"""
			public sealed class AnyOfJsonConverter<T1, T2> : JsonConverter<AnyOf<T1, T2>>
			{
				public override AnyOf<T1, T2> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				{
					var readerCopy = reader;

					try
					{
						var t1 = JsonSerializer.Deserialize<T1>(ref readerCopy, options);
						reader = readerCopy;
						return new AnyOf<T1, T2>(t1!);
					}
					catch
					{
						// ignore and try T2
					}

					var t2 = JsonSerializer.Deserialize<T2>(ref reader, options);
					return new AnyOf<T1, T2>(t2!);
				}

				public override void Write(Utf8JsonWriter writer, AnyOf<T1, T2> value, JsonSerializerOptions options)
				{
					JsonSerializer.Serialize(writer, value.Value, value.Value.GetType(), options);
				}
			}
		""";

	private string AnyOf_Polyfill(string @namespace)
	{
		var anyOf = new StringBuilder();

		anyOf.AppendLine(AnyOf_Imports);
		anyOf.AppendLine();
		anyOf.AppendLine($"namespace {@namespace}\n{{");
		anyOf.AppendLine();
		anyOf.AppendLine(AnyOf_BaseClass);
		anyOf.AppendLine();
		anyOf.AppendLine(AnyOf_GenericClass);
		anyOf.AppendLine();
		anyOf.AppendLine(AnyOf_JsonConverter);
		anyOf.AppendLine();
		anyOf.AppendLine("}");

		return anyOf.ToString();
	}
}
