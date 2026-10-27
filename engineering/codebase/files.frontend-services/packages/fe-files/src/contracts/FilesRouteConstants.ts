/** Holds the Files API's routes and the content host's route prefixes, as the be contract names them. */
export const FilesRouteConstants = {
  api: 'api/files/v1',
  files: 'api/files/v1/files',
  audit: 'api/files/v1/audit',
  signedFile: 'f',
  publicFile: 'm',
  idempotencyKeyHeader: 'Idempotency-Key',
  apiKeyHeader: 'X-Api-Key',
} as const;

/** Holds the OAuth scopes a Files instance accepts. */
export const FilesScopeConstants = {
  read: 'files.read',
  write: 'files.write',
  share: 'files.share',
  admin: 'files.admin',
  delegate: 'files.delegate',
  tenants: 'files.tenants',
} as const;
