-- Migration 001 — Sync overhaul
-- ---------------------------------------------------------------------------
-- Adds conflict-safe tracking (client_modified_date, version), soft-delete
-- provenance (is_deleted, deleted_date, deleted_by_device), a device registry,
-- and a sync audit log.
--
-- Apply as an admin user (the app user has no DDL grant):
--     mysql -u root -p yeplist < 001_sync_overhaul.sql
--
-- Safe to re-run: every statement is guarded so a second apply is a no-op.
-- Requires MySQL 8.0+ (uses IF NOT EXISTS on ADD COLUMN / ADD INDEX).

USE yeplist;

-- --- todo_list -------------------------------------------------------------
ALTER TABLE todo_list
    ADD COLUMN IF NOT EXISTS version INT NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS client_modified_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS deleted_date DATETIME NULL,
    ADD COLUMN IF NOT EXISTS deleted_by_device VARCHAR(100) NULL,
    ADD INDEX IF NOT EXISTS idx_list_modified_date (modified_date),
    ADD INDEX IF NOT EXISTS idx_list_deleted (is_deleted, deleted_date);

-- Backfill the conflict arbiter so the >= guard works for pre-existing rows.
UPDATE todo_list SET client_modified_date = modified_date
    WHERE client_modified_date IS NULL OR client_modified_date < modified_date;

-- --- category --------------------------------------------------------------
ALTER TABLE category
    ADD COLUMN IF NOT EXISTS version INT NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS client_modified_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS deleted_date DATETIME NULL,
    ADD COLUMN IF NOT EXISTS deleted_by_device VARCHAR(100) NULL,
    ADD INDEX IF NOT EXISTS idx_category_modified_date (modified_date),
    ADD INDEX IF NOT EXISTS idx_category_deleted (is_deleted, deleted_date);

UPDATE category SET client_modified_date = modified_date
    WHERE client_modified_date IS NULL OR client_modified_date < modified_date;

-- --- todo_item -------------------------------------------------------------
ALTER TABLE todo_item
    ADD COLUMN IF NOT EXISTS version INT NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS client_modified_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS deleted_date DATETIME NULL,
    ADD COLUMN IF NOT EXISTS deleted_by_device VARCHAR(100) NULL,
    ADD INDEX IF NOT EXISTS idx_item_deleted (is_deleted, deleted_date);

UPDATE todo_item SET client_modified_date = modified_date
    WHERE client_modified_date IS NULL OR client_modified_date < modified_date;

-- Carry forward any historical tombstones into the new in-place model so that
-- already-deleted entities aren't resurrected. (Rows are gone, so we can only
-- record the deletion against rows that still exist; orphan tombstones are
-- harmless and ignored.)
UPDATE todo_item   i JOIN deleted_entity d ON d.entity_type = 'TodoItem' AND d.entity_id = i.item_id
    SET i.is_deleted = 1, i.deleted_date = d.deleted_date WHERE i.is_deleted = 0;
UPDATE todo_list   l JOIN deleted_entity d ON d.entity_type = 'TodoList' AND d.entity_id = l.list_id
    SET l.is_deleted = 1, l.deleted_date = d.deleted_date WHERE l.is_deleted = 0;
UPDATE category    c JOIN deleted_entity d ON d.entity_type = 'Category' AND d.entity_id = c.category_id
    SET c.is_deleted = 1, c.deleted_date = d.deleted_date WHERE c.is_deleted = 0;

-- --- new tables ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS device (
    device_id       VARCHAR(64) PRIMARY KEY,
    name            VARCHAR(100) NOT NULL,
    platform        VARCHAR(50) NULL,
    first_seen      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_seen       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS sync_log (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    device_id       VARCHAR(64) NULL,
    device_name     VARCHAR(100) NULL,
    action          VARCHAR(30) NOT NULL,
    entity_type     VARCHAR(30) NULL,
    entity_id       BIGINT NULL,
    since_value     DATETIME NULL,
    lists_count     INT NULL,
    items_count     INT NULL,
    categories_count INT NULL,
    deleted_count   INT NULL,
    result          VARCHAR(20) NULL,
    detail          VARCHAR(500) NULL,
    INDEX idx_sync_log_created (created_at),
    INDEX idx_sync_log_device (device_id),
    INDEX idx_sync_log_action (action)
) ENGINE=InnoDB;
