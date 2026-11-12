namespace OrderHub.Infrastructure.Migrations;

// Frozen seed: provider-neutral (PostgreSQL + SQLite), idempotent and insert-only.
// It never updates, deletes or renames existing rows.
internal static class SealedRoleSeedSql
{
    public const string V1 = """
        INSERT INTO roles (name, is_sealed, role_key, created_at)
        SELECT candidates.name, TRUE, candidates.role_key, CURRENT_TIMESTAMP
        FROM (
            SELECT 'مبيعات' AS name, 'sales' AS role_key
            UNION ALL SELECT 'مشتريات', 'purchasing'
            UNION ALL SELECT 'إدخال البيانات', 'data-entry'
            UNION ALL SELECT 'مدير', 'manager'
        ) AS candidates
        WHERE NOT EXISTS (SELECT 1 FROM roles existing WHERE existing.role_key = candidates.role_key)
          AND NOT EXISTS (SELECT 1 FROM roles existing WHERE existing.name = candidates.name);

        INSERT INTO role_permissions (role_id, feature, level)
        SELECT role_row.id, cells.feature, cells.level
        FROM roles role_row
        INNER JOIN (
            SELECT 'sales' AS role_key, 'Orders' AS feature, 'Write' AS level
            UNION ALL SELECT 'sales', 'Documents', 'Read'
            UNION ALL SELECT 'sales', 'Products', 'Read'
            UNION ALL SELECT 'sales', 'Categories', 'Read'
            UNION ALL SELECT 'sales', 'Clients', 'Write'
            UNION ALL SELECT 'sales', 'Deliverymen', 'Read'
            UNION ALL SELECT 'sales', 'ShippingCarriers', 'Read'
            UNION ALL SELECT 'sales', 'Cities', 'Read'
            UNION ALL SELECT 'sales', 'PaymentMethods', 'Read'
            UNION ALL SELECT 'purchasing', 'Orders', 'Read'
            UNION ALL SELECT 'purchasing', 'Documents', 'Read'
            UNION ALL SELECT 'purchasing', 'Products', 'Read'
            UNION ALL SELECT 'purchasing', 'Suppliers', 'Write'
            UNION ALL SELECT 'purchasing', 'Cities', 'Read'
            UNION ALL SELECT 'purchasing', 'PaymentMethods', 'Read'
            UNION ALL SELECT 'data-entry', 'Products', 'Write'
            UNION ALL SELECT 'data-entry', 'Categories', 'Write'
            UNION ALL SELECT 'data-entry', 'Properties', 'Write'
            UNION ALL SELECT 'data-entry', 'Cities', 'Write'
            UNION ALL SELECT 'data-entry', 'PaymentMethods', 'Read'
            UNION ALL SELECT 'manager', 'Orders', 'Approve'
            UNION ALL SELECT 'manager', 'Documents', 'Approve'
            UNION ALL SELECT 'manager', 'Products', 'Approve'
            UNION ALL SELECT 'manager', 'Categories', 'Approve'
            UNION ALL SELECT 'manager', 'Properties', 'Approve'
            UNION ALL SELECT 'manager', 'Clients', 'Approve'
            UNION ALL SELECT 'manager', 'Suppliers', 'Approve'
            UNION ALL SELECT 'manager', 'Deliverymen', 'Approve'
            UNION ALL SELECT 'manager', 'ShippingCarriers', 'Approve'
            UNION ALL SELECT 'manager', 'Cities', 'Approve'
            UNION ALL SELECT 'manager', 'PaymentMethods', 'Approve'
        ) AS cells ON cells.role_key = role_row.role_key
        WHERE NOT EXISTS (
            SELECT 1 FROM role_permissions existing
            WHERE existing.role_id = role_row.id
              AND existing.feature = cells.feature
        );
        """;

    public const string V2 = """
        INSERT INTO roles (name, is_sealed, role_key, created_at)
        SELECT candidates.name, TRUE, candidates.role_key, CURRENT_TIMESTAMP
        FROM (
            SELECT 'أمين مخزن' AS name, 'storekeeper' AS role_key
        ) AS candidates
        WHERE NOT EXISTS (SELECT 1 FROM roles existing WHERE existing.role_key = candidates.role_key)
          AND NOT EXISTS (SELECT 1 FROM roles existing WHERE existing.name = candidates.name);

        INSERT INTO role_permissions (role_id, feature, level)
        SELECT role_row.id, cells.feature, cells.level
        FROM roles role_row
        INNER JOIN (
            SELECT 'sales' AS role_key, 'Inventory' AS feature, 'Read' AS level
            UNION ALL SELECT 'purchasing', 'Inventory', 'Read'
            UNION ALL SELECT 'manager', 'Warehouses', 'Write'
            UNION ALL SELECT 'manager', 'Inventory', 'Approve'
            UNION ALL SELECT 'storekeeper', 'Warehouses', 'Read'
            UNION ALL SELECT 'storekeeper', 'Products', 'Read'
            UNION ALL SELECT 'storekeeper', 'Inventory', 'Write'
        ) AS cells ON cells.role_key = role_row.role_key
        WHERE NOT EXISTS (
            SELECT 1 FROM role_permissions existing
            WHERE existing.role_id = role_row.id
              AND existing.feature = cells.feature
        );
        """;
}
