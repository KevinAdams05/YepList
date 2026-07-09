-- YepList Database Schema
-- MySQL 8.0+
-- Run: mysql -u root -p < init.sql
--
-- This script creates the schema only. Provision the application user
-- out-of-band — see README. Example (run as root, choose a real password):
--
--   CREATE USER 'yepapp'@'localhost' IDENTIFIED BY '<strong-password>';
--   GRANT SELECT, INSERT, UPDATE, DELETE ON yeplist.* TO 'yepapp'@'localhost';
--   FLUSH PRIVILEGES;
--
-- Use 'localhost' (or the actual API host), not '%'. Grant only the four
-- DML verbs — the app never issues DDL. (DDL changes ship as admin-applied
-- migration scripts under Schema/Migrations — see that folder's README.)

CREATE DATABASE IF NOT EXISTS yeplist
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE yeplist;

-- -----------------------------------------------------------
-- Tables
-- -----------------------------------------------------------
--
-- Sync/tracking columns shared by todo_list, category, todo_item:
--   modified_date        server receipt time (auto). The pull cursor: a
--                        client asks for everything changed `since` this.
--   client_modified_date the real time the *user* made the edit, supplied by
--                        the client. The conflict arbiter: a write is applied
--                        only if its client_modified_date >= the stored one,
--                        so a stale edit pushed late can't clobber a newer one.
--   version              monotonic per-row counter, bumped on each applied
--                        write. Tracking/diagnostics only (not the arbiter).
--   is_deleted           soft-delete flag. Deleted rows are retained.
--   deleted_date         when the soft-delete happened.
--   deleted_by_device    which device deleted it (X-Device-Name).

CREATE TABLE IF NOT EXISTS todo_list (
    list_id             BIGINT AUTO_INCREMENT PRIMARY KEY,
    name                VARCHAR(200) NOT NULL,
    sort_order          INT NOT NULL DEFAULT 0,
    version             INT NOT NULL DEFAULT 1,
    created_date        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_date       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    client_modified_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted          TINYINT(1) NOT NULL DEFAULT 0,
    deleted_date        DATETIME NULL,
    deleted_by_device   VARCHAR(100) NULL,
    INDEX idx_list_modified_date (modified_date),
    INDEX idx_list_deleted (is_deleted, deleted_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS category (
    category_id         BIGINT AUTO_INCREMENT PRIMARY KEY,
    name                VARCHAR(100) NOT NULL,
    color               VARCHAR(7),
    version             INT NOT NULL DEFAULT 1,
    created_date        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_date       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    client_modified_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted          TINYINT(1) NOT NULL DEFAULT 0,
    deleted_date        DATETIME NULL,
    deleted_by_device   VARCHAR(100) NULL,
    INDEX idx_category_modified_date (modified_date),
    INDEX idx_category_deleted (is_deleted, deleted_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS todo_item (
    item_id             BIGINT AUTO_INCREMENT PRIMARY KEY,
    list_id             BIGINT NOT NULL,
    category_id         BIGINT NULL,
    title               VARCHAR(500) NOT NULL,
    notes               TEXT,
    is_completed        TINYINT(1) NOT NULL DEFAULT 0,
    due_date            DATE NULL,
    sort_order          INT NOT NULL DEFAULT 0,
    version             INT NOT NULL DEFAULT 1,
    created_date        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_date       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    client_modified_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted          TINYINT(1) NOT NULL DEFAULT 0,
    deleted_date        DATETIME NULL,
    deleted_by_device   VARCHAR(100) NULL,
    CONSTRAINT fk_item_list FOREIGN KEY (list_id) REFERENCES todo_list(list_id) ON DELETE CASCADE,
    CONSTRAINT fk_item_category FOREIGN KEY (category_id) REFERENCES category(category_id) ON DELETE SET NULL,
    INDEX idx_item_list_id (list_id),
    INDEX idx_item_category_id (category_id),
    INDEX idx_item_modified_date (modified_date),
    INDEX idx_item_deleted (is_deleted, deleted_date)
) ENGINE=InnoDB;

-- Legacy tombstone table. Soft-delete is now tracked in-place via the
-- is_deleted columns above; this table is retained only so older clients /
-- historical rows keep working and is no longer written to.
CREATE TABLE IF NOT EXISTS deleted_entity (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    entity_type     VARCHAR(50) NOT NULL,
    entity_id       BIGINT NOT NULL,
    deleted_date    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_deleted_entity_date (deleted_date)
) ENGINE=InnoDB;

-- Known sync devices. Upserted by the API on every request that carries an
-- X-Device-Id header, so the log viewer can show friendly device names.
CREATE TABLE IF NOT EXISTS device (
    device_id       VARCHAR(64) PRIMARY KEY,
    name            VARCHAR(100) NOT NULL,
    platform        VARCHAR(50) NULL,
    first_seen      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_seen       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- Sync/diagnostics audit trail. One row per pull and per write, so you can
-- see what was refreshed, when, and from which device.
CREATE TABLE IF NOT EXISTS sync_log (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    device_id       VARCHAR(64) NULL,
    device_name     VARCHAR(100) NULL,
    action          VARCHAR(30) NOT NULL,      -- pull | create | update | toggle | delete | reorder
    entity_type     VARCHAR(30) NULL,          -- TodoList | TodoItem | Category
    entity_id       BIGINT NULL,
    since_value     DATETIME NULL,             -- the ?since used on a pull
    lists_count     INT NULL,
    items_count     INT NULL,
    categories_count INT NULL,
    deleted_count   INT NULL,
    result          VARCHAR(20) NULL,          -- applied | stale | notfound | ok
    detail          VARCHAR(500) NULL,
    INDEX idx_sync_log_created (created_at),
    INDEX idx_sync_log_device (device_id),
    INDEX idx_sync_log_action (action)
) ENGINE=InnoDB;

-- -----------------------------------------------------------
-- Seed data
-- -----------------------------------------------------------

INSERT INTO todo_list (name, sort_order) VALUES ('My Tasks', 0);
