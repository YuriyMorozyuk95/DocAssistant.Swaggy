using Shared.Json;
using System.Text.Json;

namespace Shared.Extensions;

public static class JsonExtensions
{
    public static string ToJson(this object obj, JsonSerializerOptions options = null)
    {
        options ??= SerializerOptions.Default;
        var serializedObj = JsonSerializer.Serialize(obj, options);

        return System.Text.RegularExpressions.Regex.Unescape(serializedObj);  
    }
}