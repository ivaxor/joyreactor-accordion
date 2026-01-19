export interface SearchResponse {
  score: number;
  hostName: string;
  postId?: number;
  postAttributeId?: number;
  commentId?: number;
  commentAttributeId?: number;  
}