CREATE TABLE IF NOT EXISTS audit_logs (
  id uuid NOT NULL PRIMARY KEY,
  created_at_utc timestamptz NOT NULL DEFAULT NOW(),
  actor_user_id uuid NULL,
  action varchar(200) NOT NULL,
  entity_type varchar(100) NULL,
  entity_id uuid NULL,
  metadata_json jsonb NULL,
  correlation_id varchar(64) NULL
);
CREATE INDEX IF NOT EXISTS IX_audit_logs_action ON audit_logs(action);
CREATE INDEX IF NOT EXISTS IX_audit_logs_correlation_id ON audit_logs(correlation_id);
CREATE INDEX IF NOT EXISTS IX_audit_logs_created_at_utc ON audit_logs(created_at_utc);
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260218000000_AddAuditLogs', '8.0.11') ON CONFLICT DO NOTHING;
