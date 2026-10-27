import { describe, expect, it } from 'vitest';
import { fileKindVisuals, fileTypeRows } from '@wow-two-beta/fe-files/icons';
import { fileIconFiles } from '../vite.icons';

describe('icon files', () => {
  const files = fileIconFiles();

  it('ships one labelled icon per registry extension and one per kind', () => {
    expect(files.size).toBe(fileTypeRows.length + Object.keys(fileKindVisuals).length);
    expect(files.get('icons/pdf.svg')).toContain('>PDF</text>');
    expect(files.get('icons/kinds/generic.svg')).not.toContain('<text');
  });

  it('writes standalone svg documents', () => {
    for (const svg of files.values()) expect(svg).toMatch(/^<svg xmlns="http:\/\/www\.w3\.org\/2000\/svg" .*<\/svg>$/);
  });
});
