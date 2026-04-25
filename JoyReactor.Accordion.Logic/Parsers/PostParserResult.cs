using JoyReactor.Accordion.Logic.Database.Sql.Entities;

namespace JoyReactor.Accordion.Logic.Parsers;

public record PostParserResult
{
    public required ParsedPost Post { get; init; }

    public required int[] NewPostAttributePictureNumberIds { get; init; }
    public required string[] NewPostAttributeEmbeddedUniqueIds { get; init; }
}