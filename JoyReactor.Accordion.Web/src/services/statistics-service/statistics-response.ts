export interface StatisticsResponse {
  vectors: number,

  parsedTags: number,
  emptyTags: number,

  parsedPosts: number,

  parsedPostAttributePictures: number,
  parsedPostAttributePicturesNoContent: number,
  parsedPostAttributePicturesNoContentDueToDns: number,
  parsedPostAttributePicturesUnsupported: number,
  parsedPostAttributePicturesWithVector: number,
  parsedPostAttributePicturesWithoutVector: number,
  parsedPostAttributePicturesCheckedForDuplicates: number,

  parsedPostAttributeEmbeds: number,

  parsedBandCamps: number,
  parsedCoubs: number,
  parsedSoundClouds: number,
  parsedVimeos: number,
  parsedYouTubes: number,
}