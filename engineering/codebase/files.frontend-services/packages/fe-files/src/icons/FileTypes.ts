import type { FileKind } from '../contracts/FileKind';
import { fileTypeRows } from './FileTypeTable';

/** Holds what the registry says about one file type. */
export interface FileTypeInfo {
  /** The extension, lowercase without the dot; empty when the file has none. */
  readonly ext: string;
  readonly kind: FileKind;
  /** The short text the icon shows. */
  readonly label: string;
  /** The content type Files stores and serves the file as. */
  readonly mime: string;
}

/** Holds what identifies a file: its name, its extension or its content type, tried in that order. */
export interface FileTypeQuery {
  readonly name?: string | null;
  readonly ext?: string | null;
  readonly mime?: string | null;
}

const toInfo = ([ext, kind, label, mime]: (typeof fileTypeRows)[number]): FileTypeInfo => ({ ext, kind, label, mime });
const byExt = new Map(fileTypeRows.map((row) => [row[0], toInfo(row)]));
const byMime = new Map<string, FileTypeInfo>();
for (const row of fileTypeRows) if (!byMime.has(row[3])) byMime.set(row[3], toInfo(row));

/** Returns a name's extension: the text after the last dot of its last path part, lowercase, without the dot. */
export function extensionOf(name: string | null | undefined): string {
  const lastPart = (name ?? '').split(/[/\\]/).pop() ?? '';
  const dot = lastPart.lastIndexOf('.');
  return dot < 0 || dot === lastPart.length - 1 ? '' : lastPart.slice(dot + 1).toLowerCase();
}

/** Returns a file's type from the registry; an unknown file is generic and labelled with its own extension. */
export function fileTypeOf(query: FileTypeQuery): FileTypeInfo {
  const ext = (query.ext ?? extensionOf(query.name)).replace(/^\./, '').toLowerCase();
  const known = ext ? byExt.get(ext) : undefined;
  if (known) return known;

  const mime = query.mime?.split(';')[0]?.trim().toLowerCase();
  const byType = !ext && mime ? byMime.get(mime) : undefined;
  return byType ?? { ext, kind: 'generic', label: ext.toUpperCase().slice(0, 8), mime: 'application/octet-stream' };
}

/** Returns a file's kind, as `fileTypeOf` resolves it. */
export function fileKind(query: FileTypeQuery): FileKind {
  return fileTypeOf(query).kind;
}

/** Returns every registry type, in registry order. */
export function fileTypes(): readonly FileTypeInfo[] {
  return [...byExt.values()];
}
