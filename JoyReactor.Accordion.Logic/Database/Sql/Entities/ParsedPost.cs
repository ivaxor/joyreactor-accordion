namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public record ParsedPost
{
    public Guid Id { get; set; }

    public int NumberId { get; set; }

    public virtual ParsedPostAttributePicture[] AttributePictures { get; set; }
    public virtual ParsedPostAttributeEmbeded[] AttributeEmbeds { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}