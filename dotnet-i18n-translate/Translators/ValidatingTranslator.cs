namespace dotnet_i18n_translate.Translators;

internal sealed class ValidatingTranslator : BaseTranslator
{
    public ValidatingTranslator(Options options, ILogger<ValidatingTranslator> logger)
        : base(options, logger)
    {
    }

    protected override Task<bool> Translate(Translation source, Translation target)
    {
        _logger.LogInformation("- Checking {source} to {target}", source.Language, target.Language);

        var missingKeys = target.FindMissing(source);
        if (missingKeys?.Any() == true)
        {
            _logger.LogError(@"Found missing keys in {language} file:
{keyList}", target.Language, string.Join(Environment.NewLine, missingKeys.Select(x => "\t" + x)));
            return Task.FromResult(false);
        }
        else
        {
            _logger.LogInformation("{language} file is OK", target.Language);
            return Task.FromResult(true);
        }
    }
}
