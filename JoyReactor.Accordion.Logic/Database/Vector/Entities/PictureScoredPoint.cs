using Qdrant.Client.Grpc;

namespace JoyReactor.Accordion.Logic.Database.Vector.Entities;

public record PictureScoredPoint : PictureRetrivedPoint
{
    public PictureScoredPoint() { }

    public PictureScoredPoint(ScoredPoint scoredPoint) : base(scoredPoint.Payload)
    {
        Score = scoredPoint.Score;
    }

    public float Score { get; set; }
}