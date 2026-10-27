import { describe, expect, it } from 'vitest';
import { FilesRouteConstants, FilesScopeConstants } from '../src/contracts';

describe('contracts', () => {
  it('mirrors the be routes and scopes', () => {
    expect(FilesRouteConstants.files).toBe('api/files/v1/files');
    expect(FilesRouteConstants.signedFile).toBe('f');
    expect(Object.values(FilesScopeConstants)).toContain('files.write');
  });
});
