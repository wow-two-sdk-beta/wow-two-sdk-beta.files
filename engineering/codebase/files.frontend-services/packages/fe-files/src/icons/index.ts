export { default as FileIcon } from './FileIcon.vue';
export {
  fileIconFold,
  fileIconGlyphTransform,
  fileIconLabelLength,
  fileIconLabelSize,
  fileIconPage,
  renderFileIconSvg,
  renderFileKindIconSvg,
  type FileIconSvgOptions,
} from './FileIconDrawing';
export { extensionOf, fileKind, fileTypeOf, fileTypes, type FileTypeInfo, type FileTypeQuery } from './FileTypes';
export { fileKindVisuals, type FileKindVisual } from './FileKindVisuals';
export { fileTypeRows, type FileTypeRow } from './FileTypeTable';
export type { FileKind } from '../contracts/FileKind';
