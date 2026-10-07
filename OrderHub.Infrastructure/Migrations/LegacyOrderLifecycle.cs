namespace OrderHub.Infrastructure.Migrations;

internal static class LegacyOrderLifecycle
{
    // Retain the source status before resetting active orders. The transaction owns
    // this temporary snapshot; repeat runs skip orders that already have history.
    internal const string Sql = """
        DO $$ BEGIN
            IF EXISTS (SELECT 1 FROM orders WHERE NOT isfinite(created_at))
                OR EXISTS (SELECT 1 FROM order_delivery_steps WHERE NOT isfinite(created_at)) THEN
                RAISE EXCEPTION 'Review non-finite legacy creation dates before lifecycle initialization.';
            END IF;
        END $$;

        CREATE TEMP TABLE lifecycle_legacy_orders ON COMMIT DROP AS
        SELECT id, order_status AS original_status, created_at
        FROM orders o WHERE NOT EXISTS (SELECT 1 FROM order_status_history h WHERE h.order_id = o.id);

        INSERT INTO order_status_history
            (order_id, previous_status, new_status, changed_at_utc, source, order_version, original_status)
        SELECT id, NULL, 'Processing', created_at, 'LegacyMigration', 1, original_status
        FROM lifecycle_legacy_orders;

        INSERT INTO order_status_history
            (order_id, previous_status, new_status, changed_at_utc, source, order_version, original_status)
        SELECT id, 'Processing', original_status, created_at + INTERVAL '1 second', 'LegacyMigration', 2, original_status
        FROM lifecycle_legacy_orders WHERE original_status IN ('Delivered', 'Cancelled');

        UPDATE orders o SET
            order_status = CASE WHEN l.original_status IN ('Delivered', 'Cancelled') THEN l.original_status ELSE 'Processing' END,
            version = CASE WHEN l.original_status IN ('Delivered', 'Cancelled') THEN 2 ELSE 1 END
        FROM lifecycle_legacy_orders l WHERE o.id = l.id;

        UPDATE order_delivery_steps s SET status = CASE
            WHEN l.original_status = 'Delivered' THEN 'Completed'
            WHEN l.original_status = 'Cancelled' THEN 'Cancelled'
            ELSE 'Pending' END
        FROM lifecycle_legacy_orders l WHERE s.order_id = l.id;

        INSERT INTO delivery_step_status_history
            (order_id, delivery_step_id, previous_status, new_status, changed_at_utc, source, order_version)
        SELECT s.order_id, s.id, NULL, s.status, s.created_at, 'LegacyMigration', 2
        FROM order_delivery_steps s JOIN lifecycle_legacy_orders l ON s.order_id = l.id
        WHERE l.original_status IN ('Delivered', 'Cancelled');
        """;
}
