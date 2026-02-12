import { SearchEmbeddedType } from '../search-embedded-service/search-embedded-request';

export interface SearchEmbeddedHistoryRecord {
  id?: number,
  type: SearchEmbeddedType,
  text: string,
  postIds: number[],
  createdAt: Date,
}