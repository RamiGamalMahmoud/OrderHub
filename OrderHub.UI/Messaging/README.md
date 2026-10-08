# UI messaging contracts

All contracts in this folder use `OrderHub.UI.Messaging`. Each container is a public static class with immutable, sealed nested records. ViewModels send and receive them using `WeakReferenceMessenger.Default`; the contracts have no messenger dependencies and carry no Domain entities.

## Contract inventory

Use each nested type with its container prefix.

| Container | Nested message types |
| --- | --- |
| `UsersMessages` | `UserCreatedMessage`, `UserUpdatedMessage` |
| `CategoriesMessages` | `CategoryCreatedMessage`, `CategoryUpdatedMessage`, `CategoryDeletedMessage` |
| `CitiesMessages` | `CityCreatedMessage`, `CityUpdatedMessage`, `CityDeletedMessage` |
| `ClientsMessages` | `ClientCreatedMessage`, `ClientUpdatedMessage`, `ClientDeletedMessage` |
| `DeliverymenMessages` | `DeliverymanCreatedMessage`, `DeliverymanUpdatedMessage`, `DeliverymanDeletedMessage` |
| `ProductsMessages` | `ProductCreatedMessage`, `ProductUpdatedMessage`, `ProductDeletedMessage` |
| `ShippingCarriersMessages` | `ShippingCarrierCreatedMessage`, `ShippingCarrierUpdatedMessage`, `ShippingCarrierDeletedMessage` |
| `SuppliersMessages` | `SupplierCreatedMessage`, `SupplierUpdatedMessage`, `SupplierDeletedMessage` |
| `WhatsAppGroupsMessages` | `WhatsappGroupCreatedMessage`, `WhatsappGroupUpdatedMessage`, `WhatsappGroupDeletedMessage` |
| `OrdersMessages` | `OrderCreatedMessage`, `OrderUpdatedMessage`, `OrderDeletedMessage`, `OrderStatusChangedMessage`, `OrderPaymentMethodChangedMessage`, `OrderDraftSavedMessage`, `OrderDraftDeletedMessage` |
| `PropertiesMessages` | `PropertyCreatedMessage`, `PropertyUpdatedMessage` |
| `MessagesMessages` | `OutboxMessagesQueuedMessage` |

Updates and deletes carry the entity ID. Product/order/property creation also carries the returned ID. Order status and payment changes include the new status/payment-method ID. Draft events carry the existing GUID.

Seven legacy create commands (categories, cities, clients, deliverymen, shipping carriers, suppliers, WhatsApp groups) return only `Result`, with no created ID. Their created events deliberately have no payload; existing consumers reload through Application queries. No IDs are fabricated and no Application handlers were changed to expose IDs.

Outbox queue commands also return only success. `OutboxMessagesQueuedMessage` carries the associated order ID for a batch, or null for a manually composed message. Its consumer reloads through `GetOutboxMessagesQuery`.

## Migration

- Administration user creation now calls the existing `CreateUserCommand` and publishes `UserCreatedMessage` only after success. Like the legacy create commands, it returns no ID, so the event has no payload. The details editor publishes `UserUpdatedMessage(UserId)` after its update succeeds. `UsersViewModel` reloads after either event, including events received during an in-progress load. Failed saves remain editable and publish nothing.
- All created/updated/deleted UI contracts formerly in `OrderHub.Application.Messages` moved into the containers above. Names were retained except for the corrections below.
- `ProductedCreatedMessage`, `ProductedUpdatedMessage`, and `ProductedDeletedMessage` became `ProductCreatedMessage`, `ProductUpdatedMessage`, and `ProductDeletedMessage`.
- `DeleverymanUpdateMessage` became `DeliverymanUpdatedMessage`.
- Feature-local `DraftSavedMessage` and `DraftDeletedMessage` became `OrdersMessages.OrderDraftSavedMessage` and `OrdersMessages.OrderDraftDeletedMessage`.
- Feature-local `PropertyMessage` became separate `PropertyCreatedMessage` and `PropertyUpdatedMessage` events, published by the concrete editors after their commands complete. The base editor no longer publishes a success event.
- `MessagesCreatedMessage(IEnumerable<OutboxMessage>)` was removed. Its only old publisher was commented out. The outbox list now receives `OutboxMessagesQueuedMessage` from successful manual/order queue operations and reloads through an Application query instead of accepting Domain entities.
- Order payment changes now publish `OrderPaymentMethodChangedMessage` rather than `OrderUpdatedMessage`. Successful dedicated status commands publish `OrderStatusChangedMessage` with the returned status. The order list subscribes to both.
- Successful category deletes now publish the existing deletion event, whose receiver previously had no sender.

The obsolete UI message declarations were removed. The only retained `Application.Messages` contract is `OutboxMessages.MessageStatusChangedMessage`: this is an existing Infrastructure-to-UI delivery-status notification published by `MessageService`, not a ViewModel-to-ViewModel UI contract. Its publisher and two UI registrations are unchanged. Duplicating it in UI or referencing UI from Infrastructure would violate the layer boundary.

## Updated ViewModels

Paths below are relative to `OrderHub.UI/Features`; `ViewModel` refers to the class in the named feature folder.

| Area | Updated senders/receivers |
| --- | --- |
| Administration/Users | `Create/CreateUserViewModel`, `Details/UserDetailsViewModel`, `List/UsersViewModel` |
| Categories | `Create/ViewModel`, `Update/ViewModel`, `Index/ViewModel` |
| Cities | `Create/CreateCityViewModel`, `Edit/ViewModel`, `Index/ViewModel` |
| Clients | `Create/ViewModel`, `Update/ViewModel`, `Index/ViewModel`, `Edit/ViewModelBase` |
| Deliverymen | `Create/ViewModel`, `Edit/ViewModel`, `Index/ViewModel`, `Editor/ViewModel` |
| Products | `Create/ViewModel`, `Update/ViewModel`, `Index/ViewModel`, `Editor/ViewModel` |
| ShippingCarriers | `Create/ViewModel`, `Edit/ViewModel`, `Index/ViewModel`, `Editor/ViewModel` |
| Suppliers | `Create/ViewModel`, `Update/ViewModel`, `Index/SuppliersViewModel`, `Editor/EditSupplierViewModelBase` |
| WhatsappGroups | `Create/ViewModel`, `Update/ViewModel`, `Index/WhatsappGroupsViewModel` |
| Orders | `Create/ViewModel`, `Edit/ViewModel`, `Editor/ViewModel`, `Index/ViewModel`, `Index/OrderDraftsDrawerViewModel`, `OrderViewModel` |
| Settings/Properties | `PropertiesViewModel`, `PropertyEditor/CreatePropertyViewModel`, `PropertyEditor/UpdatePropertyViewModel`, `PropertyEditor/PropertyEditorViewModelBase` |
| Messages | `Index/ViewModel`, `SendMessage/SendMessageViewModel` |

## Intentionally excluded

- **Other Administration events:** user status is persisted by `UpdateUserCommand` and covered by `UserUpdatedMessage`; no separate activate/deactivate use cases exist. The list's status-toggle placeholder and unused standalone update editor are unchanged. First-admin setup continues directly through application initialization and has no independent UI listener. No deleted or unused admin event was invented.
- **Quotation, ProformaInvoice, Invoice:** actual creation paths directly open a document preview. No independent document list/listener or document update/status UI operation exists. No generic document event or unused container was added.
- **Payment methods:** lookup data exists, but there is no payment-method maintenance UI requiring events. Changing an order's payment method belongs to `OrdersMessages`.
- **Settings and Logs:** no existing independent listener needs a settings/log event. Authentication, credentials, and configuration workflows remain unchanged.
- **Property deletion and outbox resend/deletion:** their current screens update their own state; no additional consumer was identified.
- **Child/value models:** addresses, phones, permissions, property options, product properties, order items/properties/delivery steps, document items, recipients, attachments, sessions, tokens, and entity sequences have no independent UI event use case.
- **Navigation data and local callbacks:** dialog parameters such as `MessageData`, editor `PropertyChanged`, and close requests are not broadcast events and retain their existing roles.

## Verification

- Full solution build: `dotnet build OrderHub.sln --no-restore -v:q` succeeded with **0 warnings and 0 errors**.
- Focused `OrderMessagingTests`: **5 passed**, covering successful/failed status and payment changes, event payloads, and withholding an event while a status command is pending.
- Administration messaging and existing user-editor tests pass, covering success/failure publication, pending creates, duplicate-submit prevention, user-ID payloads, and list refreshes during loading.
- All migrated registrations and sends compile. No Feature accesses another Feature's namespace for a broadcast message contract.
- No messenger dependencies or Domain entity payloads exist in these contracts. No duplicate legacy UI event declarations remain.
- No business rules, persistence, permissions, authentication, or Application handlers were changed by this migration. Existing unrelated workspace changes were preserved.
