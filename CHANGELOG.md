# Changelog

This changelog summarizes the feature work and behavior changes added on the current branch after `master`.

## Order Editing

- Added order edit selection flow from the orders index.
- Added `Feature/Orders/Edit` support on top of the shared abstract order editor.
- Extended the shared order editor view model so it supports both create and edit scenarios.
- Added query/DTO/update-command support for loading an order into edit mode and saving updates.
- Registered the order edit view/viewmodel in DI and wired the orders list edit action.

## Delivery Chain

- Added delivery chain support to orders in domain, handlers, and UI.
- Added `OrderDeliveryStep` handling and related flow updates.
- Updated order UI/editor behavior to support multi-step delivery routing.

## Orders Feature Completion

- Completed order delete flow end to end.
- Replaced placeholder recipient-send behavior in the orders table.
- Improved order notification generation for delivery and shipping recipients.
- Added live order-row recipient status updates based on outbox message status.
- Disabled order recipient-send buttons when the order does not have the related recipient.
- Added sent-state styling for recipient buttons in the orders table.

## Outbox Messaging

- Added outbox recipient tables and binding flow.
- Added outbox resend action for failed messages.
- Added message status change messaging so orders and outbox screens refresh live.
- Added support for recipient-based status tracking per order.
- Added message resend scheduling from the messages list.

## Recipient Phone Synchronization

- Added automatic outbox recipient phone updates when source entity phone numbers change.
- Synced clients, suppliers, shipping carriers, and deliverymen into pending/failed outbox recipients.

## Deliverymen Phone Refactor

- Moved deliverymen toward the shared `Phone` entity model used by other entities.
- Added country code support to deliveryman create/edit/update DTOs and UI.
- Added fallback support for legacy saved deliveryman phone values when opening edit mode.
- Fixed deliveryman edit city restoration to use `CityId`.
- Added deliveryman phone relation migration metadata and fallback column handling.

## Search and List Improvements

- Wired search/filter behavior in Messages, Clients, Deliverymen, Shipping Carriers, Suppliers, and Categories index screens.
- Fixed category list binding issues.
- Fixed missing or broken delete confirmation text in some list screens.

## WhatsApp Automation Hardening

- Hardened WhatsApp startup lifecycle to avoid duplicate browser sessions.
- Added more reliable stale-driver recovery for:
  - closed browser windows
  - missing web views
  - invalid Selenium sessions
- Improved invalid-recipient detection for numbers not on WhatsApp.
- Restricted message targeting to the actual footer composer instead of the search box.
- Added stronger startup/send-state checks before reporting message send success.
- Added startup notification when WhatsApp Web fails to start.
- Started the outbox worker automatically after successful WhatsApp startup.
- Added cleaner message worker start/stop lifecycle and restart behavior.
- Added WhatsApp cleanup and message worker shutdown on app exit.

## Application Logging and Production Error Handling

- Added `IAppLogger` and file-based logging implementation.
- Added per-day log file writing under the application logs directory.
- Added unhandled exception logging for startup, dispatcher, AppDomain, and unobserved task exceptions.
- Changed production error behavior to show a safe generic message while saving full exception details to log files.

## Application Directories

- Added logs directory support to `ApplicationDirectoriesService`.
- Moved logs directory creation responsibility into the application directories service.
- Updated startup directory preparation to create logs folder alongside the other app folders.

## Splash Screen

- Added real startup progress reporting instead of a permanently indeterminate progress bar.
- Added startup step display names and progress percentage reporting from the startup pipeline.
- Bound splash UI to startup progress state.
- Added a splash close button so the user can terminate the app if startup hangs.
- Yielded the dispatcher before startup begins so the splash can render first.
- Moved heavy WhatsApp startup work off the UI thread to reduce splash freezing while Chrome opens.

## Infrastructure and DI

- Registered new services introduced by this branch:
  - order edit flow dependencies
  - outbox resend/status support
  - file app logger
  - startup progress service

## Notes

- Infrastructure builds were validated multiple times with:
  - `dotnet build OrderHub.Infrastructure/OrderHub.Infrastructure.csproj`
- Full WPF/UI validation is still limited by the existing project/XAML issue already present in the workspace.
