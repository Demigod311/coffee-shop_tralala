-- =============================================================================
-- schema.sql — Coffee Shop POS Database Schema
-- =============================================================================
-- This script creates all tables required by the Coffee Shop POS system.
-- Run this script in MySQL Workbench or via command line before starting the app.
--
-- Tables created:
--   users          — Staff accounts (connects to UserDAL.cs)
--   categories     — Menu categories (connects to CategoryDAL.cs)
--   menu_items     — Individual menu products (connects to MenuItemDAL.cs)
--   tables         — Restaurant tables (connects to TableDAL.cs)
--   orders         — Customer orders (connects to OrderDAL.cs)
--   order_items    — Line items in each order (connects to OrderDAL.cs)
--   inventory      — Stock/ingredient tracking (connects to InventoryDAL.cs)
--   inventory_log  — Audit trail for stock changes (connects to InventoryDAL.cs)
-- =============================================================================

-- Create the database if it doesn't exist
CREATE DATABASE IF NOT EXISTS coffee_shop_pos
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE coffee_shop_pos;

-- =============================================================================
-- USERS / STAFF TABLE
-- Stores staff accounts with hashed passwords and role-based access.
-- Referenced by: orders (user_id), inventory_log (changed_by)
-- Used by: UserDAL.cs, AuthService.cs
-- =============================================================================
CREATE TABLE IF NOT EXISTS users (
    user_id       INT AUTO_INCREMENT PRIMARY KEY,
    username      VARCHAR(50) UNIQUE NOT NULL,        -- Login username, must be unique
    password_hash VARCHAR(255) NOT NULL,               -- BCrypt hash — never store plain text
    full_name     VARCHAR(100) NOT NULL,               -- Display name for receipts/reports
    role          ENUM('Admin','Manager','Cashier','Waiter') NOT NULL,  -- Determines UI access
    is_active     TINYINT(1) DEFAULT 1,                -- Soft delete flag (0 = deactivated)
    created_at    DATETIME DEFAULT CURRENT_TIMESTAMP   -- Auto-set on insert
) ENGINE=InnoDB;

-- =============================================================================
-- CATEGORIES TABLE
-- Groups menu items (e.g., "Hot Drinks", "Cold Drinks", "Pastries")
-- Referenced by: menu_items (category_id)
-- Used by: CategoryDAL.cs
-- =============================================================================
CREATE TABLE IF NOT EXISTS categories (
    category_id   INT AUTO_INCREMENT PRIMARY KEY,
    name          VARCHAR(100) NOT NULL,                -- Category display name
    description   VARCHAR(255),                         -- Optional description
    is_active     TINYINT(1) DEFAULT 1                  -- Soft delete flag
) ENGINE=InnoDB;

-- =============================================================================
-- MENU ITEMS TABLE
-- Individual products available for sale.
-- References: categories (category_id)
-- Referenced by: order_items (item_id)
-- Used by: MenuItemDAL.cs
-- =============================================================================
CREATE TABLE IF NOT EXISTS menu_items (
    item_id       INT AUTO_INCREMENT PRIMARY KEY,
    category_id   INT NOT NULL,                         -- FK to categories table
    name          VARCHAR(100) NOT NULL,                 -- Item display name
    description   VARCHAR(255),                          -- Optional description
    price         DECIMAL(10,2) NOT NULL,                -- Sale price
    is_available  TINYINT(1) DEFAULT 1,                  -- Can be toggled off when out of stock
    image_path    VARCHAR(255),                           -- Path to item image (optional)
    created_at    DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category_id) REFERENCES categories(category_id)
        ON UPDATE CASCADE ON DELETE RESTRICT             -- Prevent deleting categories with items
) ENGINE=InnoDB;

-- =============================================================================
-- TABLES (Restaurant seating)
-- Tracks table availability for dine-in orders.
-- Referenced by: orders (table_id)
-- Used by: TableDAL.cs
-- =============================================================================
CREATE TABLE IF NOT EXISTS tables (
    table_id      INT AUTO_INCREMENT PRIMARY KEY,
    table_number  VARCHAR(10) UNIQUE NOT NULL,           -- e.g., "T1", "T2", "Patio-1"
    capacity      INT NOT NULL,                           -- Number of seats
    status        ENUM('Available','Occupied','Reserved') DEFAULT 'Available'
) ENGINE=InnoDB;

-- =============================================================================
-- ORDERS TABLE
-- Each order record with payment and status tracking.
-- References: tables (table_id), users (user_id)
-- Referenced by: order_items (order_id)
-- Used by: OrderDAL.cs, OrderService.cs
-- =============================================================================
CREATE TABLE IF NOT EXISTS orders (
    order_id      INT AUTO_INCREMENT PRIMARY KEY,
    table_id      INT,                                    -- NULL for takeaway orders
    user_id       INT NOT NULL,                           -- FK to users — who placed the order
    order_type    ENUM('Dine-In','Takeaway') DEFAULT 'Dine-In',
    status        ENUM('Pending','Preparing','Served','Completed','Cancelled')
                  DEFAULT 'Pending',
    subtotal      DECIMAL(10,2) DEFAULT 0,                -- Sum of item prices * quantities
    tax           DECIMAL(10,2) DEFAULT 0,                -- Calculated tax amount
    discount      DECIMAL(10,2) DEFAULT 0,                -- Applied discount
    total         DECIMAL(10,2) DEFAULT 0,                -- subtotal + tax - discount
    payment_method ENUM('Cash','Card','Online') DEFAULT 'Cash',
    created_at    DATETIME DEFAULT CURRENT_TIMESTAMP,
    completed_at  DATETIME,                                -- Set when status = Completed
    FOREIGN KEY (table_id) REFERENCES tables(table_id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- =============================================================================
-- ORDER ITEMS TABLE
-- Line items linking orders to menu items with quantity and notes.
-- References: orders (order_id), menu_items (item_id)
-- Used by: OrderDAL.cs
-- =============================================================================
CREATE TABLE IF NOT EXISTS order_items (
    order_item_id INT AUTO_INCREMENT PRIMARY KEY,
    order_id      INT NOT NULL,                           -- FK to orders table
    item_id       INT NOT NULL,                           -- FK to menu_items table
    quantity      INT NOT NULL DEFAULT 1,                  -- How many of this item
    unit_price    DECIMAL(10,2) NOT NULL,                  -- Price at time of order (snapshot)
    notes         VARCHAR(255),                             -- e.g., "No sugar", "Extra hot"
    FOREIGN KEY (order_id) REFERENCES orders(order_id)
        ON UPDATE CASCADE ON DELETE CASCADE,               -- Delete items when order deleted
    FOREIGN KEY (item_id) REFERENCES menu_items(item_id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- =============================================================================
-- INVENTORY TABLE
-- Tracks raw materials / ingredients / supplies.
-- Referenced by: inventory_log (inventory_id)
-- Used by: InventoryDAL.cs, InventoryService.cs
-- =============================================================================
CREATE TABLE IF NOT EXISTS inventory (
    inventory_id  INT AUTO_INCREMENT PRIMARY KEY,
    name          VARCHAR(100) NOT NULL,                   -- e.g., "Coffee Beans", "Milk"
    unit          VARCHAR(20) NOT NULL,                     -- e.g., "kg", "liters", "pcs"
    quantity      DECIMAL(10,2) NOT NULL,                   -- Current stock level
    reorder_level DECIMAL(10,2) DEFAULT 10,                -- Alert threshold
    cost_per_unit DECIMAL(10,2),                             -- Cost for financial tracking
    last_updated  DATETIME DEFAULT CURRENT_TIMESTAMP
                  ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- =============================================================================
-- INVENTORY LOG TABLE
-- Audit trail for all stock changes (restock, usage, adjustments).
-- References: inventory (inventory_id), users (changed_by)
-- Used by: InventoryDAL.cs
-- =============================================================================
CREATE TABLE IF NOT EXISTS inventory_log (
    log_id        INT AUTO_INCREMENT PRIMARY KEY,
    inventory_id  INT NOT NULL,                            -- FK to inventory table
    change_qty    DECIMAL(10,2) NOT NULL,                  -- Positive = restock, Negative = usage
    reason        VARCHAR(255),                              -- e.g., "Order #45", "Manual restock"
    changed_by    INT,                                      -- FK to users — who made the change
    changed_at    DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (inventory_id) REFERENCES inventory(inventory_id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    FOREIGN KEY (changed_by) REFERENCES users(user_id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB;

-- =============================================================================
-- INDEXES for performance on commonly queried columns
-- =============================================================================
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created ON orders(created_at);
CREATE INDEX idx_orders_user ON orders(user_id);
CREATE INDEX idx_order_items_order ON order_items(order_id);
CREATE INDEX idx_menu_items_category ON menu_items(category_id);
CREATE INDEX idx_inventory_log_inventory ON inventory_log(inventory_id);
