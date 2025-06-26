using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace WebThoiTrang.Extensions
{
    public static class SessionExtensions
    {
        private static readonly JsonSerializerOptions _jsonOpt =
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value, _jsonOpt));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpt);
        }
    }
}
