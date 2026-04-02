using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DbEx.Test.Empty.Persistence
{
    public class JsonConverter<T>() : ValueConverter<T, string?>(v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null), v => System.Text.Json.JsonSerializer.Deserialize<T>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
    {
    }
}