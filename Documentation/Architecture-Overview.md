# SmartPPC - Architecture Overview

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           CLIENT (Browser)                               │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────┐   │
│  │                    User Interface (HTML/CSS)                    │   │
│  │                      MudBlazor Components                       │   │
│  └────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│                              ↕ SignalR (WebSocket)                      │
└─────────────────────────────────────────────────────────────────────────┘
                                      ↕
┌─────────────────────────────────────────────────────────────────────────┐
│                    BLAZOR SERVER (.NET 8)                                │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    Presentation Layer                             │  │
│  │  ┌────────────┐  ┌────────────┐  ┌───────────────┐             │  │
│  │  │  Pages     │  │  Layouts   │  │  Components   │             │  │
│  │  │  (.razor)  │  │  (.razor)  │  │  (.razor)     │             │  │
│  │  └────────────┘  └────────────┘  └───────────────┘             │  │
│  │         │               │                 │                      │  │
│  │         └───────────────┴─────────────────┘                      │  │
│  │                         │                                         │  │
│  │                         ↓                                         │  │
│  │  ┌──────────────────────────────────────────────────────────┐   │  │
│  │  │            App.razor (Router)                            │   │  │
│  │  │  - CascadingAuthenticationState                          │   │  │
│  │  │  - AuthorizeRouteView                                    │   │  │
│  │  └──────────────────────────────────────────────────────────┘   │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    Service Layer                                  │  │
│  │  ┌────────────────────┐  ┌─────────────────────────────────┐    │  │
│  │  │ ConfigurationService│  │ RevalidatingIdentityAuth...     │    │  │
│  │  └────────────────────┘  └─────────────────────────────────┘    │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                Authentication & Authorization                     │  │
│  │  ┌──────────────────┐  ┌──────────────────┐                      │  │
│  │  │  SignInManager   │  │  UserManager     │                      │  │
│  │  │  <User>          │  │  <User>          │                      │  │
│  │  └──────────────────┘  └──────────────────┘                      │  │
│  │  ┌─────────────────────────────────────────────────────────┐    │  │
│  │  │      ASP.NET Core Identity + Cookie Authentication       │    │  │
│  │  └─────────────────────────────────────────────────────────┘    │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    Data Access Layer                              │  │
│  │  ┌────────────────────────────────────────────────────────┐      │  │
│  │  │      ApplicationDbContext (Entity Framework Core)      │      │  │
│  │  └────────────────────────────────────────────────────────┘      │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    REST API Layer (Optional)                      │  │
│  │  ┌────────────────────────────────────────────────────────┐      │  │
│  │  │      ProductionPlanningController                      │      │  │
│  │  │      (For external API consumers)                      │      │  │
│  │  └────────────────────────────────────────────────────────┘      │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
                                      ↕
┌─────────────────────────────────────────────────────────────────────────┐
│                     DATABASE (PostgreSQL)                                │
│                                                                          │
│  ┌──────────────┐  ┌────────────────┐  ┌─────────────────┐           │
│  │ AspNetUsers  │  │ AspNetRoles    │  │ AspNetUserClaims│           │
│  └──────────────┘  └────────────────┘  └─────────────────┘           │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────┐      │
│  │          Application-Specific Tables                         │      │
│  │    (ModelInputs, StationDeclarations, etc.)                 │      │
│  └─────────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Request Flow

### 1. Initial Page Load (Unauthenticated User)

```
┌─────────┐
│ Browser │
└────┬────┘
     │
     │ 1. HTTP GET /
     │
     ▼
┌────────────────┐
│  _Host.cshtml  │  (Entry point)
└────┬───────────┘
     │
     │ 2. Load App.razor with render-mode="Server"
     │
     ▼
┌────────────────────┐
│   App.razor        │
│   (Router)         │
└────┬───────────────┘
     │
     │ 3. SignalR connection established
     │
     ▼
┌─────────────────────────────┐
│  AuthorizeRouteView         │
│  checks authorization       │
└────┬────────────────────────┘
     │
     │ 4. User not authenticated?
     │
     ▼
┌─────────────────────────────┐
│  <NotAuthorized>            │
│  Shows "Access Denied"      │
│  with Login button          │
└─────────────────────────────┘
```

### 2. Login Flow

```
┌─────────┐
│ Browser │
└────┬────┘
     │
     │ 1. User navigates to /Authentication/Login
     │
     ▼
┌────────────────────┐
│  Login.razor       │  (Public page, no [Authorize])
└────┬───────────────┘
     │
     │ 2. User enters email/password
     │    Clicks "Sign In"
     │
     ▼
┌────────────────────────────────────┐
│  HandleLogin() method              │
│  ┌──────────────────────────────┐ │
│  │ 1. Check SemaphoreSlim       │ │
│  │    (prevent concurrent)      │ │
│  │                              │ │
│  │ 2. Set _isLoading = true     │ │
│  │    StateHasChanged()         │ │
│  │                              │ │
│  │ 3. await Task.Delay(100)     │ │
│  │    (stabilize SignalR)       │ │
│  └──────────────────────────────┘ │
└────┬───────────────────────────────┘
     │
     │ 4. Call SignInManager.PasswordSignInAsync()
     │
     ▼
┌──────────────────────────────────────┐
│  ASP.NET Core Identity               │
│  ┌────────────────────────────────┐  │
│  │ 1. UserManager.FindByNameAsync │  │
│  │    Query database for user     │  │
│  │                                │  │
│  │ 2. Validate password hash      │  │
│  │                                │  │
│  │ 3. Load user claims & roles    │  │
│  │                                │  │
│  │ 4. Create authentication       │  │
│  │    cookie                      │  │
│  └────────────────────────────────┘  │
└──────────────────┬───────────────────┘
                   │
                   │ 5. Return SignInResult
                   │
                   ▼
┌────────────────────────────────────┐
│  HandleLogin() (continued)         │
│  ┌──────────────────────────────┐ │
│  │ if (result.Succeeded)        │ │
│  │ {                            │ │
│  │   JavaScript redirect:       │ │
│  │   window.location.href = "/" │ │
│  │ }                            │ │
│  └──────────────────────────────┘ │
└────┬───────────────────────────────┘
     │
     │ 6. Browser receives JavaScript command
     │
     ▼
┌─────────────────────────────┐
│  Full page reload           │
│  (with authentication       │
│   cookie in headers)        │
└────┬────────────────────────┘
     │
     │ 7. GET / with auth cookie
     │
     ▼
┌────────────────────────────────────┐
│  AuthenticationStateProvider       │
│  recognizes authenticated user     │
└────┬───────────────────────────────┘
     │
     │ 8. User authorized
     │
     ▼
┌─────────────────────────────┐
│  Index.razor loads          │
│  (Dashboard)                │
└─────────────────────────────┘
```

### 3. Accessing Protected Page (Authenticated)

```
┌─────────┐
│ Browser │
└────┬────┘
     │
     │ 1. GET /Settings/GeneralSettings
     │    (with authentication cookie)
     │
     ▼
┌────────────────────────────────────┐
│  AuthorizeRouteView                │
│  ┌──────────────────────────────┐  │
│  │ 1. Check authentication      │  │
│  │    cookie                    │  │
│  │                              │  │
│  │ 2. Validate cookie           │  │
│  │                              │  │
│  │ 3. Load user claims          │  │
│  └──────────────────────────────┘  │
└────┬───────────────────────────────┘
     │
     │ User authenticated?
     │
     ├── NO ──▶ Show <NotAuthorized>
     │
     └── YES
         │
         ▼
┌─────────────────────────────────┐
│  GeneralSettings.razor          │
│  ┌───────────────────────────┐  │
│  │ OnInitializedAsync()      │  │
│  │ {                         │  │
│  │   var config = await      │  │
│  │     ConfigService         │  │
│  │     .LoadConfigurationAsync()│
│  │ }                         │  │
│  └───────────────────────────┘  │
└────┬────────────────────────────┘
     │
     │ 2. Inject ConfigurationService
     │
     ▼
┌─────────────────────────────────┐
│  ConfigurationService           │
│  ┌───────────────────────────┐  │
│  │ Load from database or     │  │
│  │ configuration file        │  │
│  └───────────────────────────┘  │
└────┬────────────────────────────┘
     │
     │ 3. Return data
     │
     ▼
┌─────────────────────────────────┐
│  Page renders with data         │
└─────────────────────────────────┘
```

---

## Component Communication Patterns

### 1. Parent → Child (Parameters)

```razor
<!-- Parent Component -->
<ChildComponent
    Title="@pageTitle"
    Data="@modelData"
    OnDataChanged="HandleDataChanged" />

<!-- Child Component -->
@code {
    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public ModelData Data { get; set; }

    [Parameter]
    public EventCallback<ModelData> OnDataChanged { get; set; }
}
```

### 2. Service-Based Communication

```
┌──────────────┐
│   Page A     │
└──────┬───────┘
       │
       │ Inject ConfigurationService
       │
       ▼
┌─────────────────────────┐
│  ConfigurationService   │  (Scoped service)
│  - Shared state         │
│  - Business logic       │
└──────▲──────────────────┘
       │
       │ Inject ConfigurationService
       │
┌──────┴───────┐
│   Page B     │
└──────────────┘
```

### 3. Cascading Values

```razor
<CascadingAuthenticationState>
    <!-- All child components can access AuthenticationState -->
    <Router>
        ...
    </Router>
</CascadingAuthenticationState>

<!-- Child component anywhere in tree -->
@code {
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; }
}
```

---

## Data Flow Example: Loading Configuration

```
┌─────────────────┐
│ User clicks     │
│ "General        │
│  Settings"      │
└────┬────────────┘
     │
     ▼
┌──────────────────────────────────────────┐
│  GeneralSettings.razor                   │
│  protected override async Task           │
│  OnInitializedAsync()                    │
└────┬─────────────────────────────────────┘
     │
     │ @inject ConfigurationService ConfigService
     │
     ▼
┌──────────────────────────────────────────┐
│  ConfigurationService                    │
│  public async Task<ModelInputs>          │
│  LoadConfigurationAsync()                │
└────┬─────────────────────────────────────┘
     │
     │ Inject ApplicationDbContext
     │
     ▼
┌──────────────────────────────────────────┐
│  ApplicationDbContext                    │
│  (Entity Framework Core)                 │
└────┬─────────────────────────────────────┘
     │
     │ Generate SQL query
     │ SELECT * FROM ModelInputs...
     │
     ▼
┌──────────────────────────────────────────┐
│  PostgreSQL Database                     │
└────┬─────────────────────────────────────┘
     │
     │ Return data
     │
     ▼
┌──────────────────────────────────────────┐
│  ConfigurationService                    │
│  Map data to ModelInputs object          │
└────┬─────────────────────────────────────┘
     │
     │ Return ModelInputs
     │
     ▼
┌──────────────────────────────────────────┐
│  GeneralSettings.razor                   │
│  ModelInputs = config;                   │
│  StateHasChanged();                      │
└────┬─────────────────────────────────────┘
     │
     │ Blazor re-renders component
     │
     ▼
┌──────────────────────────────────────────┐
│  Browser UI updates via SignalR          │
└──────────────────────────────────────────┘
```

---

## Authentication State Management

```
┌────────────────────────────────────────────────────────────┐
│  RevalidatingIdentityAuthenticationStateProvider           │
│  - Periodically revalidates user authentication            │
│  - Checks if user still exists in database                 │
│  - Validates security stamp hasn't changed                 │
└────┬───────────────────────────────────────────────────────┘
     │
     │ Provides AuthenticationState
     │
     ▼
┌────────────────────────────────────────────────────────────┐
│  CascadingAuthenticationState                              │
│  (in App.razor)                                            │
└────┬───────────────────────────────────────────────────────┘
     │
     │ Cascades to all child components
     │
     ├─────────────┬─────────────┬─────────────┐
     ▼             ▼             ▼             ▼
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│ Page 1   │  │ Page 2   │  │ Layout   │  │ Component│
│ @code {  │  │ @code {  │  │ @code {  │  │ @code {  │
│ [Cascade]│  │ [Cascade]│  │ [Cascade]│  │ [Cascade]│
│ Auth...  │  │ Auth...  │  │ Auth...  │  │ Auth...  │
│ }        │  │ }        │  │ }        │  │ }        │
└──────────┘  └──────────┘  └──────────┘  └──────────┘
```

---

## Security Layers

```
┌──────────────────────────────────────────────────────────┐
│  Layer 1: Network Security                               │
│  - HTTPS enforcement                                     │
│  - HttpOnly cookies                                      │
│  - CORS configuration                                    │
└──────────────────────────────────────────────────────────┘
                          ↓
┌──────────────────────────────────────────────────────────┐
│  Layer 2: Authentication                                 │
│  - Cookie authentication                                 │
│  - SignInManager validates credentials                   │
│  - Password hashing (Identity default)                   │
└──────────────────────────────────────────────────────────┘
                          ↓
┌──────────────────────────────────────────────────────────┐
│  Layer 3: Authorization                                  │
│  - [Authorize] attribute on pages                        │
│  - AuthorizeRouteView enforcement                        │
│  - Role-based authorization (if configured)              │
└──────────────────────────────────────────────────────────┘
                          ↓
┌──────────────────────────────────────────────────────────┐
│  Layer 4: Data Access                                    │
│  - Entity Framework parameterized queries                │
│  - SQL injection prevention                              │
│  - Data validation                                       │
└──────────────────────────────────────────────────────────┘
```

---

## Deployment Architecture (Future)

### Current: Monolithic Blazor Server

```
┌────────────────────────────────────┐
│        Single Server               │
│  ┌──────────────────────────────┐ │
│  │   Blazor Server App          │ │
│  │   - Pages                    │ │
│  │   - Services                 │ │
│  │   - Authentication           │ │
│  │   - Database access          │ │
│  └──────────────────────────────┘ │
└────────────────────────────────────┘
                ↓
     ┌────────────────────┐
     │  PostgreSQL        │
     │  Database          │
     └────────────────────┘
```

### Future Option 1: Blazor WebAssembly + API

```
┌──────────────────┐           ┌───────────────────┐
│  Client (WASM)   │  ←HTTP→   │   Web API         │
│  - UI Only       │           │   - Business      │
│  - No secrets    │           │     Logic         │
└──────────────────┘           │   - Auth          │
                               │   - Data Access   │
                               └─────────┬─────────┘
                                         ↓
                                  ┌──────────────┐
                                  │  PostgreSQL  │
                                  └──────────────┘
```

### Future Option 2: Microservices

```
┌──────────────┐        ┌────────────────────┐
│ Blazor UI    │ ←───→  │  API Gateway       │
└──────────────┘        └─────────┬──────────┘
                                  │
                    ┌─────────────┼─────────────┐
                    ↓             ↓             ↓
            ┌───────────┐  ┌──────────┐  ┌──────────┐
            │ Auth       │  │ Planning │  │ Solver   │
            │ Service    │  │ Service  │  │ Service  │
            └─────┬──────┘  └────┬─────┘  └────┬─────┘
                  ↓              ↓             ↓
            ┌─────────┐    ┌─────────┐   ┌─────────┐
            │ Auth DB │    │ Plan DB │   │ Calc DB │
            └─────────┘    └─────────┘   └─────────┘
```

---

## Performance Considerations

### SignalR Circuit Management

```
User connects
     ↓
Circuit created (in-memory state)
     ↓
Maintains WebSocket connection
     ↓
┌──────────────────────────────────┐
│  Circuit Lifecycle               │
│  - DisconnectedCircuitRetention  │
│    Period: 3 minutes             │
│  - Max circuits: 100             │
│  - JS Interop timeout: 1 min     │
└──────────────────────────────────┘
     ↓
User disconnects or times out
     ↓
Circuit disposed (memory freed)
```

### Database Connection Pooling

```
┌──────────────────────────┐
│  EF Core DbContext       │
│  (Scoped per circuit)    │
└────────┬─────────────────┘
         │
         ▼
┌──────────────────────────┐
│  Connection Pool         │
│  - Min connections: 0    │
│  - Max connections: 100  │
│  - Reuse connections     │
└────────┬─────────────────┘
         │
         ▼
┌──────────────────────────┐
│  PostgreSQL Server       │
└──────────────────────────┘
```

---

**Document Version:** 1.0
**Last Updated:** 2025-01-25
