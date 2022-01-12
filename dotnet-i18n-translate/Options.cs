using CommandLine;

namespace dotnet_i18n_translate;

public class Options
{
    [Option('v', "verbose", Required = false, Default = false, HelpText = "Turns on verbose logging")]
    public bool Verbose { get; set; }

    [Option('l', "language", Default = "en", Required = false, HelpText = "The language to start from")]
    public string Language { get; set; } = "en";

    [Option('t', "target", Required = false, HelpText = "The target language. If unset, all languages will be selected.")]
    public string? TargetLanguage { get; set; } = null!;

    [Option('a', "authkey", Required = true, HelpText = "DeepL Auth Key. If not set, tries to use DeepLAuthKey environment variable instead. Required if no such variable exists.")]
    public string AuthKey { get; set; } = null!;

    [Option('k', "key", Required = false, HelpText = "Specific key to translate. If it already exists it will be overwritten.")]
    public IEnumerable<string>? SpecificKeys { get; set; } = null!;

    [Option("validate", Required = false, HelpText = "Only validate without actually translating.", Default = false)]
    public bool Validate { get; set; }

    public static Options? Get(IEnumerable<string> args)
    {
        if (!args.Contains("-a") && !args.Contains("--authkey"))
        {
            if (args.Contains("--validate"))
            {
                args = args.Append("-a").Append("fake_key").ToList();
            }
            else
            {
                var key = Environment.GetEnvironmentVariable("DeepLAuthKey");
                if (!string.IsNullOrWhiteSpace(key))
                {
                    args = args.Append("-a").Append(key).ToList();
                }
            }
        }

        var parser = new Parser(with => with.HelpWriter = Console.Out);
        var parsed = parser.ParseArguments<Options>(args);

        return parsed.MapResult(x => x, e =>
        {
            if (args?.Any() != true || e.Any(x => x.Tag is ErrorType.HelpRequestedError or ErrorType.HelpVerbRequestedError or ErrorType.VersionRequestedError))
            {
                return null!;
            }
            else
            {
                throw new ApplicationException("Invalid startup arguments");
            }
        });
    }
}
