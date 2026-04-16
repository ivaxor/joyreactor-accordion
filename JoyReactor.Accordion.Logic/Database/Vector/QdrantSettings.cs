namespace JoyReactor.Accordion.Logic.Database.Vector;

public record QdrantSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string CollectionName { get; set; }
    public ulong CollectionVectorSize { get; set; }
}