import type { FileKind } from './FileKind';

/** Refers to who may read a file. */
export type FileVisibility = 'private' | 'organization' | 'public';

/** Refers to where a file is between upload and use. */
export type FileState = 'uploading' | 'pending' | 'ready' | 'rejected' | 'quarantined';

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

/** Holds a short-lived signed URL to a private file on the content host. */
export interface SignedFileUrl {
  readonly url: string;
  readonly expiresAtUtc: string;
}
