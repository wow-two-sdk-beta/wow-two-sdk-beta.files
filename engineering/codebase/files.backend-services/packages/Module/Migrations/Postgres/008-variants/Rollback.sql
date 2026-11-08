-- Inverts 008-variants — drops every variant row. Dev/test recovery only: the files stay, and the sweep purges the
-- variants' bytes once no file uses them.

DROP INDEX IF EXISTS ix_files_variants_tenant_blob;
DROP TABLE IF EXISTS files_variants;
