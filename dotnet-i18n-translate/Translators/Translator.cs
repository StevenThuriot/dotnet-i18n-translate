using System.Text.RegularExpressions;

namespace dotnet_i18n_translate.Translators;

internal sealed class Translator : BaseTranslator
{
    private readonly ITranslationService _translationService;

    public Translator(ITranslationService translationService, Options options, ILogger<Translator> logger)
        : base(options, logger)
    {
        _translationService = translationService;
    }

    private static readonly Regex s_variableCorrectingRegex = new(@"{{\W*(?<variable>.+?)\W*}}", RegexOptions.Compiled);
    protected override async Task<bool> Translate(Translation source, Translation target)
    {
        _logger.LogInformation("- Translating {source} to {target}", source.Language, target.Language);

        var missingKeys = _options.SpecificKeys?.Any() == true ? _options.SpecificKeys.Select(x => new JsonPath(x)).ToList() : target.FindMissing(source);
        var sourceRows = source.Get(missingKeys).ToList();

        var targetRows = (await _translationService.Translate(sourceRows, source.Language, target.Language)).ToList();

        int i = 0;
        foreach (var key in missingKeys)
        {
            string value = targetRows[i];

            var sourceMatches = s_variableCorrectingRegex.Matches(sourceRows[i]);
            var targetMatches = s_variableCorrectingRegex.Matches(value);

            if (sourceMatches.Count > 0)
            {
                if (sourceMatches.Count == targetMatches.Count)
                {
                    for (int m = 0; m < sourceMatches.Count; m++)
                    {
                        var sourceMatch = sourceMatches[m];
                        var targetMatch = targetMatches[m];

                        value = value.Replace(targetMatch.ToString(), sourceMatch.ToString());
                    }
                }
                else
                {
                    _logger.LogWarning("Something might have gone wrong translating {source} to {target} for {key}.", source.Language, target.Language, key);
                }
            }

            target.Set(key, value);

            i++;
        }

        await target.Save();

        return true;
    }
}