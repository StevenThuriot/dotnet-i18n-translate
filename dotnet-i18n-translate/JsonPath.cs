using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace dotnet_i18n_translate
{
    public class JsonPath : IEquatable<JsonPath>
    {
        private readonly IReadOnlyList<string> _path;

        public JsonPath(string path)
            : this (Split(path))
        {
        }

        private static IEnumerable<string> Split(string path)
        {
            var builder = new StringBuilder(path.Length);

            bool singleKey = false;
            foreach (char token in path)
            {
                switch (token)
                {
                    case '.' when !singleKey:
                        yield return builder.ToString();
                        builder.Clear();
                        break;

                    case '\'':
                        singleKey = !singleKey;
                        break;

                    default:
                        builder.Append(token);
                        break;
                }
            }

            if (builder.Length > 0)
            {
                yield return builder.ToString();
            }
        }

        public JsonPath(IEnumerable<string> path)
        {
            _path = path.ToList().AsReadOnly();
            if (_path.Count == 0)
            {
                throw new ArgumentException("An empty path is not allowed", nameof(path));
            }
        }

        public void Set(JObject document, string value)
        {
            var newObject = new JObject();
            var original = newObject;

            foreach (string section in _path.Take(_path.Count - 1))
            {
                newObject[section] = newObject = new JObject();
            }

            newObject[_path.Last()] = value;

            document.Merge(original);
        }

        public bool TryGet(JObject document, [NotNullWhen(true)] out string? value)
        {
            JToken? token = _path.Aggregate((JToken?)document, (result, next) => result?[next]);

            if (token is null)
            {
                value = null;
                return false;
            }
            else
            {
                value = token.ToString();
                return true;
            }
        }

        public override string ToString() => _path.Aggregate("$", (result, next) => result + "[" + next + "]");

        public override bool Equals(object? obj) => obj is JsonPath path && Equals(path);

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (string item in _path)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }

        public bool Equals(JsonPath? other)
        {
            if (other is null)
            {
                return false;
            }

            if (other._path.Count != _path.Count)
            {
                return false;
            }

            for (int i = 0; i < _path.Count; i++)
            {
                if (_path[i] != other._path[i])
                {
                    return false;
                } 
            }

            return true;
        }
    }
}
