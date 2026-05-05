# Data Provisioning Engine

The **Data Provisioning Engine** is a comprehensive .NET 8 ASP.NET Core MVC web application designed to manage and orchestrate user access requests to organizational databases and datasets. 

Originally ported from a legacy PHP architecture, this modernized application enforces Clean Architecture principles, strict separation of concerns, and enterprise-grade identity integration.

## Key Features

- **Dataset Catalog:** Browse available data assets, view metadata (Fact/Dimension/Staging), and request granular access.
- **Request Workflows:** Automated request tracking. Users can request access, while Information Asset Owners (IAO) and Approvers (IAA) can review, approve, or reject dataset access natively.
- **Enterprise Authentication:**
  - **Windows Authentication:** Seamless, zero-touch sign-on using Windows Active Directory credentials.
  - **Test/Impersonation Mode:** A configurable `TestMode` that swaps Windows Auth out for a simple dropdown-based Cookie authentication system, allowing developers to easily test workflows across different roles.
  - **Automatic User Provisioning:** As new AD users access the site, their accounts are automatically provisioned in the application. Specific domain accounts can be pre-configured as `InitialAdmins` to automatically grant them the Admin role on first login.
- **Security & Service Accounts:** The application can seamlessly impersonate backend Windows AD Service Accounts to interact with external databases securely (managed via Integrated Security), ensuring end-user credentials never touch the database.
- **Role-Based Access Control (RBAC):** Built-in support for multiple organizational roles (`Admin`, `IAO`, `IAA`, `User`), seamlessly integratable with Microsoft Entra ID (Azure AD).
- **Virtual Groups & Policies:** Enforce row-level security or specific slice access by mapping datasets to Virtual Groups and Access Policies.
- **KPI Dashboard:** A dynamic, beautiful dashboard tracking real-time metrics including "My Active Assets", "Pending Requests", 30-day activity trends, and dataset composition analytics.
- **Admin Control Centre:** Administrators can dynamically update underlying database connection strings, toggle Windows Authentication for backend services, verify current executing Service Accounts, and manage identity provider settings without touching the codebase.

## Application Architecture

The solution implements a strict Clean Architecture pattern divided into four primary layers:

1. **`DataProvisioning.Domain` (Core)**
   - Contains all enterprise logic, base Entities (e.g., `Dataset`, `AccessRequest`, `InitialAdmin`, `VirtualGroup`), Enums, and custom Exceptions.
   - **No dependencies.** The safest, most isolated layer of the application.

2. **`DataProvisioning.Application` (Use Cases)**
   - Houses the business rules, Services (`CatalogService`, `AccessRequestService`), Interfaces, Data Transfer Objects (DTOs), and ViewModels.
   - Only depends on the `Domain` layer.

3. **`DataProvisioning.Infrastructure` (Data & External)**
   - Connects to external systems. Contains the Entity Framework Core `ApplicationDbContext` mapped directly to SQL Server 2022.
   - Handles interactions with identity services or external APIs.
   - Depends on the `Application` layer to fullfil its defined interfaces.

4. **`DataProvisioning.WebUI` (Presentation)**
   - The ASP.NET Core MVC application providing the user interface.
   - Utilizes custom "Babcock" CSS styling built over Bootstrap grids, ensuring a responsive, glassmorphism-inspired dark theme.
   - Configures Dependency Injection (DI) and coordinates HTTP requests (including the `UserProvisioningMiddleware`).

5. **`DataProvisioning.UnitTests` & `DataProvisioning.IntegrationTests`**
   - xUnit based test projects to secure the application logic and ensure data adapters function correctly against the database context.

## Technology Stack

- **Framework:** .NET 8.0 SDK
- **Web App:** ASP.NET Core MVC
- **Data Access:** Entity Framework Core 8.0 (EF Core)
- **Database Target:** Microsoft SQL Server 2022
- **Frontend Stack:** HTML5, standard CSS3 (Flexbox/Grid), JavaScript, jQuery 3.x, Bootstrap 5 (Grids/Utilities).

## Getting Started for Developers

### Prerequisites
- Install the **.NET 8.0 SDK** (Windows x64).
- A local instance of SQL Server 2022 or SQL Server Express.

### Setup Instructions

1. **Open the Project:**
   Open the folder or `DataProvisioning.slnx` (or `.sln`) in Visual Studio 2022 or VS Code.

2. **Run Database Migrations:**
   Ensure your local database schema is up to date by running the Entity Framework Core migrations. Open a terminal, navigate to the WebUI project, and run:
   ```powershell
   cd DataProvisioning.WebUI
   dotnet ef database update --project ../DataProvisioning.Infrastructure --startup-project .
   ```

3. **Configure Settings:**
   - In `appsettings.json`, you can toggle `"TestMode": true` to use the dropdown-based login for easy local development, or `false` to test pure Windows Authentication.
   - You can define a master admin via `"InitialAdmin": "DOMAIN\\username"` in the settings, or statically insert them into the `initial_admins` SQL table.

4. **Run Locally:**
   Set `DataProvisioning.WebUI` as the Startup Project and press `F5` in Visual Studio, or run `dotnet run` from the terminal. 
   
5. **Admin Centre Configuration:**
   Once running, log in as an Administrator and navigate to **Administration -> Admin Centre**. From here you can configure:
   - Target Application SQL Server connection strings.
   - Target Data Warehouse (Scanning Target) database.
   - Toggle "Use Windows Auth" to use the executing Service Account instead of explicit SQL credentials.

## Deployment Success Criteria

When deploying this application, verify the following to ensure a successful deployment:

### Build & Startup
- ✅ Solution builds without errors (`dotnet build` succeeds)
- ✅ Application starts without crashing (`dotnet run` completes initialization)
- ✅ No unhandled exceptions in application logs
- ✅ Database migrations complete successfully

### UI & Theme
- ✅ Application loads at `http://localhost:5065` or `https://localhost:7250`
- ✅ **White Nucleus theme is visible:**
  - Light gray background (`#F5F7FA`)
  - White cards and surfaces (`#FFFFFF`)
  - Dark navy left sidebar (`#1B2A4A`)
  - Teal accent colors (`#2E8B8B`)
  - Modern DM Sans typography
- ✅ Sidebar navigation is properly rendered with icons and sections

### Functionality
- ✅ Login/authentication works (Windows Auth or TestMode dropdown)
- ✅ User is automatically provisioned on first login
- ✅ Sidebar navigation links are functional and route correctly
- ✅ Dashboard page loads and displays content
- ✅ Data Catalog page is accessible
- ✅ My Requests page loads without errors
- ✅ Admin pages are accessible for administrators
- ✅ Role-based access control (RBAC) works correctly

### Responsiveness
- ✅ UI is responsive on desktop browsers (1920x1080, 1366x768)
- ✅ Sidebar remains fixed and accessible on all screen sizes
- ✅ No layout breaking or text overflow issues
- ✅ All buttons and forms are properly aligned and functional

### Database
- ✅ SQL Server connection established successfully
- ✅ All tables created and accessible
- ✅ User data persists across page refreshes
- ✅ Database queries execute without timeouts or deadlocks

## Deployment Failure Criteria

**Stop deployment and investigate immediately if any of these occur:**

### Critical Build/Runtime Failures
- ❌ Build fails with compilation errors
- ❌ Application crashes on startup or throws unhandled exceptions
- ❌ Cannot establish connection to SQL Server database
- ❌ Database migrations fail or roll back
- ❌ Application port binding fails (port already in use or permission denied)

### UI/Theme Issues
- ❌ Old dark/black theme appears instead of white Nucleus theme
- ❌ CSS files fail to load (404 errors in browser console)
- ❌ Sidebar does not appear or is visually broken
- ❌ Images, fonts, or icons fail to load
- ❌ Layout is broken or severely misaligned

### Authentication & Authorization
- ❌ Login page doesn't load or is inaccessible
- ❌ Cannot authenticate with valid credentials
- ❌ User provisioning fails silently
- ❌ Unauthorized users can access protected pages
- ❌ Admin users lack required permissions

### Functionality Failures
- ❌ Navigation links return 404 or 500 errors
- ❌ Dashboard page crashes or loads indefinitely
- ❌ Database queries timeout or return errors
- ❌ Forms cannot be submitted or data is not saved
- ❌ Critical features are missing or non-functional

### Performance & Stability
- ❌ Page load times exceed 5 seconds for initial load
- ❌ Application crashes under normal user load
- ❌ Memory leaks or high CPU usage detected
- ❌ Session timeouts occur unexpectedly
- ❌ Multiple console errors or warnings on each page load
