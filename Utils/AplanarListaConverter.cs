using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eCertify.Utils
{

    public class AplanarListaConverter<T> : JsonConverter<List<T>>
    {
        private readonly string _propiedadContenedora;

        public AplanarListaConverter() : this("Tabla") { }

        public AplanarListaConverter(string propiedadContenedora)
        {
            _propiedadContenedora = propiedadContenedora;
        }

        public override List<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<T>>(doc.RootElement.GetRawText(), options);
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(_propiedadContenedora, out var subArray))
            {
                return JsonSerializer.Deserialize<List<T>>(subArray.GetRawText(), options);
            }

            return new List<T>();
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }



    public class AplanarListaConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(List<>);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            Type itemType = type.GetGenericArguments()[0];
            Type converterType = typeof(AplanarListaConverter<>).MakeGenericType(itemType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

}
