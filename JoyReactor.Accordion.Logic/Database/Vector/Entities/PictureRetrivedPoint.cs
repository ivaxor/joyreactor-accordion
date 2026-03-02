using Google.Protobuf.Collections;
using Qdrant.Client.Grpc;

namespace JoyReactor.Accordion.Logic.Database.Vector.Entities;

public record PictureRetrivedPoint
{
    public PictureRetrivedPoint() { }

    public PictureRetrivedPoint(RetrievedPoint retrievedPoint) : this(retrievedPoint.Payload) { }

    public PictureRetrivedPoint(MapField<string, Value> payload)
    {
        HostName = payload.TryGetValue("hostName", out var hostNameValue) && hostNameValue.KindCase == Value.KindOneofCase.StringValue
            ? hostNameValue.StringValue
            : null;

        ContentVersion = payload.TryGetValue("contentVersion", out var contentVersionValue) && contentVersionValue.KindCase == Value.KindOneofCase.IntegerValue
            ? Convert.ToInt32(contentVersionValue.IntegerValue)
            : null;

        PostId = payload.TryGetValue("postId", out var postIdValue) && postIdValue.KindCase == Value.KindOneofCase.IntegerValue
            ? Convert.ToInt32(postIdValue.IntegerValue)
            : null;

        PostAttributeId = payload.TryGetValue("postAttributeId", out var postAttributeId) && postAttributeId.KindCase == Value.KindOneofCase.IntegerValue
            ? Convert.ToInt32(postAttributeId.IntegerValue)
            : null;

        CommentId = payload.TryGetValue("commentId", out var commentIdValue) && commentIdValue.KindCase == Value.KindOneofCase.IntegerValue
            ? Convert.ToInt32(commentIdValue.IntegerValue)
            : null;

        CommentAttributeId = payload.TryGetValue("commentAttributeId", out var commentAttributeIdValue) && commentAttributeIdValue.KindCase == Value.KindOneofCase.IntegerValue
            ? Convert.ToInt32(commentAttributeIdValue.IntegerValue)
            : null;
    }

    public string HostName { get; set; }

    public int? ContentVersion { get; set; }

    public int? PostId { get; set; }
    public int? PostAttributeId { get; set; }

    public int? CommentId { get; set; }
    public int? CommentAttributeId { get; set; }
}