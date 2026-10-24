# AdminPanel

A multi-tenant administration panel built with **ASP.NET Core Razor Pages** and **SQL Server**. It lets administrators manage organisational data, configuration, templates and user roles for multiple companies from a single application, with every tenant seeing only its own data and sensitive tables protected by role-based access.

![Admin panel overview 1](docs/screenshots/1.jpg)

![Admin panel overview 2](docs/screenshots/2.jpg)

## Features

### Multi-tenancy (Mandant system)
- Each company (tenant) gets its own isolated view of the data.
- A **tenant provider** resolves the active tenant (Mandant) and scopes every query to it automatically.
- Tenant switcher in the top bar for administrators.
- Support for global (tenant-independent) records alongside tenant-specific ones.

### Role-based access control
- Access is managed through **roles**, assigned per user.
- **Sensitive tables** (for example configuration, config logs and system logs) are restricted to authorised roles only. Users without the required role cannot view or modify them.
- Permissions are enforced on the server, not just hidden in the UI, so restricted pages and data cannot be reached by guessing a URL.
- Roles are managed directly in the panel through the **Roles** module.
- Combined with the tenant system, a user only sees the data they are allowed to see, and only for their own tenant.

### Management modules
- **Departments & Units** (Abteilungen & Referate): organisational structure
- **Persons**: people and their details
- **Addresses**: address management
- **Config**: application and connection settings editable at runtime (restricted)
- **Config Logs**: full audit trail of configuration changes (old value, new value, changed by, changed at, tenant) (restricted)
- **Logs**: application logs (restricted)
- **Template Management** (Vorlagenverwaltung): templates
- **Roles**: create and manage roles and their permissions

### Data tables
- Custom **sorting** on every column
- Per-column **filtering** (text, dropdown, date range)
- **Pagination** with adjustable page size and jump-to-page
- Entry counter and one-click filter reset

### UI
- Dark and light theme
- Selectable colour themes
- Adjustable page zoom
- Responsive tabbed navigation

## Tech stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core (Razor Pages) |
| Language | C# |
| Database | Microsoft SQL Server |
| Front end | HTML, CSS, JavaScript |
