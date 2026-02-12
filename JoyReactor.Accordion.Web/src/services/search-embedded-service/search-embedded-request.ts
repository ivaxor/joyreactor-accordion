export interface SearchEmbeddedRequest {
  type: SearchEmbeddedType,
  text: string,
}

export enum SearchEmbeddedType {
  BandCamp,
  Coub,
  SoundCloud,
  Vimeo,
  YouTube,
}