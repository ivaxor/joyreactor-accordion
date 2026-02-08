import { SearchMediaResponse } from "../search-media-service/search-media-response";

export interface SearchMediaHistoryRecord {
  id?: number,
  url?: string,
  fileName?: string,
  results: SearchMediaResponse[],
  createdAt: Date,
}