import type { FileKind } from '../contracts/FileKind';

/** Holds how one kind draws: its colour and its glyph, stroked paths in a 24-unit box. */
export interface FileKindVisual {
  readonly color: string;
  readonly glyph: string;
}

/** Holds each kind's colour and glyph; a consumer recolours a kind with `--files-icon-{kind}`. */
export const fileKindVisuals: Readonly<Record<FileKind, FileKindVisual>> = {
  generic: { color: '#64748b', glyph: 'M7 9h10M7 13h10M7 17h6' },
  image: { color: '#0d9488', glyph: 'M4 5h16v14H4zM4 16l5-5 4 4 2-2 5 5M15.5 8.5h.01' },
  'raw-photo': { color: '#0f766e', glyph: 'M4 8h3l2-3h6l2 3h3v11H4zM12 10a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7' },
  vector: { color: '#059669', glyph: 'M5 18C8 6 16 6 19 18M3 18h4M17 18h4M12 4v4M10 6h4' },
  video: { color: '#7c3aed', glyph: 'M4 6h16v12H4zM10 9.5v5l4.5-2.5z' },
  audio: {
    color: '#db2777',
    glyph: 'M9 18V6l10-2v12M9 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0M19 16a2 2 0 1 1-4 0 2 2 0 0 1 4 0',
  },
  pdf: { color: '#dc2626', glyph: 'M6 19c2-1 4.5-5 5.5-10 .4-2 1.6-2 1.8 0 .5 5 3 8.5 4.7 9.5-3-1-8-1-12 .5' },
  document: { color: '#2563eb', glyph: 'M6 6h12M6 10h12M6 14h12M6 18h8' },
  spreadsheet: { color: '#16a34a', glyph: 'M4 5h16v14H4zM4 10h16M4 15h16M10 5v14' },
  presentation: { color: '#ea580c', glyph: 'M3 5h18M5 5v10h14V5M12 15v4M9 20l3-1 3 1M9 12v-2M12 12V8M15 12V9' },
  text: { color: '#475569', glyph: 'M5 6h14M5 10h10M5 14h14M5 18h8' },
  markdown: { color: '#334155', glyph: 'M3 17V7l4 4 4-4v10M17 7v10M14 14l3 3 3-3' },
  ebook: {
    color: '#92400e',
    glyph: 'M12 7c-2-1.5-5-2-8-2v13c3 0 6 .5 8 2 2-1.5 5-2 8-2V5c-3 0-6 .5-8 2zM12 7v13',
  },
  code: { color: '#4f46e5', glyph: 'M9 7l-5 5 5 5M15 7l5 5-5 5' },
  data: {
    color: '#ca8a04',
    glyph:
      'M9 5C7 5 6 6 6 8v2c0 1-1 2-2 2 1 0 2 1 2 2v2c0 2 1 3 3 3M15 5c2 0 3 1 3 3v2c0 1 1 2 2 2-1 0-2 1-2 2v2c0 2-1 3-3 3',
  },
  database: {
    color: '#0891b2',
    glyph:
      'M5 6c0-1.7 3.1-3 7-3s7 1.3 7 3-3.1 3-7 3-7-1.3-7-3zM5 6v12c0 1.7 3.1 3 7 3s7-1.3 7-3V6M5 12c0 1.7 3.1 3 7 3s7-1.3 7-3',
  },
  config: { color: '#57534e', glyph: 'M4 7h4M12 7h8M4 12h10M18 12h2M4 17h2M10 17h10M8 5v4M14 10v4M6 15v4' },
  archive: { color: '#b45309', glyph: 'M12 3v2M12 7v2M12 11v2M10 15h4v5h-4z' },
  'disk-image': { color: '#4b5563', glyph: 'M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16M12 10a2 2 0 1 0 0 4 2 2 0 0 0 0-4' },
  executable: { color: '#991b1b', glyph: 'M4 5h16v14H4zM7 10l3 2-3 2M12 15h5' },
  font: { color: '#9333ea', glyph: 'M5 19l7-14 7 14M8 14h8' },
  'model-3d': { color: '#6d28d9', glyph: 'M12 3l8 4.5v9L12 21l-8-4.5v-9zM4 7.5l8 4.5 8-4.5M12 12v9' },
  cad: { color: '#0369a1', glyph: 'M4 20V4l16 16zM8 16v-4l4 4zM4 8h2M4 12h2' },
  design: { color: '#c026d3', glyph: 'M12 3l7 10-3 7H8l-3-7zM12 3v8M12 11a2 2 0 1 0 0 4 2 2 0 0 0 0-4' },
  calendar: { color: '#e11d48', glyph: 'M4 6h16v14H4zM4 10h16M8 3v5M16 3v5' },
  certificate: { color: '#a16207', glyph: 'M8 10a4 4 0 1 0 0 8 4 4 0 0 0 0-8M11 11l8-8M16 6l3 3M14 8l2 2' },
  email: { color: '#0284c7', glyph: 'M3 6h18v12H3zM3 6l9 7 9-7' },
};
