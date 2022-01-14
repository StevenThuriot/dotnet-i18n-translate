using System.IO;

namespace dotnet_i18n_translate.Translators;

public interface ITranslator
{
    Task<bool> Run();
}

internal abstract class BaseTranslator : ITranslator
{
    protected readonly Options _options;
    protected readonly ILogger _logger;

    protected BaseTranslator(Options options, ILogger logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<bool> Run()
    {
        _logger.LogInformation("Translating files");

        var path = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(path);

        var fileName = $"{_options.Language}.json";
        var sourceFile = directory.GetFiles(fileName, SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (sourceFile is null)
        {
            _logger.LogError("{filename} was not found", fileName);
            return Task.FromResult(false);
        }

        IEnumerable<FileInfo> targetFiles;
        if (string.IsNullOrWhiteSpace(_options.TargetLanguage))
        {
            targetFiles = directory.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly).Where(x => x.FullName != sourceFile.FullName).ToList();

            if (!targetFiles.Any())
            {
                _logger.LogWarning("No target files found. Specify a specific language if none exist");
                return Task.FromResult(false);
            }
        }
        else
        {
            var target = new FileInfo($"{_options.TargetLanguage}.json");
            if (!target.Exists)
            {
                _logger.LogInformation("Creating {file}", target.Name);
                target.CreateText().Dispose();
            }

            targetFiles = new[] { target };
        }

        return Translate(sourceFile, targetFiles);
    }

    private async Task<bool> Translate(FileInfo sourceFile, IEnumerable<FileInfo> targetFiles)
    {
        var sourceTranslation = await Translation.Create(sourceFile, _logger);

        var result = true;

        foreach (var target in targetFiles)
        {
            try
            {
                var targetTranslation = await Translation.Create(target, _logger);
                var next = await Translate(sourceTranslation, targetTranslation);
                result = result && next;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while translating {source} to {target}", sourceTranslation.Language, target.Name);
                result = false;
            }
        }

        return result;
    }

    protected abstract Task<bool> Translate(Translation source, Translation target);
}
