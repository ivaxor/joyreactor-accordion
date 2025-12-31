namespace JoyReactor.Accordion.Logic.Database.Vector;

public record QdrantSettings
{
    public string Host { get; set; }
    public string CollectionName { get; set; }
    public ulong CollectionVectorSize { get; set; }

    public ulong SearchLimit { get; set; }
    public float SearchScoreThreshold { get; set; }
}