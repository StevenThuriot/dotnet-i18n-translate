using dotnet_i18n_translate;
using dotnet_i18n_translate.Translators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Console;

Options? options;

try
{
    options = Options.Get(args);
    if (options is null)
    {
        return;
    }

    using var services = BuildServiceProvider();
    var result = await services.GetRequiredService<ITranslator>().Run();

    if (!result)
    {
        Environment.ExitCode = 1;
    }
}
catch (ApplicationException e)
{
    Console.WriteLine(e.Message);
    Environment.ExitCode = 2;
}

ServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection()
                         .AddLogging(c =>
                         {
                             c.AddConsoleFormatter<SuperSimpleConsoleFormatter, ConsoleFormatterOptions>().AddConsole(o => o.FormatterName = nameof(SuperSimpleConsoleFormatter));
                             c.AddDebug();
                             c.SetMinimumLevel(options.Verbose ? LogLevel.Trace : LogLevel.Information);
                         })
                         .AddSingleton(options);

    if (options.Validate)
    {
        services = services.AddSingleton<ITranslator, ValidatingTranslator>();
    }
    else
    {
        services = services.AddHttpClient().RemoveAll<IHttpMessageHandlerBuilderFilter>()
                           .AddSingleton<ITranslationService, DeepLTranslationService>()
                           .AddSingleton<ITranslator, Translator>();
    }

    return services.BuildServiceProvider();
}