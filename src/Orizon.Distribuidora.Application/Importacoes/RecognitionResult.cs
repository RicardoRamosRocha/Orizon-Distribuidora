namespace Orizon.Distribuidora.Application.Importacoes;

public enum RecognitionStrategy
{
    Synonym,
    Similarity,
    Exact
}

public sealed record RecognitionResult(
    string CampoDestino,
    double Confidence,
    RecognitionStrategy Strategy,
    string MatchedHeader);
