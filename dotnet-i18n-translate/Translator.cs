using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_i18n_translate
{
    public sealed class Translator
    {
        private readonly ITranslationService _translationService;
        private readonly Options _options;
        private readonly ILogger<Translator> _logger;

        public Translator(ITranslationService translationService, Options options, ILogger<Translator> logger)
        {
            _translationService = translationService;
            _options = options;
            _logger = logger;
        }

        public Task Run()
        {
            var path = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(path);

            var fileName = $"{_options.Language}.json";
            var sourceFile = directory.GetFiles(fileName, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (sourceFile is null)
            {
                _logger.LogError("{filename} was not found", fileName);
                return Task.CompletedTask;
            }

            IEnumerable<FileInfo> targetFiles;
            if (string.IsNullOrWhiteSpace(_options.TargetLanguage))
            {
                targetFiles = directory.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly).Where(x => x.FullName != sourceFile.FullName).ToList();

                if (!targetFiles.Any())
                {
                    _logger.LogWarning("No target files found. Specify a specific language if none exist");
                    return Task.CompletedTask;
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

        private async Task Translate(FileInfo sourceFile, IEnumerable<FileInfo> targetFiles)
        {
            var sourceTranslation = await Translation.Create(sourceFile, _logger);

            foreach (var target in targetFiles)
            {
                await Translate(sourceTranslation, target);
            }
        }

        private async Task Translate(Translation sourceTranslation, FileInfo target)
        {
            _logger.LogInformation("Translating {source} to {target}", sourceTranslation.Language, target.Name);

            var targetTranslation = await Translation.Create(target, _logger);

            var missingKeys = _options.SpecificKeys?.Any() == true ? _options.SpecificKeys : targetTranslation.FindMissing(sourceTranslation);
            var sourceRows = sourceTranslation.Get(missingKeys);
            
            var targetRows = await _translationService.Translate(sourceRows, sourceTranslation.Language, targetTranslation.Language);

            int i = 0;
            foreach (var key in missingKeys)
            {
                targetTranslation.Set(key, targetRows.ElementAt(i++));
            }

            await targetTranslation.Save();
        }
    }
}
