using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace dotnet_i18n_translate
{
    internal static class Serializer
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new()
        {
            AllowTrailingCommas = false,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping //JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        public static ValueTask<T?> Deserialize<T>(Stream stream, CancellationToken cancellationToken)
        {
            return JsonSerializer.DeserializeAsync<T>(stream, s_serializerOptions, cancellationToken);
        }

        public static string Serialize<T>(T document)
        {
            return JsonSerializer.Serialize(document, s_serializerOptions);
        }
    }
}
