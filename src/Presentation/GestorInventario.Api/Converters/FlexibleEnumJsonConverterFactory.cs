using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestorInventario.Api.Converters;

public sealed class FlexibleEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var type = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return type.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
        if (underlyingType is not null)
        {
            var converterType = typeof(NullableFlexibleEnumJsonConverter<>).MakeGenericType(underlyingType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        var concreteType = typeof(FlexibleEnumJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(concreteType)!;
    }

    private sealed class FlexibleEnumJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var enumText = reader.GetString();
                if (!string.IsNullOrWhiteSpace(enumText) && Enum.TryParse(enumText, ignoreCase: true, out TEnum parsed))
                {
                    return parsed;
                }

                throw new JsonException($"Unable to convert \"{enumText}\" to {typeof(TEnum).Name}.");
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var enumValue))
                {
                    if (Enum.IsDefined(typeof(TEnum), enumValue))
                    {
                        return (TEnum)Enum.ToObject(typeof(TEnum), enumValue);
                    }

                    throw new JsonException($"Value '{enumValue}' is not defined for enum {typeof(TEnum).Name}.");
                }

                throw new JsonException($"Unable to read numeric value for enum {typeof(TEnum).Name}.");
            }

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing enum {typeof(TEnum).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToInt32(value));
        }
    }

    private sealed class NullableFlexibleEnumJsonConverter<TEnum> : JsonConverter<TEnum?>
        where TEnum : struct, Enum
    {
        private readonly FlexibleEnumJsonConverter<TEnum> innerConverter = new();

        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return innerConverter.Read(ref reader, typeof(TEnum), options);
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                innerConverter.Write(writer, value.Value, options);
                return;
            }

            writer.WriteNullValue();
        }
    }
}
