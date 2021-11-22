using System.Net.Http;

namespace dotnet_i18n_translate;

public sealed class DeepLTranslationService : ITranslationService
{
    private const string ProApiBaseUrl = "https://api.deepl.com/v2/translate";
    private const string FreeApiBaseUrl = "https://api-free.deepl.com/v2/translate";

    private readonly string _apiBaseUrl;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _authKey;

    public DeepLTranslationService(IHttpClientFactory httpClientFactory, Options options)
    {
        _httpClientFactory = httpClientFactory;

        _authKey = options.AuthKey;
        _apiBaseUrl = (_authKey.EndsWith(":fx", StringComparison.OrdinalIgnoreCase) ? FreeApiBaseUrl : ProApiBaseUrl) + "?auth_key=" + _authKey;
    }

    public async Task<IEnumerable<string>> Translate(IEnumerable<string> texts, string? sourceLanguageCode, string targetLanguageCode, CancellationToken cancellationToken)
    {
        var parameters = GetParameters(texts, sourceLanguageCode, targetLanguageCode);

        if (parameters is null)
        {
            return Enumerable.Empty<string>();
        }

        var client = _httpClientFactory.CreateClient();

        using HttpContent httpContent = new FormUrlEncodedContent(parameters!);

        var responseMessage = await client.PostAsync(_apiBaseUrl, httpContent, cancellationToken);

        if (!responseMessage.IsSuccessStatusCode)
        {
            var error = await responseMessage.Content.ReadAsStringAsync();
            throw new ApplicationException(error);
        }

        var stringContent = await responseMessage.Content.ReadAsStringAsync();
        var translationResultContent = JsonConvert.DeserializeObject<TranslationResult>(stringContent);
        return translationResultContent?.translations?.Select(x => x.text ?? "") ?? Enumerable.Empty<string>();
    }

    private IEnumerable<KeyValuePair<string, string>>? GetParameters(IEnumerable<string> texts, string? sourceLanguageCode, string targetLanguageCode)
    {
        var parameters = texts.Select(x => Pair("text", x)).ToList();

        if (parameters.Count == 0)
        {
            return null;
        }

        parameters.Add(Pair("target_lang", targetLanguageCode?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(targetLanguageCode))));
        parameters.Add(Pair("split_sentences", "1"));
        parameters.Add(Pair("preserve_formatting", "1"));
        parameters.Add(Pair("auth_key", _authKey));

        if (!string.IsNullOrWhiteSpace(sourceLanguageCode))
        {
            parameters.Add(Pair("source_lang", sourceLanguageCode.ToUpperInvariant()));
        }

        return parameters;
    }

    private static KeyValuePair<string, string> Pair(string key, string value) => new(key, value);

    class TranslationResult
    {
        public IEnumerable<Translation>? translations { get; set; }
    }

    class Translation
    {
        public string? detected_source_language { get; set; }
        public string? text { get; set; }
    }
}
