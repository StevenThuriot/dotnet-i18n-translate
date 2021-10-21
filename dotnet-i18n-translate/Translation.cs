using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_i18n_translate
{
    internal class Translation
    {
        private bool _dirty = false;
        private readonly FileInfo _file;
        private readonly Dictionary<string, string> _document;
        private readonly ILogger _logger;

        public string Language => Path.GetFileNameWithoutExtension(_file.Name).ToUpperInvariant();

        public static async Task<Translation> Create(FileInfo file, ILogger logger)
        {
            using var stream = file.OpenRead();
            var document = await Serializer.Deserialize<Dictionary<string, string>>(stream, default);
            return new Translation(file, document!, logger);
        }

        private Translation(FileInfo file, Dictionary<string, string> document, ILogger logger)
        {
            _file = file;
            _document = document;
            _logger = logger;
        }

        public IEnumerable<string> FindMissing(Translation translation)
        {
            return translation._document.Keys.Except(_document.Keys).ToList();
        }

        public IEnumerable<string> Get(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                if (_document.TryGetValue(key, out var value))
                {
                    yield return value;
                }
                else
                {
                    _logger.LogWarning("Could not find key {key}. Skipping.", key);
                }
            }
        }

        public void Set(string key, string value)
        {
            _document[key] = value;
            _dirty = true;
        }

        public async Task Save()
        {
            if (_dirty)
            {
                _logger.LogInformation("Saving {file}", _file.Name);

                string serialized = Serializer.Serialize(_document);
                await File.WriteAllTextAsync(_file.FullName, serialized);

                _dirty = false;
            }
            else
            {
                _logger.LogInformation("File {file} is not dirty, skipping save", _file.Name);
            }
        }
    }
}