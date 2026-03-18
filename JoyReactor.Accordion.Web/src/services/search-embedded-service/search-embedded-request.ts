export interface SearchEmbeddedRequest {
  type: SearchEmbeddedType,
  text: string,
  limit: number,
}

export enum SearchEmbeddedType {
  BandCamp,
  Coub,
  SoundCloud,
  Vimeo,
  YouTube,
}