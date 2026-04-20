export interface VoteResponse {
  id: string,

  originalPictureAttributeId: number,
  originalPostId: number,
  originalPostPictureCount: number,

  duplicatePictureAttributeId: number,
  duplicatePostId: number,
  duplicatePostPictureCount: number,

  nsfw: boolean,

  yesVotes: number,
  noVotes: number,

  createdAt: string,
}