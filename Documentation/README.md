# SmartPPC Documentation

Welcome to the SmartPPC technical documentation. This directory contains comprehensive documentation for developers working on the SmartPPC application.

## Available Documentation

### [Architecture Overview](./Architecture-Overview.md)
Visual diagrams and flow charts showing the system architecture and request flows.

**Topics covered:**
- System Architecture Diagram
- Request Flow Diagrams
- Login Flow Visualization
- Component Communication Patterns
- Data Flow Examples
- Security Layers
- Deployment Architecture
- Performance Considerations

**Recommended for:**
- Getting a high-level understanding of the system
- Visual learners
- Understanding data flow
- Architecture reviews

### [Pages and Authentication System](./Pages-And-Authentication.md)
Complete guide to understanding how Blazor pages work and the authentication/authorization system.

**Topics covered:**
- Blazor Pages Architecture
- Authentication System Architecture
- Authorization Flow
- Page Structure and Routing
- Technical Implementation Details
- Security Considerations
- Troubleshooting Guide
- Best Practices

**Recommended for:**
- New developers joining the project
- Understanding the authentication flow
- Debugging authentication issues
- Learning Blazor Server patterns

---

## Quick Start

### Understanding the Application Architecture

1. **Start with visuals**: [Architecture Overview](./Architecture-Overview.md) - See diagrams and flow charts
2. **Deep dive**: [Pages and Authentication](./Pages-And-Authentication.md) - Understand the fundamentals
3. **Explore code**: `/SmartPPC.Api/Pages/` - Review actual page implementations
4. **Review configuration**: `/SmartPPC.Api/Program.cs` - See how services are configured

### Key Concepts

**Blazor Server** - Server-side rendering with real-time updates via SignalR
- Pages are `.razor` files combining HTML and C#
- No controllers needed for UI (only for REST APIs)
- Services handle business logic via dependency injection

**Authentication** - ASP.NET Core Identity with Cookie Authentication
- User credentials stored in PostgreSQL
- Passwords hashed securely
- Session managed via authentication cookies

**Authorization** - Page-level using `[Authorize]` attribute
- `AuthorizeRouteView` enforces authorization
- Redirects to login for unauthenticated users

---

## Project Structure Overview

```
SmartPPC.Api/
├── Pages/
│   ├── Authentication/         # Login, Register, Access Denied
│   ├── Forecast/              # Demand forecasting pages
│   ├── Profile/               # User profile management
│   ├── Settings/              # Application settings
│   ├── Shared/                # Layouts (AppLayout, EmptyLayout)
│   ├── Stations/              # Station configuration
│   ├── App.razor              # Router configuration
│   ├── Index.razor            # Dashboard
│   ├── SolverPage.razor       # Genetic algorithm solver
│   ├── _Host.cshtml           # Blazor Server host
│   └── _Imports.razor         # Global using statements
├── Services/
│   ├── ConfigurationService.cs               # Configuration management
│   └── RevalidatingIdentityAuthenticationStateProvider.cs
├── Data/
│   └── ApplicationDbContext.cs               # EF Core DbContext
├── Controllers/
│   └── ProductionPlanningController.cs       # REST API endpoints
└── Program.cs                                # Application startup

SmartPPC.Core/
├── Domain/
│   └── User.cs                # User entity
└── Model/
    └── DDMRP/                 # Domain models

Documentation/
├── README.md                   # This file
└── Pages-And-Authentication.md # Pages & auth guide
```

---

## Common Tasks

### Adding a New Protected Page

1. Create a new `.razor` file in appropriate folder
2. Add `@page` directive with route
3. Add `@attribute [Authorize]` for authentication
4. Inject required services
5. Implement page logic

Example:
```razor
@page "/MyNewPage"
@attribute [Authorize]
@inject MyService MyService

<h1>My New Page</h1>

@code {
    protected override async Task OnInitializedAsync()
    {
        // Initialize page
    }
}
```

### Adding a New Service

1. Create service class in `Services/` folder
2. Register in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<MyService>();
   ```
3. Inject in pages using `@inject MyService MyService`

### Modifying Authentication Rules

Edit `Program.cs`:
```csharp
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Modify password requirements
    options.Password.RequiredLength = 8;
    // Add other options
})
```

---

## Development Guidelines

### Code Style
- Follow C# coding conventions
- Use meaningful variable names
- Add XML comments for public methods
- Keep methods focused and small

### Blazor Best Practices
- Use services for business logic (not in pages)
- Keep components small and reusable
- Use proper async/await patterns
- Handle errors gracefully with try-catch

### Security
- Never store sensitive data in client-side code
- Always validate user input
- Use parameterized queries (Entity Framework does this)
- Keep dependencies updated

---

## Troubleshooting

For common issues and solutions, see the [Troubleshooting section](./Pages-And-Authentication.md#troubleshooting) in the Pages and Authentication documentation.

Quick links:
- [Headers are read-only error](./Pages-And-Authentication.md#1-headers-are-read-only-response-has-already-started)
- [HttpContext null error](./Pages-And-Authentication.md#2-httpcontext-must-not-be-null)
- [Authorization not working](./Pages-And-Authentication.md#3-authorization-not-working)

---

## Contributing to Documentation

When adding new documentation:

1. Create markdown files in this directory
2. Update this README with links
3. Follow the existing documentation structure
4. Include code examples where helpful
5. Add troubleshooting sections for common issues

---

## Additional Resources

### External Documentation
- [ASP.NET Core Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [MudBlazor Components](https://mudblazor.com/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

### Tools
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/)

---

**Need Help?**

If you can't find what you're looking for in this documentation, please:
1. Check the troubleshooting sections
2. Review the external documentation links
3. Contact the development team

---

*Last Updated: 2025-01-25*
