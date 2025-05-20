using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HitServicesCore.Helpers.JsonConverters;

public class AutoStringToInt64Converter : JsonConverter<object>
{
	public override bool CanConvert(Type typeToConvert)
	{
		return typeof(long) == typeToConvert;
	}

	public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			string stringValue = reader.GetString();
			if (long.TryParse(stringValue, out var value))
			{
				return value;
			}
		}
		else if (reader.TokenType == JsonTokenType.Number)
		{
			return reader.GetInt64();
		}
		throw new JsonException();
	}

	public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue((long)value);
	}
}
