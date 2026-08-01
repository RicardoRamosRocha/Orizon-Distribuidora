using Orizon.Distribuidora.Application.Importacoes;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface ISimilarityEngine
{
    IReadOnlyList<RecognitionResult> Recognize(
        string header,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? additionalSynonyms = null);
}
