-- Seed: Demo tenant for development and local testing
-- Run after applying migrations: psql -d SportFlow_Dev -f SeedDemoTenant.sql

INSERT INTO "Tenants" ("Id", "Name", "Slug", "Status", "Plan", "CreatedAt", "UpdatedAt")
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Demo Gym',
    'demo',
    'active',
    'basic',
    NOW(),
    NOW()
)
ON CONFLICT ("Id") DO NOTHING;
