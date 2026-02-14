# JoyReactor.Accordion

## Development dependencies
- [VS 2026](https://visualstudio.microsoft.com)
- [VS Code](https://code.visualstudio.com)
- [Node.js](https://nodejs.org/en/download)
- [FFMpeg](https://www.ffmpeg.org/download.html)

   Windows installation via winget:
   ``` ps1
   winget install ffmpeg
   ```
- Fuckton of free time and CPU/GPU/IO resources

## Documentation

### GraphQL
- [joyreactor.cc](https://api.joyreactor.cc/graphql-playground)
- [joyreactor.com](https://api.joyreactor.com/graphql-playground)

### Media
Examples of post and comment media URLs using only `PostAttributeId`:
- img0.joyreactor.cc/pics/post/picture-[postAttributeId].[extension]
- img0.joyreactor.cc/pics/post/full/picture-[postAttributeId].[extension]
- img0.joyreactor.cc/pics/comment/picture-[postAttributeId].[extension]
- img0.joyreactor.cc/pics/comment/full/picture-[postAttributeId].[extension]

Examples of post and comment media URLs:
- img10.joyreactor.cc/pics/post/[postTags]-[postAttributeId].[extension]
- img10.joyreactor.cc/pics/post/full/[postTags]-[postAttributeId].[extension]
- img10.joyreactor.cc/pics/comment/[postTags]-[postAttributeId].[extension]
- img10.joyreactor.cc/pics/comment/full/[postTags]-[postAttributeId].[extension]

Examples of post video media and static preview URLS:
- img10.joyreactor.cc/pics/post/webm/[postTags]-[postAttributeId].webm
- img10.joyreactor.cc/pics/post/mp4/[postTags]-[postAttributeId].mp4
- img10.joyreactor.cc/pics/post/static/[postTags]-[postAttributeId].jpeg

## Notes
- CC and COM domains looks similar, but are mostly independent.
- Posts seems to be synced only if both side have at least single common tag, so there are posts unique for specific domain.
- Media CDN servers are agnostic to unique post media. For example, CC unique post media could be downloaded via img10.joyreactor.com.
- There are multiple Media CDN servers, but they seems to have identical data set. Why two posts in the same page are referencing different CDNs to be found.
- Media extension names doesn't really matter. For example, `webm` video can be downloaded using `png` extension and even empty extension (just a `.`) will be valid.
- Post comments are unique per domain.
- `общее` and `general` tags are representing main feed on CC and COM domains respectfully.
- Seems like main feed could contain posts with any tags as long they are not from `секретные разделы` or `xxx-files`.
- "Linked" tags could or could not have identical name, but are most likely to have similar meaning in different languages.
- Tag tree/hierarchy is different even for the "linked" tags. For example,  `секретные разделы` is root tag, but `xxx-files` tag is under `fandoms`.