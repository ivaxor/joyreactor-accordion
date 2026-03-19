export interface VoteResponse {
  id: string,

  originalPictureAttributeId: number,
  originalPostId: number,

  duplicatePictureAttributeId: number,
  duplicatePostId: number,

  yesVotes: number,
  noVotes: number,

  createdAt: string,
}