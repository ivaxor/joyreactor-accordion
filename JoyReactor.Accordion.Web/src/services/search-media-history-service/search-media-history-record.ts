import { SearchResponse } from "../search-service/search-response";

export interface SearchMediaHistoryRecord {
  id?: number;
  url?: string;
  fileName?: string;
  results: SearchResponse[];
  createdAt: Date;
}