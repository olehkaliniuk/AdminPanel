# AdminPanel

AdminPanel is my first real project at **DreiF in Berlin**. It is a web-based administration panel where one application serves several companies at once, and each company only ever sees its own data.

I built it with **ASP.NET Core (Razor Pages)**, **C#** and **SQL Server**. The front end is plain HTML, CSS and JavaScript, with no big front-end framework on top.

![Admin panel overview 1](docs/screenshots/1.jpg)

![Admin panel overview 2](docs/screenshots/2.jpg)

## What it does

### One application, many companies
The panel is multi-tenant (in German: *mandantenfähig*). Every company is a **Mandant**, and a **tenant provider** works out which Mandant is currently active and limits every query to that Mandant's data. Administrators can switch between Mandanten from the top bar. Some records are global and don't belong to any single company, and the system handles those too.

### Roles and protected tables
Not everyone should be able to open everything. Access is controlled through **roles**, which are assigned per user and managed in the panel itself. The especially sensitive areas, such as the configuration, the config logs and the system logs, are only available to authorised roles. The check happens on the server, so typing a URL by hand doesn't get you into a page you aren't allowed to see.

### Modules
- **Departments & Units** (Abteilungen & Referate): the organisational structure
- **Persons**: people and their details
- **Addresses**: address management
- **Config**: application settings that can be changed while the app is running (restricted)
- **Config Logs**: an audit trail of every configuration change with old value, new value, who changed it, when, and for which Mandant (restricted)
- **Logs**: application logs (restricted)
- **Template Management** (Vorlagenverwaltung): templates
- **Roles**: creating and managing roles and their permissions

### Tables that are pleasant to work with
Every data table supports custom **sorting** on each column and **filtering** per column (text, dropdown, date range). You can set the page size, jump to a specific page, see the number of entries, and reset all filters with one click.

### Interface
There is a dark and a light theme, a choice of colour themes, adjustable page zoom, and tab-based navigation between the modules.

## Technologies

| Area | What I used |
|------|-------------|
| Backend | ASP.NET Core, Razor Pages, C# |
| Database | Microsoft SQL Server |
| Front end | HTML, CSS, JavaScript |
| Access control | Role-based permissions enforced on the server |
| Multi-tenancy | Tenant (Mandant) provider that scopes all data access |
| IDE | Visual Studio |

## About the project

This was my first project in a real working environment, so a lot of it was about doing things properly: keeping each company's data separated, protecting sensitive areas, and making the tables usable for people who work with them every day.
