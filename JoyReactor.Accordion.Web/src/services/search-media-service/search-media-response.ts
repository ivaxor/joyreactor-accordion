export interface SearchMediaResponse {
  score: number,
  hostName: string,
  postId?: number,
  postAttributeId?: number,
  commentId?: number,
  commentAttributeId?: number,
}