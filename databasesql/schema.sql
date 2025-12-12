-- =====================================================
-- Database: AiBotOrderingDB
-- Engine: PostgreSQL
-- Purpose: WhatsApp AI Ordering Bot - Schema
-- =====================================================

BEGIN;

-- =========================
-- CATEGORIES
-- =========================
CREATE TABLE categories (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(150) NOT NULL,
    description     TEXT,
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMP DEFAULT now(),
    updated_at      TIMESTAMP DEFAULT now()
);

-- =========================
-- MENU ITEMS
-- =========================
CREATE TABLE menu_items (
    id              SERIAL PRIMARY KEY,
    category_id     INTEGER REFERENCES categories(id),
    name            VARCHAR(150) NOT NULL,
    description     TEXT,
    price           NUMERIC(10,2) NOT NULL,
    is_available    BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMP DEFAULT now(),
    updated_at      TIMESTAMP DEFAULT now(),
    image_url       TEXT
);

-- =========================
-- ADDONS
-- =========================
CREATE TABLE addons (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(150) NOT NULL,
    price           NUMERIC(10,2) NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMP DEFAULT now(),
    updated_at      TIMESTAMP DEFAULT now()
);

-- =========================
-- MENU ITEM ↔ ADDONS
-- =========================
CREATE TABLE menu_item_addons (
    id              SERIAL PRIMARY KEY,
    menu_item_id    INTEGER REFERENCES menu_items(id),
    addon_id        INTEGER REFERENCES addons(id)
);

-- =========================
-- ORDERS
-- =========================
CREATE TABLE orders (
    id                      SERIAL PRIMARY KEY,
    customer_name           VARCHAR(150),
    order_type              INTEGER NOT NULL,
    status                  INTEGER NOT NULL DEFAULT 1,
    payment_status          INTEGER NOT NULL DEFAULT 1,
    payment_mode            INTEGER,
    total_amount            NUMERIC(10,2) DEFAULT 0,
    created_at              TIMESTAMP DEFAULT now(),
    updated_at              TIMESTAMP DEFAULT now(),
    is_order_from_whatsapp  BOOLEAN NOT NULL DEFAULT FALSE
);

-- =========================
-- ORDER ITEMS
-- =========================
CREATE TABLE order_items (
    id                  SERIAL PRIMARY KEY,
    order_id            INTEGER NOT NULL REFERENCES orders(id),
    menu_item_id        INTEGER NOT NULL REFERENCES menu_items(id),
    quantity            INTEGER NOT NULL DEFAULT 1,
    price_at_order      NUMERIC(10,2) NOT NULL,
    line_total          NUMERIC(12,2) NOT NULL,
    created_at          TIMESTAMP DEFAULT now()
);

-- =========================
-- ORDER ITEM ADDONS
-- =========================
CREATE TABLE order_item_addons (
    id                      SERIAL PRIMARY KEY,
    order_item_id           INTEGER NOT NULL REFERENCES order_items(id),
    addon_id                INTEGER NOT NULL REFERENCES addons(id),
    addon_price_at_order    NUMERIC(10,2) NOT NULL,
    created_at              TIMESTAMP DEFAULT now()
);

-- =========================
-- N8N CHAT HISTORIES
-- =========================
CREATE TABLE n8n_chat_histories (
    id          SERIAL PRIMARY KEY,
    session_id  VARCHAR(255) NOT NULL,
    message     JSONB NOT NULL
);

COMMIT;
