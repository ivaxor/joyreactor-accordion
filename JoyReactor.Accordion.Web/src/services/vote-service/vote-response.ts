export interface VoteResponse {
  id: string,

  originalPictureAttributeId: number,
  duplicatePictureAttributeId: number,

  yesVotes: number,
  noVotes: number,

  createdAt: Date,
}