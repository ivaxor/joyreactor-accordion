using JoyReactor.Accordion.Logic.Database.Sql.Entities;

namespace JoyReactor.Accordion.WebAPI.Models.Responses;

public record DuplicatePictureVoteThinResponse
{
    public Guid Id { get; set; }

    public int OriginalPictureAttributeId { get; set; }
    public int OriginalPostId { get; set; }
    public int OriginalPostPictureCount { get; set; }

    public int DuplicatePictureAttributeId { get; set; }
    public int DuplicatePostId { get; set; }
    public int DuplicatePostPictureCount { get; set; }

    public int YesVotes { get; set; }
    public int NoVotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DuplicatePictureVoteThinResponse() { }

    public DuplicatePictureVoteThinResponse(DuplicatePictureVoteExtended duplicatePictureVote)
    {
        Id = duplicatePictureVote.Id;

        OriginalPictureAttributeId = duplicatePictureVote.OriginalPicture.AttributeId;
        OriginalPostId = duplicatePictureVote.OriginalPostNumberId;
        OriginalPostPictureCount = duplicatePictureVote.OriginalPostPictureCount;

        DuplicatePictureAttributeId = duplicatePictureVote.DuplicatePicture.AttributeId;
        DuplicatePostId = duplicatePictureVote.DuplicatePostNumberId;
        DuplicatePostPictureCount = duplicatePictureVote.DuplicatePostPictureCount;

        YesVotes = duplicatePictureVote.YesVotes.Length;
        NoVotes = duplicatePictureVote.NoVotes.Length;
        CreatedAt = duplicatePictureVote.CreatedAt;
    }
}