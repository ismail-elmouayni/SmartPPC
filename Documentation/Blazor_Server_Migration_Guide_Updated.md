
# 🧱 Migrating SmartPPC to Full Blazor Server with MudBlazor Layout

This guide explains how to convert your existing SmartPPC Razor Pages setup into a **fully integrated Blazor Server application** using MudBlazor components and layout providers.

---

## 🧩 Step 1 — Create `_Host.cshtml`

**Path:** `Pages/_Host.cshtml`

```razor
@page "/"
@namespace SmartPPC.Api.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = "_Layout";
}

<component type="typeof(App)" render-mode="ServerPrerendered" />
```

✅ This file is the main Razor Page that bootstraps your Blazor application. It uses `_Layout.cshtml` for the global HTML structure.

---

## 🧩 Step 2 — Create or Update `_Layout.cshtml`

**Path:** `Pages/Shared/_Layout.cshtml`

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - SmartPPC</title>
    <base href="~/" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
</head>
<body>
    <app>
        @RenderBody()
    </app>

    <script src="_framework/blazor.server.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

✅ This provides the base HTML wrapper and includes Blazor + MudBlazor scripts.

---

## 🧩 Step 3 — Create `App.razor`

**Path:** `/App.razor`

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(Pages.Shared.AppLayout)" />
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <LayoutView Layout="@typeof(Pages.Shared.AppLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

✅ The router maps URLs to Blazor components and defines the default layout.

---

## 🧩 Step 4 — Update `AppLayout.razor`

**Path:** `Pages/Shared/AppLayout.razor`

```razor
@inherits LayoutComponentBase

<MudThemeProvider>
    <MudPopoverProvider>
        <MudDialogProvider>
            <MudSnackbarProvider>
                <MudAppBar Color="Color.Primary" Elevation="1">
                    <MudText Typo="Typo.h6" Class="ml-2">SmartPPC</MudText>
                </MudAppBar>

                <MudContainer MaxWidth="MaxWidth.Large" Class="mt-4 mb-8">
                    @Body
                </MudContainer>
            </MudSnackbarProvider>
        </MudDialogProvider>
    </MudPopoverProvider>
</MudThemeProvider>
```

✅ This ensures all MudBlazor services (Snackbar, Dialog, etc.) are available everywhere.

---

## 🧩 Step 5 — Update `Program.cs`

Make sure your app is configured as a **Blazor Server app**:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Razor Pages + Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MudBlazor services
builder.Services.AddMudServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub(); // 👈 Important for Blazor Server
app.MapFallbackToPage("/_Host"); // 👈 Use _Host.cshtml as entry page

app.Run();
```

✅ This registers Blazor Server services and routing.

---

## 🧩 Step 6 — Convert Razor Pages to Blazor Components

### DemandForecast Page

Rename your old Razor Page:

**From:** `Pages/Forecast/DemandForecast.cshtml`  
**To:** `Pages/Forecast/DemandForecast.razor`

```razor
@page "/Forecast/DemandForecast"
@using SmartPPC.Core.Model.DDMRP
@using SmartPPC.Api.Services
@layout SmartPPC.Api.Pages.Shared.AppLayout

<DemandForecastComponent ModelInputs="ModelInputs" />
```

✅ The page is now a true Blazor component handled by the router.

### Index / Dashboard Page

Rename your old Razor Page:

**From:** `Pages/Index.cshtml`  
**To:** `Pages/Index.razor`

```razor
@page "/"
@using MudBlazor
@layout SmartPPC.Api.Pages.Shared.AppLayout
@inject SmartPPC.Api.Services.ConfigurationService ConfigService

<MudGrid>
    <MudItem xs="12">
        <MudPaper Class="pa-6" Elevation="2">
            <MudText Typo="Typo.h3" Class="mb-2">SmartPPC - Production Planning & Control</MudText>
            <MudText Typo="Typo.body1" Color="Color.Secondary">
                Demand-Driven Material Requirements Planning (DDMRP) System
            </MudText>
        </MudPaper>
    </MudItem>

    <!-- Additional dashboard cards for Settings, Stations, Demand Forecast, Solver, etc. -->
</MudGrid>
```

✅ Key changes:
- `@page "/"` makes it routable in Blazor.
- Removed `@model` — Blazor doesn’t use MVC models.
- Added `@layout AppLayout` to wrap it in MudBlazor providers.
- Any data should be injected via `@inject` or passed as parameters.

---

## ✅ Final Runtime Flow

```
Request → / (or /Forecast/DemandForecast, etc.)
   ↓
_Host.cshtml (bootstraps Blazor)
   ↓
App.razor (router)
   ↓
AppLayout.razor (layout + Mud providers)
   ↓
Index.razor / DemandForecast.razor (page)
   ↓
DemandForecastComponent (interactive UI, if any)
```

---

## 🗂️ Recommended Folder Structure

```
SmartPPC.Api/
│
├── Pages/
│   ├── _Host.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   └── AppLayout.razor
│   ├── Forecast/
│   │   └── DemandForecast.razor
│   └── Index.razor
│
├── App.razor
├── Program.cs
└── _Imports.razor
```

✅ With this structure, all MudBlazor providers work correctly, and Blazor routing handles every page seamlessly.
