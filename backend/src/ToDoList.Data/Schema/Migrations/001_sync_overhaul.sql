-- Migration 001 — Sync overhaul
-- ---------------------------------------------------------------------------
-- Adds conflict-safe tracking (client_modified_date, version), soft-delete
-- provenance (is_deleted, deleted_date, deleted_by_device), a device registry,
-- and a sync audit log.
--
-- Apply as an admin user (the app user has no DDL grant):
--     mysql -u root -p yeplist < 001_sync_overhaul.sql
--
-- Safe to re-run: every change is guarded, so a second apply is a no-op.
-- Requires MySQL 8.0+. (MySQL has no ADD COLUMN/INDEX IF NOT EXISTS, so the
-- guards are done with information_schema checks via helper procedures.)

USE yeplist;

-- --- idempotent-DDL helpers ------------------------------------------------
DROP PROCEDURE IF EXISTS yl_add_column;
DROP PROCEDURE IF EXISTS yl_add_index;
DROP PROCEDURE IF EXISTS yl_carry_tombstones;

DELIMITER $$

CREATE PROCEDURE yl_add_column(IN p_table VARCHAR(64), IN p_col VARCHAR(64), IN p_def TEXT)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = p_table
              AND COLUMN_NAME = p_col) THEN
        SET @ddl = CONCAT('ALTER TABLE `', p_table, '` ADD COLUMN `', p_col, '` ', p_def);
        PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
    END IF;
END$$

CREATE PROCEDURE yl_add_index(IN p_table VARCHAR(64), IN p_index VARCHAR(64), IN p_cols TEXT)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = p_table
              AND INDEX_NAME = p_index) THEN
        SET @ddl = CONCAT('ALTER TABLE `', p_table, '` ADD INDEX `', p_index, '` (', p_cols, ')');
        PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
    END IF;
END$$

-- Carry forward any historical tombstones into the new in-place model so that
-- already-deleted entities aren't resurrected. Guarded on deleted_entity still
-- existing (it is absent on fresh installs). Orphan tombstones (rows already
-- gone) are harmless and ignored.
CREATE PROCEDURE yl_carry_tombstones()
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'deleted_entity') THEN
        UPDATE todo_item i JOIN deleted_entity d
            ON d.entity_type = 'TodoItem' AND d.entity_id = i.item_id
            SET i.is_deleted = 1, i.deleted_date = d.deleted_date WHERE i.is_deleted = 0;
        UPDATE todo_list l JOIN deleted_entity d
            ON d.entity_type = 'TodoList' AND d.entity_id = l.list_id
            SET l.is_deleted = 1, l.deleted_date = d.deleted_date WHERE l.is_deleted = 0;
        UPDATE category c JOIN deleted_entity d
            ON d.entity_type = 'Category' AND d.entity_id = c.category_id
            SET c.is_deleted = 1, c.deleted_date = d.deleted_date WHERE c.is_deleted = 0;
    END IF;
END$$

DELIMITER ;

-- --- todo_list -------------------------------------------------------------
CALL yl_add_column('todo_list', 'version', 'INT NOT NULL DEFAULT 1');
CALL yl_add_column('todo_list', 'client_modified_date', 'DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP');
CALL yl_add_column('todo_list', 'is_deleted', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL yl_add_column('todo_list', 'deleted_date', 'DATETIME NULL');
CALL yl_add_column('todo_list', 'deleted_by_device', 'VARCHAR(100) NULL');
CALL yl_add_index('todo_list', 'idx_list_modified_date', 'modified_date');
CALL yl_add_index('todo_list', 'idx_list_deleted', 'is_deleted, deleted_date');

-- Backfill the conflict arbiter so the >= guard works for pre-existing rows.
UPDATE todo_list SET client_modified_date = modified_date
    WHERE client_modified_date IS NULL OR client_modified_date < modified_date;

-- --- category --------------------------------------------------------------
CALL yl_add_column('category', 'version', 'INT NOT NULL DEFAULT 1');
CALL yl_add_column('category', 'client_modified_date', 'DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP');
CALL yl_add_column('category', 'is_deleted', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL yl_add_column('category', 'deleted_date', 'DATETIME NULL');
CALL yl_add_column('category', 'deleted_by_device', 'VARCHAR(100) NULL');
CALL yl_add_index('category', 'idx_category_modified_date', 'modified_date');
CALL yl_add_index('category', 'idx_category_deleted', 'is_deleted, deleted_date');

UPDATE category SET client_modified_date = modified_date
    WHERE client_modified_date IS NULL OR client_modified_date < modified_date;

-- --- todo_item -------------------------------------------------------------
CALL yl_add_column('todo_item', 'version', 'INT NOT NULL DEFAULT 1');
CALL yl_add_column('todo_item', 'client_modified_date', 'DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP');
CALL yl_add_column('todo_item', 'is_deleted', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL yl_add_column('todo_item', 'deleted_date', 'DATETIME NULL');
CALL yl_add_column('todo_item', 'deleted_by_device', 'VARCHAR(100) NULL');
CALL yl_add_index('todo_item', 'idx_item_deleted', 'is_deleted, deleted_date');

UPDATE todo_item SET client_modified_date = modified_date
    WHERE client_modified_date IS NULL OR client_modified_date < modified_date;

-- --- historical tombstone carry-forward ------------------------------------
CALL yl_carry_tombstones();

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

-- --- clean up helpers ------------------------------------------------------
DROP PROCEDURE IF EXISTS yl_add_column;
DROP PROCEDURE IF EXISTS yl_add_index;
DROP PROCEDURE IF EXISTS yl_carry_tombstones;
