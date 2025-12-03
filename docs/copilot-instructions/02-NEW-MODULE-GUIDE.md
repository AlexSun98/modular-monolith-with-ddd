# Guide: Creating a New Module

This guide explains how to add a new module to the Modular Monolith.

## 1. Folder Structure
Create a new folder in `src/Modules/` with the module name (e.g., `Inventory`).
Inside, create the standard sub-projects (folders):
*   `Application`
*   `Domain`
*   `Infrastructure`
*   `IntegrationEvents`
*   `Tests` (Optional, usually in `src/Tests/Inventory/`)

## 2. Project Setup
Create `.csproj` files for each layer.
*   **Domain**: No dependencies on other layers.
*   **Application**: Depends on `Domain`.
*   **Infrastructure**: Depends on `Application` and `Domain`.
*   **IntegrationEvents**: Standalone, no dependencies (or minimal).

## 3. Module Interface
Define the public interface for the module in the `Application` layer (or a separate Contracts project if needed, but usually `Application` defines the `IModule` interface).
It should expose:
```csharp
public interface IInventoryModule
{
    Task<TResult> ExecuteCommandAsync<TResult>(ICommand<TResult> command);
    Task ExecuteCommandAsync(ICommand command);
    Task<TResult> ExecuteQueryAsync<TResult>(IQuery<TResult> query);
}
```

## 4. Composition Root (Infrastructure)
Implement the `InventoryModule` class (Autofac Module) in `Infrastructure`.
*   Register Mediator, Repositories, EF Context, etc.
*   Implement a static `Startup` or `Initialize` method to configure the container.

## 5. Database Schema
*   Create a new schema in the database (e.g., `inventory`).
*   Create a `DbContext` in `Infrastructure`.
*   Configure Entity mappings.
*   Add migration scripts to `src/Database/CompanyName.MyMeetings.Database/inventory/`.

## 6. Integration with API
*   In `src/API/Startup.cs`, call the module's initialization method.
*   Register the module interface in the API's DI container.

## 7. Event Bus Integration
*   If the module needs to publish events, use `IEventsBus`.
*   If the module needs to subscribe to events, register `IIntegrationEventHandler`s.
