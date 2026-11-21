namespace Lugares.Application.Configuration;

public class EmbeddingBackfillOptions
{
    public const string SectionName = "Embeddings:Backfill";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 300;
    public int BatchSize { get; set; } = 32;
    public int MaxBackoffSeconds { get; set; } = 3600;
    public int SampleSize { get; set; } = 20;
    public double SimilarityThreshold { get; set; } = 0.999;
}
