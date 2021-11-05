using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        private readonly JObject _document;
        private readonly Lazy<IEnumerable<JsonPath>> _keys;
        private readonly ILogger _logger;

        public string Language => Path.GetFileNameWithoutExtension(_file.Name).ToUpperInvariant();

        public static async Task<Translation> Create(FileInfo file, ILogger logger)
        {
            using var stream = file.OpenRead();
            
            var textreader = new StreamReader(stream);
            var jsonreader = new JsonTextReader(textreader);
            var document = await JObject.LoadAsync(jsonreader);

            return new Translation(file, document, logger);
        }

        private Translation(FileInfo file, JObject document, ILogger logger)
        {
            _file = file;
            _document = document;
            _keys = new Lazy<IEnumerable<JsonPath>>(() => ListKeys(document));
            _logger = logger;
        }

        public IEnumerable<JsonPath> FindMissing(Translation translation)
        {
            var myKeys = _keys.Value;
            var theirKeys = translation._keys.Value;

            return myKeys.Except(theirKeys);
        }

        private IEnumerable<JsonPath> ListKeys(JToken token)
        {
            foreach (var sub in token)
            {
                var childResults = sub.Children().SelectMany(child => ListKeys(child)).ToList();
                if (childResults.Count == 0)
                {
                    yield return new JsonPath(sub.Path.Replace("[", "").Replace("]", ""));
                }
                else
                {
                    foreach (var childResult in childResults)
                    {
                        yield return childResult;
                    }
                }
            }
        }

        public IEnumerable<string> Get(IEnumerable<JsonPath> keys)
        {
            foreach (var key in keys)
            {
                if (key.TryGet(_document, out string? value))
                {
                    yield return value;
                }
                else
                {
                    _logger.LogWarning("Could not find key {key}. Skipping.", key);
                }
            }

            yield break;
        }

        public void Set(JsonPath key, string value)
        {
            key.Set(_document, value);
            _dirty = true;
        }

        public async Task Save()
        {
            if (_dirty)
            {
                _logger.LogInformation("Saving {file}", _file.Name);

                string serialized = _document.ToString(Formatting.Indented);
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