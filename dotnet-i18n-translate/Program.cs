using dotnet_i18n_translate;
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
    await services.GetRequiredService<Translator>().Run();
}
catch (ApplicationException e)
{
    Console.WriteLine(e.Message);
}

ServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection()
                         .AddHttpClient().RemoveAll<IHttpMessageHandlerBuilderFilter>()
                         .AddLogging(c =>
                         {
                             c.AddConsoleFormatter<SuperSimpleConsoleFormatter, ConsoleFormatterOptions>().AddConsole(o => o.FormatterName = nameof(SuperSimpleConsoleFormatter));
                             c.AddDebug();
                             c.SetMinimumLevel(options.Verbose ? LogLevel.Trace : LogLevel.Information);
                         })
                         .AddSingleton(options)
                         .AddSingleton<ITranslationService, DeepLTranslationService>()
                         .AddSingleton<Translator>();

    return services.BuildServiceProvider();
}