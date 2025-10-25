# SmartPPC - Pages and Authentication System Documentation

## Table of Contents
1. [Overview](#overview)
2. [Blazor Pages Architecture](#blazor-pages-architecture)
3. [Authentication System](#authentication-system)
4. [Authorization Flow](#authorization-flow)
5. [Page Structure](#page-structure)
6. [Technical Implementation Details](#technical-implementation-details)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Overview

SmartPPC is built using **ASP.NET Core Blazor Server**, a framework that allows building interactive web applications using C# instead of JavaScript. The application uses **ASP.NET Core Identity** for authentication and authorization.

### Key Technologies
- **Blazor Server**: Server-side rendering with real-time UI updates via SignalR
- **ASP.NET Core Identity**: User authentication and authorization framework
- **MudBlazor**: Material Design component library for Blazor
- **Entity Framework Core**: ORM for database access (PostgreSQL)

---

## Blazor Pages Architecture

### What is a Blazor Page?

A **Blazor page** (also called a Razor component) is a self-contained UI component defined in a `.razor` file. It combines:
- **HTML markup** for the user interface
- **C# code** for logic and behavior
- **Component parameters** for data passing
- **Lifecycle methods** for initialization and updates

### Example Structure

```razor
@page "/Settings/GeneralSettings"
@attribute [Authorize]
@using SmartPPC.Api.Services
@inject ConfigurationService ConfigService

<h1>General Settings</h1>

<GeneralSettingsComponent ModelInputs="ModelInputs" />

@code {
    private ModelInputs ModelInputs { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        var config = await ConfigService.LoadConfigurationAsync();
        ModelInputs = config ?? ConfigService.CreateDefaultConfiguration();
    }
}
```

### Key Components

1. **@page directive**: Defines the route/URL for the page
2. **@attribute [Authorize]**: Marks the page as requiring authentication
3. **@using**: Import namespaces
4. **@inject**: Dependency injection for services
5. **@code block**: C# logic for the component

### Why No Controllers?

In **Blazor Server**, pages handle their own logic directly:
- ✅ **Pages for UI**: `.razor` files with `@page` directive
- ✅ **Services for Business Logic**: Injected services (e.g., `ConfigurationService`)
- ✅ **Controllers for APIs**: Only needed for REST API endpoints (e.g., `ProductionPlanningController`)

This is the **recommended Microsoft pattern** for Blazor Server monolithic applications.

**Controllers are only needed when:**
- Building REST APIs for external consumers
- Supporting multiple client types (mobile apps, SPAs)
- Planning to migrate to Blazor WebAssembly
- Using microservices architecture

---

## Authentication System

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Browser (User)                        │
└───────────────┬─────────────────────────────────────────┘
                │
                ↓ (SignalR/WebSocket)
┌─────────────────────────────────────────────────────────┐
│              Blazor Server Application                   │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │  App.razor (Router Configuration)              │    │
│  │  - CascadingAuthenticationState                │    │
│  │  - AuthorizeRouteView                          │    │
│  └────────────────┬───────────────────────────────┘    │
│                   │                                      │
│  ┌────────────────▼───────────────────────────────┐    │
│  │  ASP.NET Core Identity                         │    │
│  │  - SignInManager<User>                         │    │
│  │  - UserManager<User>                           │    │
│  │  - Cookie Authentication                       │    │
│  └────────────────┬───────────────────────────────┘    │
│                   │                                      │
│  ┌────────────────▼───────────────────────────────┐    │
│  │  RevalidatingIdentityAuthenticationStateProvider│   │
│  │  - Provides authentication state to components │    │
│  └────────────────┬───────────────────────────────┘    │
│                   │                                      │
│  ┌────────────────▼───────────────────────────────┐    │
│  │  Database (PostgreSQL)                         │    │
│  │  - AspNetUsers                                 │    │
│  │  - AspNetRoles                                 │    │
│  │  - AspNetUserClaims                            │    │
│  └────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

### Components

#### 1. ASP.NET Core Identity
Located in `Program.cs`:

```csharp
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

#### 2. Cookie Configuration
Configured for Blazor Server with special handling for SignalR:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Authentication/Login";
    options.AccessDeniedPath = "/Authentication/AccessDenied";

    // Critical: Prevent redirects on SignalR paths
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/_blazor"))
            {
                context.Response.StatusCode = 401;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }
            return Task.CompletedTask;
        }
    };
});
```

**Why this is needed:** Blazor Server uses SignalR (WebSocket), not traditional HTTP. Cookie redirects must be handled differently for SignalR paths.

#### 3. Authentication State Provider
`RevalidatingIdentityAuthenticationStateProvider<User>` provides authentication state to all components.

---

## Authorization Flow

### 1. Accessing a Protected Page

```
User navigates to "/" (protected page)
        ↓
App.razor Router checks AuthorizeRouteView
        ↓
Is user authenticated?
        ↓
    ┌───┴───┐
    NO      YES
    │       │
    ↓       ↓
Show "Access Denied"    Load page normally
with Login button
```

### 2. Login Process

```
1. User enters credentials on Login.razor
        ↓
2. HandleLogin() method called
        ↓
3. SemaphoreSlim prevents concurrent execution
        ↓
4. SignInManager.PasswordSignInAsync() called
        ↓
5. ASP.NET Core Identity validates credentials
        ↓
6. Authentication cookie set
        ↓
7. JavaScript redirect: window.location.href = "/"
        ↓
8. Browser makes fresh request with auth cookie
        ↓
9. AuthenticationStateProvider recognizes user
        ↓
10. Protected pages now accessible
```

### 3. Registration Process

```
1. User fills registration form on Register.razor
        ↓
2. HandleRegister() validates passwords match
        ↓
3. UserManager.CreateAsync() creates user
        ↓
4. SignInManager.SignInAsync() logs user in
        ↓
5. JavaScript redirect to "/"
        ↓
6. User is authenticated and can access protected pages
```

---

## Page Structure

### Application Pages

#### Public Pages (No Authentication Required)
- **`/Authentication/Login`** - User login page
- **`/Authentication/Register`** - User registration page
- **`/Authentication/AccessDenied`** - Access denied page

#### Protected Pages (Authentication Required)
All have `@attribute [Authorize]`:

- **`/` (Index.razor)** - Dashboard/home page
- **`/Settings/GeneralSettings`** - DDMRP configuration settings
- **`/Stations/StationConfig`** - Production station configuration
- **`/Forecast/DemandForecast`** - Demand forecasting
- **`/SolverPage`** - Genetic algorithm solver
- **`/Profile`** - User profile management

### Layout Components

#### AppLayout.razor
Main application layout with:
- Navigation drawer (MudDrawer)
- App bar with user menu
- Theme toggle (dark/light mode)
- Authentication controls

```razor
<AuthorizeView>
    <Authorized>
        <!-- Show user menu with Profile/Logout -->
    </Authorized>
    <NotAuthorized>
        <!-- Show Login button -->
    </NotAuthorized>
</AuthorizeView>
```

#### EmptyLayout.razor
Used for authentication pages (login/register) without navigation.

---

## Technical Implementation Details

### 1. Render Mode Configuration

**File:** `Pages/_Host.cshtml`

```cshtml
<component type="typeof(App)" render-mode="Server" />
```

**Why `Server` mode (not `ServerPrerendered`):**
- `ServerPrerendered` causes two renders (prerender + interactive)
- During prerender, HttpContext is not available
- SignInManager requires HttpContext, causing errors
- `Server` mode only renders after SignalR connection is established
- Ensures HttpContext is always available for authentication

### 2. Router Configuration

**File:** `App.razor`

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData"
                                DefaultLayout="@typeof(Pages.Shared.AppLayout)">
                <Authorizing>
                    <!-- Loading spinner -->
                </Authorizing>
                <NotAuthorized>
                    <!-- Access denied message -->
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</CascadingAuthenticationState>
```

**Key Points:**
- `AuthorizeRouteView` enforces `[Authorize]` attributes (RouteView does not!)
- `CascadingAuthenticationState` provides auth state to all child components
- `<Authorizing>` shows while checking authentication
- `<NotAuthorized>` shows when user lacks permission

### 3. Concurrency Control in Authentication

**Problem:** Blazor's rendering cycle can trigger form submissions twice, causing:
- Duplicate database queries
- "Headers are read-only" errors
- Race conditions

**Solution:** SemaphoreSlim for thread-safe locking

**File:** `Login.razor`

```csharp
private readonly SemaphoreSlim _loginSemaphore = new SemaphoreSlim(1, 1);

private async Task HandleLogin()
{
    // Prevent concurrent execution
    if (!await _loginSemaphore.WaitAsync(0))
    {
        return; // Already processing
    }

    try
    {
        _isLoading = true;
        StateHasChanged(); // Update UI immediately

        await Task.Delay(100); // Stabilize SignalR connection

        var result = await SignInManager.PasswordSignInAsync(...);

        if (result.Succeeded)
        {
            // JavaScript redirect for full page reload
            await JSRuntime.InvokeVoidAsync("eval",
                $"window.location.href = '{redirectUrl}'");
        }
    }
    catch (Exception ex)
    {
        // Handle error
        _loginSemaphore.Release();
    }
}
```

**How it works:**
1. `WaitAsync(0)` tries to acquire lock without waiting
2. First call succeeds and proceeds
3. Concurrent calls return immediately
4. Lock only released on failure (success redirects away)

### 4. JavaScript Redirect vs NavigationManager

**Why JavaScript redirect:**

```csharp
// ✅ CORRECT: JavaScript redirect
await JSRuntime.InvokeVoidAsync("eval", "window.location.href = '/'");

// ❌ WRONG: NavigationManager in Blazor Server after SignIn
Navigation.NavigateTo("/", forceLoad: true);
```

**Reason:** After `SignInAsync`, authentication cookie is set. Using `NavigationManager` can cause:
- Headers already sent errors (SignalR connection open)
- Cookie not properly recognized
- Authentication state not updated

JavaScript redirect causes a **full page reload** with proper cookie handling.

---

## Best Practices

### 1. Page Development

✅ **DO:**
- Use `@attribute [Authorize]` on protected pages
- Inject services for business logic
- Keep page components focused on UI
- Use child components for complex UI sections
- Implement proper error handling

❌ **DON'T:**
- Put business logic directly in pages
- Use controllers for UI (Blazor Server doesn't need them)
- Mix authentication logic with UI logic

### 2. Authentication

✅ **DO:**
- Use `SignInManager` and `UserManager` from ASP.NET Core Identity
- Implement proper concurrency control (SemaphoreSlim)
- Use JavaScript redirect after authentication
- Handle errors gracefully with user feedback

❌ **DON'T:**
- Use `Response.Redirect()` in Blazor Server
- Use `ServerPrerendered` mode with authentication
- Allow concurrent form submissions

### 3. Service Pattern

```csharp
// ✅ CORRECT: Service handles business logic
public class ConfigurationService
{
    public async Task<ModelInputs> LoadConfigurationAsync() { ... }
    public async Task SaveConfigurationAsync(ModelInputs config) { ... }
}

// Page uses service
@inject ConfigurationService ConfigService

@code {
    protected override async Task OnInitializedAsync()
    {
        var config = await ConfigService.LoadConfigurationAsync();
    }
}
```

### 4. State Management

- **Component State**: Private fields in `@code` block
- **Cascading Parameters**: Pass data down component tree
- **Services (Scoped)**: Share data across components in same circuit
- **Services (Singleton)**: Share data across all users (use carefully!)

---

## Troubleshooting

### Common Issues

#### 1. "Headers are read-only, response has already started"

**Cause:** Trying to use `Response.Redirect()` after SignalR connection started

**Solution:**
- Use JavaScript redirect: `JSRuntime.InvokeVoidAsync("eval", "window.location.href = '...'")`
- Configure cookie events to handle SignalR paths
- Use `Server` render mode (not `ServerPrerendered`)

#### 2. "HttpContext must not be null"

**Cause:** Using `ServerPrerendered` render mode causes prerendering without HttpContext

**Solution:** Change to `Server` mode in `_Host.cshtml`

#### 3. Authorization Not Working

**Checklist:**
- ✅ Is `@attribute [Authorize]` on the page?
- ✅ Is `AuthorizeRouteView` used in `App.razor` (not `RouteView`)?
- ✅ Is `CascadingAuthenticationState` wrapping the Router?
- ✅ Is `AuthenticationStateProvider` registered in `Program.cs`?

#### 4. Concurrent Execution Errors

**Cause:** Form submitted multiple times during render cycle

**Solution:** Implement SemaphoreSlim locking pattern (see Technical Details)

#### 5. Authentication Not Persisting

**Checklist:**
- ✅ Is cookie authentication configured in `Program.cs`?
- ✅ Is `app.UseAuthentication()` before `app.UseAuthorization()`?
- ✅ Are cookies enabled in browser?
- ✅ Is HTTPS configured properly?

---

## File Reference

### Key Configuration Files

| File | Purpose |
|------|---------|
| `Program.cs` | Application startup, service registration, middleware configuration |
| `App.razor` | Router configuration, authorization flow |
| `Pages/_Host.cshtml` | Blazor Server host page, render mode configuration |
| `Pages/_Layout.cshtml` | HTML layout wrapper |
| `Pages/Shared/AppLayout.razor` | Main application layout with navigation |
| `Pages/Shared/EmptyLayout.razor` | Minimal layout for auth pages |

### Authentication Pages

| File | Route | Purpose |
|------|-------|---------|
| `Pages/Authentication/Login.razor` | `/Authentication/Login` | User login |
| `Pages/Authentication/Register.razor` | `/Authentication/Register` | User registration |
| `Pages/Authentication/AccessDenied.razor` | `/Authentication/AccessDenied` | Access denied page |
| `Pages/Profile/Profile.razor` | `/Profile` | User profile management |

### Application Pages

| File | Route | Purpose |
|------|-------|---------|
| `Pages/Index.razor` | `/` | Dashboard |
| `Pages/Settings/GeneralSettings.razor` | `/Settings/GeneralSettings` | DDMRP settings |
| `Pages/Stations/StationConfig.razor` | `/Stations/StationConfig` | Station configuration |
| `Pages/Forecast/DemandForecast.razor` | `/Forecast/DemandForecast` | Demand forecasting |
| `Pages/SolverPage.razor` | `/SolverPage` | Genetic algorithm solver |

---

## Security Considerations

### Password Requirements
Configured in `Program.cs`:
- Minimum 6 characters
- Requires digit
- Requires lowercase letter
- Requires uppercase letter
- Non-alphanumeric optional (disabled)

### Cookie Security
- `HttpOnly = true` (prevents JavaScript access)
- `SlidingExpiration = true` (extends session with activity)
- `ExpireTimeSpan = 7 days`

### Database
- Passwords hashed using ASP.NET Core Identity's password hasher
- User data stored in PostgreSQL
- Entity Framework Core for parameterized queries (SQL injection protection)

---

## Further Reading

- [ASP.NET Core Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Blazor Server Hosting Model](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-8.0)
- [MudBlazor Component Library](https://mudblazor.com/)

---

**Document Version:** 1.0
**Last Updated:** 2025-01-25
**Author:** SmartPPC Development Team
