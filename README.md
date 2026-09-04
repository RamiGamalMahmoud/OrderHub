# OrderHub

> A desktop business application for managing orders, products, customers, pricing, and commercial workflows.

**OrderHub** is a Windows desktop application built with **C# and .NET**, designed to help businesses manage their day-to-day order workflows and the operations surrounding them.

The project focuses on building software around **real business processes**, rather than treating each feature as an isolated function. Orders, products, customers, pricing, documents, integrations, and background processes are designed to work together as part of a consistent workflow.

---

## Overview

OrderHub is being developed as a customizable business solution that brings together different parts of the order management process in one application.

The application is designed to support workflows such as:

* Managing customers and their information
* Managing products and product properties
* Creating and managing orders
* Applying pricing and calculation rules
* Managing quotations and commercial documents
* Generating PDF documents
* Integrating with external platforms and services
* Synchronizing data
* Managing users and permissions
* Automating repetitive business operations

The goal is to provide a reliable application that can evolve as business requirements change.

---

## Key Features

### Orders

OrderHub provides a workflow for creating and managing orders while keeping the different parts of the order process connected.

Order-related functionality includes:

* Order creation and editing
* Order items
* Customer information
* Delivery information
* Payment methods
* Order status management
* Delivery steps
* Order-related business rules

The application also supports working with incomplete orders through a draft workflow before converting them into finalized orders.

---

### Products

Products are managed as part of the overall business workflow.

The product system supports:

* Product management
* Product properties
* Property options
* Product-specific properties
* Product information used during order creation
* Pricing and calculation integration

This allows product information to participate directly in order and pricing workflows.

---

### Customers

Customer information can be managed and associated with business operations such as orders and deliveries.

The system is designed so that customer data can be reused across different workflows instead of being duplicated inside individual operations.

---

### Pricing & Calculations

Pricing logic is implemented as reusable domain services rather than being tied to a specific UI screen or use case.

The pricing system is responsible for calculating values such as:

* Item prices
* Quantities
* Discounts
* Subtotals
* VAT
* Final totals

The same calculation logic can be used from different parts of the application, helping keep business rules consistent.

---

### Commercial Documents

OrderHub supports commercial documents that can be associated with the business workflow.

Current document types include:

* Quotations
* Proforma Invoices
* Invoices

Documents share common commercial calculation concepts while allowing each document type to have its own business rules.

For example, invoices can be associated with the order from which they were created.

---

### PDF Generation

Commercial documents can be generated as PDF files automatically.

The project uses **QuestPDF** for document generation, allowing business documents such as quotations and invoices to be generated directly from application data.

This helps reduce manual document preparation and keeps generated documents connected to the actual business workflow.

---

## Integrations

OrderHub is designed to communicate with external services and platforms when required by the business workflow.

### Salla

The application includes work toward integrating with **Salla** to allow business data to be synchronized between OrderHub and the customer's Salla store.

The integration is designed around communicating with the platform through its APIs rather than requiring manual data entry.

---

### WhatsApp

OrderHub also integrates with WhatsApp to support communication as part of the business workflow.

The integration includes capabilities for:

* Sending messages
* Sending attachments
* Working with files associated with messages
* Automating parts of the communication workflow

---

### APIs & External Services

The application can communicate with external services through APIs, allowing OrderHub to be extended beyond its internal database and desktop environment.

This makes it possible to connect the application with services used by the business without coupling the core business logic directly to a specific external platform.

---

## Business Workflow

One of the main design goals of OrderHub is to model the relationships between business operations.

A typical workflow can involve:

```text
Customer
   │
   ▼
Order
   │
   ├── Products
   │
   ├── Pricing
   │
   ├── Payment
   │
   ├── Delivery
   │
   └── Commercial Documents
           │
           ├── Quotation
           ├── Proforma Invoice
           └── Invoice
```

External integrations can then participate in the workflow:

```text
             ┌───────────────┐
             │   OrderHub    │
             └───────┬───────┘
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
      Salla       WhatsApp    PDF Documents
```

This approach allows the application to evolve around the actual business process rather than around individual technical features.

---

## Architecture

OrderHub follows a modular architecture based on **Domain-Driven Design**, **CQRS**, and **Vertical Slice Architecture**, with **MVVM** used for the WPF presentation layer.

The architecture is intended to keep business rules independent from infrastructure and UI concerns while still remaining practical for a desktop application.

### Main Layers

```text
OrderHub
│
├── Domain
│   ├── Entities
│   ├── Value Objects
│   ├── Domain Services
│   └── Business Rules
│
├── Application
│   ├── Features
│   ├── Commands
│   ├── Queries
│   └── Use Cases
│
├── Infrastructure
│   ├── Persistence
│   ├── EF Core
│   ├── External Services
│   └── File Storage
│
└── UI
    ├── Views
    ├── ViewModels
    └── Components
```

---

## Domain-Driven Design

Business rules are kept inside the domain wherever possible.

The domain contains concepts such as:

* Orders
* Order Items
* Products
* Customers
* Commercial Documents
* Pricing
* Delivery
* Payments

This allows the core business logic to remain independent of the WPF UI or specific infrastructure implementations.

---

## CQRS

OrderHub uses **Command Query Separation** to distinguish between operations that modify the application state and operations that retrieve data.

Examples include:

```text
Commands
├── Create Order
├── Change Payment Method
├── Update Product
└── Create Commercial Document

Queries
├── Get Orders
├── Get Products
└── Get Customers
```

This separation helps keep use cases focused and makes the application easier to evolve as it grows.

---

## Vertical Slice Architecture

Application features are organized around use cases rather than forcing the entire application into technical layers of controllers, services, and repositories.

A feature can contain the components required to implement a specific business operation.

For example:

```text
Features
│
├── Orders
│   ├── Create
│   ├── Update
│   ├── Delete
│   └── Get
│
├── Products
│   ├── Create
│   ├── Update
│   └── Get
│
└── CommercialDocuments
    ├── CreateQuotation
    ├── CreateProformaInvoice
    └── CreateInvoice
```

This keeps related code close together and makes individual business operations easier to understand.

---

## MVVM

The WPF application uses the **MVVM (Model-View-ViewModel)** pattern.

The UI is separated from application and domain logic, allowing ViewModels to coordinate UI interactions without placing business rules directly inside XAML or code-behind.

The project also uses **CommunityToolkit.Mvvm** to simplify ViewModel and command implementation.

---

## Reliability & Background Processing

OrderHub includes infrastructure for handling operations that should not depend entirely on the immediate success of a UI action.

The project uses an **Outbox-based approach** for reliable message processing and supports retrying failed operations.

This is particularly useful when communicating with external services where network failures or temporary service errors can occur.

---

## Data & Persistence

The application uses **Entity Framework Core** for data access and persistence.

The architecture keeps persistence concerns inside the Infrastructure layer while allowing the Domain and Application layers to remain independent of the database implementation.

Repository and Unit of Work patterns are used where they provide value within the application's use cases.

---

## Draft Workflow

Order creation can involve multiple steps before an order becomes finalized.

To support this workflow, incomplete order data can be stored as a draft before the final order is created.

The general flow is:

```text
Start Order
    │
    ▼
Create Draft
    │
    ├── Add Customer
    ├── Add Products
    ├── Configure Pricing
    ├── Add Delivery Information
    └── Add Documents
    │
    ▼
Complete Order
    │
    ▼
Persist Final Order
    │
    ▼
Remove Draft
```

This provides a safer workflow for long-running or partially completed order creation processes.

---

## Technology Stack

### Core

* C#
* .NET
* Entity Framework Core

### Desktop UI

* WPF
* MVVM
* CommunityToolkit.Mvvm
* HandyControls
* Material Icons

### Architecture

* Domain-Driven Design
* CQRS
* Vertical Slice Architecture
* Repository Pattern
* Unit of Work
* Outbox Pattern

### Integration & Automation

* REST APIs
* Selenium WebDriver
* Playwright
* Salla Integration
* WhatsApp Integration

### Documents

* QuestPDF
* PDF generation
* File storage

### Development

* Visual Studio
* Git
* GitHub

---

## Project Structure

The project is organized around clear responsibilities:

```text
src/
│
├── OrderHub.Domain
│
├── OrderHub.Application
│
├── OrderHub.Infrastructure
│
└── OrderHub.UI
```

### Domain

Contains the core business concepts and rules.

### Application

Contains use cases and application-specific workflows.

### Infrastructure

Contains persistence, external integrations, file storage, and other technical implementations.

### UI

Contains the WPF presentation layer, Views, ViewModels, and UI components.

---

## Screenshots

> Screenshots will be added here to showcase the main parts of the application.

### Orders

<!-- Add order editor screenshot -->

### Products

<!-- Add products screenshot -->

### Commercial Documents

<!-- Add quotation / invoice screenshot -->

### Dashboard / Main Application

<!-- Add main application screenshot -->

---

## Project Status

**OrderHub is currently under active development.**

The project continues to evolve as new business requirements and integrations are added.

The architecture and domain model are also being refined alongside the implementation to keep the application maintainable as its scope grows.

---

## Why OrderHub?

OrderHub is more than a collection of CRUD screens.

The project is being developed around the idea that a business application should represent the **actual workflow of the business**.

Instead of implementing isolated features such as:

```text
Orders
Products
Invoices
Customers
```

the system focuses on how those concepts interact:

```text
Customer
   ↓
Order
   ↓
Products
   ↓
Pricing
   ↓
Documents
   ↓
Delivery / Payment
   ↓
External Integrations
```

This approach helps create software that is easier to extend when the business process changes.

---

## Development Philosophy

The project follows a simple principle:

> **Architecture should solve problems, not create them.**

DDD, CQRS, Vertical Slice Architecture, and other patterns are used where they provide practical value.

The goal is not to build the most complex architecture possible, but to create a codebase that is:

* Maintainable
* Understandable
* Testable
* Extensible
* Consistent with the business domain

---

## License

This project is currently a private/custom business application and is not intended as an open-source product.

The repository is primarily used to document and showcase the development of the project.
