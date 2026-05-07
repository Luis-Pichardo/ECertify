using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Json;

namespace eCertify.Utils
{
    public static class TempDataExtensions
    {
        public static void SetObject<T>(this ITempDataDictionary tempData, string key, T value)
        {
            tempData[key] = JsonSerializer.Serialize(value);
        }

        public static T GetObject<T>(this ITempDataDictionary tempData, string key)
        {
            tempData.TryGetValue(key, out var value);
            return value == null ? default : JsonSerializer.Deserialize<T>((string)value);
        }
    }
}
