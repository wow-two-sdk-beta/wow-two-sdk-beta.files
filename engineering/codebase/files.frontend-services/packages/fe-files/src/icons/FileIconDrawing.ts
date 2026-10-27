import type { FileKind } from '../contracts/FileKind';
import { fileKindVisuals } from './FileKindVisuals';
import { fileTypeOf, type FileTypeQuery } from './FileTypes';

/** Holds the page outline, in the icon's 32-unit box. */
export const fileIconPage = 'M8 2h12l7 7v19a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2z';

/** Holds the page's folded corner. */
export const fileIconFold = 'M20 2v5a2 2 0 0 0 2 2h5';

/** Holds the smallest edge, in pixels, that shows the extension label. */
export const fileIconLabelSize = 24;

/** Returns where the kind's glyph sits: raised over the label band when the label shows, centred otherwise. */
export function fileIconGlyphTransform(labelled: boolean): string {
  return labelled ? 'translate(10 6) scale(0.5)' : 'translate(9 10) scale(0.5833)';
}

/** Returns the text length that squeezes a label of five or more characters into the band; undefined otherwise. */
export function fileIconLabelLength(label: string): number | undefined {
  return label.length > 4 ? 23 : undefined;
}

/** Holds how a standalone icon draws. */
export interface FileIconSvgOptions {
  /** The edge in pixels the markup declares; 32 when unset. */
  readonly size?: number;
  /** Shows the extension label; from 24 px when unset. */
  readonly label?: boolean;
}

/**
 * Returns a file's icon as standalone SVG markup in its kind's colour, for icons used outside Vue: the files the Files
 * service serves under `/icons`, emails, server-rendered pages.
 */
export function renderFileIconSvg(query: FileTypeQuery, options: FileIconSvgOptions = {}): string {
  const type = fileTypeOf(query);
  const size = options.size ?? 32;
  const label = (options.label ?? size >= fileIconLabelSize) ? type.label : '';
  return draw(type.kind, label, size, type.label ? `${type.label} file` : 'File');
}

/** Returns a kind's icon as standalone SVG markup, without a label, for small sizes or a file known by its kind only. */
export function renderFileKindIconSvg(kind: FileKind, size = 32): string {
  return draw(kind, '', size, 'File');
}

function draw(kind: FileKind, label: string, size: number, title: string): string {
  const { color, glyph } = fileKindVisuals[kind];
  const stroke = `fill="none" stroke="${color}" stroke-linejoin="round"`;
  const length = fileIconLabelLength(label);
  return [
    `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 32 32" role="img" aria-label="${escape(title)}">`,
    `<path d="${fileIconPage}" fill="${color}" fill-opacity="0.1" stroke="${color}" stroke-width="1.25" stroke-linejoin="round"/>`,
    `<path d="${fileIconFold}" ${stroke} stroke-width="1.25"/>`,
    `<path d="${glyph}" transform="${fileIconGlyphTransform(label.length > 0)}" ${stroke} stroke-width="2" stroke-linecap="round"/>`,
    label
      ? `<rect x="3" y="19.5" width="26" height="8" rx="1.5" fill="${color}"/>` +
        `<text x="16" y="25.6" text-anchor="middle" font-family="ui-sans-serif, system-ui, sans-serif" font-size="6.5" ` +
        `font-weight="700" fill="#fff"${length ? ` textLength="${length}" lengthAdjust="spacingAndGlyphs"` : ''}>` +
        `${escape(label)}</text>`
      : '',
    '</svg>',
  ].join('');
}

function escape(text: string): string {
  return text.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
}
