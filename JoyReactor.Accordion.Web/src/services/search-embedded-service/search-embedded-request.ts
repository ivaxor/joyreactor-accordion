export interface SearchEmbeddedRequest {
  type: any,
  text: string,
}

export enum SearchEmbeddedType {
  BandCamp,
  Coub,
  SoundCloud,
  Vimeo,
  YouTube,
}