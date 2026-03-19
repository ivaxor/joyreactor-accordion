using JoyReactor.Accordion.Logic.Database.Sql.Entities;
using JoyReactor.Accordion.Logic.Extensions;

namespace JoyReactor.Accordion.WebAPI.Models.Responses;

public record DuplicatePictureVoteThinResponse
{
    public Guid Id { get; set; }

    public int OriginalPictureAttributeId { get; set; }
    public int OriginalPostId { get; set; }

    public int DuplicatePictureAttributeId { get; set; }
    public int DuplicatePostId { get; set; }

    public int YesVotes { get; set; }
    public int NoVotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DuplicatePictureVoteThinResponse() { }

    public DuplicatePictureVoteThinResponse(DuplicatePictureVote duplicatePictureVote)
    {
        Id = duplicatePictureVote.Id;
        OriginalPictureAttributeId = duplicatePictureVote.OriginalPicture.AttributeId;
        OriginalPostId = duplicatePictureVote.OriginalPicture.PostId.ToInt();
        DuplicatePictureAttributeId = duplicatePictureVote.DuplicatePicture.AttributeId;
        DuplicatePictureAttributeId = duplicatePictureVote.DuplicatePicture.PostId.ToInt();
        YesVotes = duplicatePictureVote.YesVotes.Length;
        NoVotes = duplicatePictureVote.NoVotes.Length;
        CreatedAt = duplicatePictureVote.CreatedAt;
    }
}