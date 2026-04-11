-- ToDoList Database Schema
-- MySQL 8.0+
-- Run: mysql -u root -p < init.sql

CREATE DATABASE IF NOT EXISTS todolist
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE todolist;

-- Create application user
CREATE USER IF NOT EXISTS 'todoapp'@'%' IDENTIFIED BY 'changeme';
GRANT ALL PRIVILEGES ON todolist.* TO 'todoapp'@'%';
FLUSH PRIVILEGES;

-- -----------------------------------------------------------
-- Tables
-- -----------------------------------------------------------

CREATE TABLE IF NOT EXISTS todo_list (
    list_id         BIGINT AUTO_INCREMENT PRIMARY KEY,
    name            VARCHAR(200) NOT NULL,
    sort_order      INT NOT NULL DEFAULT 0,
    created_date    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_date   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS category (
    category_id     BIGINT AUTO_INCREMENT PRIMARY KEY,
    name            VARCHAR(100) NOT NULL,
    color           VARCHAR(7),
    created_date    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_date   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS todo_item (
    item_id         BIGINT AUTO_INCREMENT PRIMARY KEY,
    list_id         BIGINT NOT NULL,
    category_id     BIGINT NULL,
    title           VARCHAR(500) NOT NULL,
    notes           TEXT,
    is_completed    TINYINT(1) NOT NULL DEFAULT 0,
    due_date        DATE NULL,
    sort_order      INT NOT NULL DEFAULT 0,
    created_date    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_date   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_item_list FOREIGN KEY (list_id) REFERENCES todo_list(list_id) ON DELETE CASCADE,
    CONSTRAINT fk_item_category FOREIGN KEY (category_id) REFERENCES category(category_id) ON DELETE SET NULL,
    INDEX idx_item_list_id (list_id),
    INDEX idx_item_category_id (category_id),
    INDEX idx_item_modified_date (modified_date)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS deleted_entity (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    entity_type     VARCHAR(50) NOT NULL,
    entity_id       BIGINT NOT NULL,
    deleted_date    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_deleted_entity_date (deleted_date)
) ENGINE=InnoDB;

-- -----------------------------------------------------------
-- Seed data
-- -----------------------------------------------------------

INSERT INTO todo_list (name, sort_order) VALUES ('My Tasks', 0);
