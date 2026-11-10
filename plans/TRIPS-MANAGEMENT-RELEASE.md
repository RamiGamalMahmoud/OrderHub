# Standalone Trips (مشاوير مستقلة) — Deployment & Rollback

- **Date:** 2026-11-09
- **Applies to:** `20261109124656_AddStandaloneTrips`
- **Feature state:** implemented and tested; inert until the `Trips` permission is granted
- **Owner-run:** migration application, rollback rehearsal, permission grants, UI sign-off. No agent commit, tag, push or database command was run.

---

## 1. What ships

| Layer | Change | Reversible? |
|---|---|---|
| Database | Three new tables: `standalone_trips`, `standalone_trip_attempts`, `standalone_trip_histories` with their indexes and check constraints | Yes — `Down` drops exactly these three tables |
| Database | `Down` also removes `role_permissions` rows whose `feature = 'Trips'` | Yes; grants are re-creatable |
| Database | No existing table, column, row, index, constraint, enum encoding or `number_sequences` row is modified | — |
| Code | Trips domain/application/infrastructure/Web pages, lookup extension, four delete guards, `Trips` permission enum + catalog + nav entry | Previous build is a clean rollback |
| Numbering | Uses the existing `Deliveryman` sequence. No new `NumberSequenceKind`; no change to work-record numbering | Gaps stay; never decrement |

Migration file: `OrderHub.Infrastructure/Migrations/20261109124656_AddStandaloneTrips.cs` (created after `20261108214724_SeedSealedRoles`).

---

## 2. Deployment order

1. **Backup** the target database (and note the current migration id).
2. **Apply the migration first**, before starting the new build:
   ```powershell
   dotnet ef database update --project OrderHub.Infrastructure --startup-project OrderHub.Web
   ```
   After this step the three tables exist and are empty; the running previous build ignores them.
3. **Deploy the new build.**
4. **Smoke test** with an admin account:
   - the existing pages load (orders, deliverymen, clients, suppliers, carriers, roles);
   - `/deliverymen` still lists, edits, deletes and pages as before;
   - the Trips navigation entry is visible only to users with `Trips.Read`;
   - creating one trip allocates the next deliveryman monthly number and appears in the list and details.
5. **Grant `Trips`** to the intended roles from the role editor ("المشاوير", Read/Write). Until then sealed roles and existing users see no new entry, by design.

If the build is deployed before the migration, Trips pages fail with a missing-table error while every existing page keeps working. Apply the migration first.

---

## 3. Rollback

### 3.1 Preferred: code-only rollback

Redeploy the previous build and **leave the three tables in place**. They are inert: no code in the previous build reads or writes them. Trip data is preserved for a later retry. Granted `Trips` permissions become unreachable until the feature returns.

### 3.2 Full rollback (owner approval required)

1. Stop the application.
2. **Export trip data first** if it must be kept:
   ```sql
   COPY standalone_trips TO '.../standalone_trips.csv' CSV HEADER;
   COPY standalone_trip_attempts TO '.../standalone_trip_attempts.csv' CSV HEADER;
   COPY standalone_trip_histories TO '.../standalone_trip_histories.csv' CSV HEADER;
   ```
3. Revert the migration (its `Down` deletes `Trips` grants, then drops the three tables):
   ```powershell
   dotnet ef database update 20261108214724_SeedSealedRoles --project OrderHub.Infrastructure --startup-project OrderHub.Web
   ```
4. Deploy the previous build.

**Never run** `UPDATE number_sequences SET ...` or any decrement. Numbers already handed out stay
consumed, exactly like every other work record. Reverting them would hand the same number to a future trip.

---

## 4. Release note (draft)

> **المشاوير المستقلة**
>
> أُضيفت ميزة إدارة المشاوير المستقلة: إنشاء مشوار لمندوب، تسجيل طرفي المسار، جدولة اختيارية، بدء/إكمال/تفشيل/إلغاء/إعادة المحاولة، إعادة إسناد مع ترقيم من جديد، سجل زمني محمي، وقائمة وتفاصيل مع بحث وفلترة.
>
> الميزة تظهر فقط لمن يملك صلاحية «المشاوير». لا يتغير أي سلوك قائم للطلبات أو سلاسل التوصيل أو الحذف ما لم يكن السجل مرتبطاً بمشوار.
>
> الترقيم الشهري يستخدم تسلسل المندوب الحالي. أرقام المشاوير لا تُعاد أبداً عند الإلغاء.
>
> تطبيق القاعدة: نفّذ الترحيل قبل نشر النسخة الجديدة. الرجوع عن النسخة لا يتطلب حذف الجداول، وبيانات المشاوير تُحفظ إن بقيت الجداول.

---

## 5. Owner sign-off checklist

- [ ] Migration applied to a populated copy; existing pages behave as before.
- [ ] Rollback rehearsed (at minimum §3.1; §3.2 only with exported data and explicit approval).
- [ ] Manual UI matrix from the spec (checks 51–63 and 69) recorded.
- [ ] Sealed roles still show no Trips entry before granting.
- [ ] `Trips` granted to the agreed roles.
- [ ] Full test suite and the Node time test re-run on the release build:
      `dotnet test OrderHub.sln --configuration Release --no-build` and
      `node OrderHub.Tests/Web/order-local-time.test.mjs`.
- [ ] Release note published and the `Trips` visibility decision communicated.

---

## 6. Verified test evidence (2026-11-09)

| Check | Result |
|---|---|
| `dotnet build OrderHub.sln --configuration Release` | Succeeded, zero errors |
| `dotnet test OrderHub.sln --configuration Release --no-build` | 271 passed, 0 failed |
| `node OrderHub.Tests/Web/order-local-time.test.mjs` | Passed |
| Trips suites | Domain, persistence, repositories, create/update/lifecycle handlers, list/details/editor/lookups, deliveryman queries, usage reader, delete guards, DI — all green |

Not covered by automated tests (manual, owner-run): Razor page behaviour, navigation permission cache, role-editor badge, `TableFooter` interaction and the full UI matrix.
