import type { FileKind } from './FileKind';

/** Refers to who may read a file. */
export type FileVisibility = 'private' | 'organization' | 'public';

/** Refers to where a file is between upload and use. */
export type FileState = 'uploading' | 'pending' | 'ready' | 'rejected' | 'quarantined';

/** Holds one image variant of a file: a resized copy made on upload at one of the app policy's widths. */
export interface FileVariantModel {
  /** The width in pixels, one of the app policy's widths. */
  readonly width: number;
  /** The height in pixels, keeping the source's aspect ratio. */
  readonly height: number;
  /** The content type of the variant's bytes, such as `image/webp`. */
  readonly contentType: string;
  readonly size: number;
}

/** Holds one stored file's metadata, as the Files API returns it. */
export interface FileModel {
  readonly id: string;
  readonly tenantId: string;
  readonly organizationId?: string;
  readonly appId?: string;
  readonly ownerId?: string;
  readonly name: string;
  /** The content type the bytes were recognized as. */
  readonly contentType: string;
  readonly kind: FileKind;
  readonly size: number;
  readonly sha256: string;
  readonly visibility: FileVisibility;
  readonly state: FileState;
  readonly createdAtUtc: string;
  readonly expiresAtUtc?: string;
  readonly deletedAtUtc?: string;
  /** The image variants made on upload, narrowest first; empty for a file that is not a raster image. */
  readonly variants: readonly FileVariantModel[];
}

/** Holds one page of a file list, newest first. */
export interface FilePage {
  readonly items: readonly FileModel[];
  readonly nextCursor?: string;
}

/** Holds a record that uses a file; a file lives while any reference does. */
export interface FileReference {
  readonly recordType: string;
  readonly recordId: string;
}

/** Refers to whether a browser shows a served file or saves it. */
export type FileDisposition = 'inline' | 'attachment';

/** Describes a signed URL to issue. */
export interface UrlSpec {
  /** How long the URL stays valid, as a .NET `TimeSpan` (`00:05:00`); the instance caps it. */
  readonly lifetime?: string;
  readonly disposition?: FileDisposition;
  /** The width in pixels the file is shown at: the narrowest variant at least that wide serves, else the widest; the
   * original when none is asked or the file has no variants. */
  readonly width?: number;
}

/** Holds a short-lived signed URL to a private file on the content host. */
export interface SignedFileUrl {
  readonly url: string;
  readonly expiresAtUtc: string;
}
