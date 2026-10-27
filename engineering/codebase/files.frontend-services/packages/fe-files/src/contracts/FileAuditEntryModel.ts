/** Refers to what an audited call did to a file. */
export type FileAuditAction = 'upload' | 'download' | 'update' | 'delete' | 'restore';

/** Refers to how an audited call ended. */
export type FileAuditOutcome = 'ok' | 'refused';

/** Holds one audited call on a tenant's file. */
export interface FileAuditEntryModel {
  readonly sequence: number;
  readonly fileId?: string;
  readonly action: FileAuditAction;
  readonly outcome: FileAuditOutcome;
  readonly errorCode?: string;
  readonly subjectId?: string;
  readonly appId?: string;
  readonly bytes?: number;
  readonly occurredAtUtc: string;
}

/** Holds one page of the audit trail, newest first. */
export interface FileAuditPage {
  readonly items: readonly FileAuditEntryModel[];
  readonly nextCursor?: string;
}
