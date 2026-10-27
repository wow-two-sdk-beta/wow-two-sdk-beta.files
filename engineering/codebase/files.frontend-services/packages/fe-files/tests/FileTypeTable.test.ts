import { describe, expect, it } from 'vitest';
import registry from '../../../../files.backend-services/packages/Formats/Registry/file-types.json';
import { fileTypeRows } from '../src/icons';

describe('file type table', () => {
  it('matches the be registry row for row', () => {
    const expected = registry.types.map((type) => [type.ext, type.kind, type.label, type.mime]);

    expect(fileTypeRows).toEqual(expected);
  });
});
