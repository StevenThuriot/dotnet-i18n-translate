using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace dotnet_i18n_translate
{


    public interface ITranslationService
    {
        Task<IEnumerable<string>> Translate(IEnumerable<string> texts, string? sourceLanguageCode, string targetLanguageCode, CancellationToken cancellationToken = default);
    }
}
