using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using SmartPPC.Api.Data;
using SmartPPC.Api.Endpoints;
using SmartPPC.Core.Domain;

namespace Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add Database Context - using Aspire-provided PostgreSQL connection
        builder.AddNpgsqlDbContext<ApplicationDbContext>("smartppc");

        // Add ASP.NET Core Identity
        builder.Services.AddIdentity<User, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;

            // User settings
            options.User.RequireUniqueEmail = true;

            // Sign in settings
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Configure application cookie for Blazor Server
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.LoginPath = "/Authentication/Login";
            options.AccessDeniedPath = "/Authentication/AccessDenied";
            options.SlidingExpiration = true;

            // Critical for Blazor Server: Prevent redirects on AJAX/SignalR requests
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    // Check if this is a SignalR/AJAX request
                    if (context.Request.Path.StartsWithSegments("/_blazor"))
                    {
                        context.Response.StatusCode = 401;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }
                    return Task.CompletedTask;
                },

                OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/_blazor"))
                    {
                        context.Response.StatusCode = 403;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor(options =>
        {
            // Increase the disconnect timeout to help with authentication operations
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
            options.DisconnectedCircuitMaxRetained = 100;
            options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
            // Detailed errors in development
            options.DetailedErrors = builder.Environment.IsDevelopment();
        });
        builder.Services.AddMudServices();

        // Add HttpContextAccessor for accessing user context
        builder.Services.AddHttpContextAccessor();

        // Register Authentication State Provider for Blazor Server
        builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider,
            SmartPPC.Api.Services.RevalidatingIdentityAuthenticationStateProvider<User>>();

        // Register Configuration Service
        builder.Services.AddScoped<SmartPPC.Api.Services.ConfigurationService>();

        // Register Configuration State Service (for sharing state across pages)
        builder.Services.AddScoped<SmartPPC.Api.Services.ConfigurationStateService>();

        // Register ML Forecasting Services
        builder.Services.AddScoped<SmartPPC.Core.ML.Services.IForecastDataCollectionService,
            SmartPPC.Core.ML.Services.ForecastDataCollectionService>();
        builder.Services.AddScoped<SmartPPC.Core.ML.Services.IForecastingService,
            SmartPPC.Core.ML.Services.ForecastingService>();
        builder.Services.AddScoped<SmartPPC.Core.ML.Services.IForecastFeatureEngineer,
            SmartPPC.Core.ML.Services.ForecastFeatureEngineer>();
        builder.Services.AddScoped<SmartPPC.Core.ML.Models.IForecastModelTrainer,
            SmartPPC.Core.ML.Models.ForecastModelTrainer>();
        builder.Services.AddScoped<SmartPPC.Core.ML.Models.ModelEvaluator>();

        // Register ML Forecasting Repositories
        builder.Services.AddScoped<SmartPPC.Core.ML.Repositories.IForecastTrainingDataRepository,
            SmartPPC.Api.Repositories.ForecastTrainingDataRepository>();
        builder.Services.AddScoped<SmartPPC.Core.ML.Repositories.IForecastModelRepository,
            SmartPPC.Api.Repositories.ForecastModelRepository>();
        builder.Services.AddScoped<SmartPPC.Core.ML.Repositories.IForecastPredictionRepository,
            SmartPPC.Api.Repositories.ForecastPredictionRepository>();
        builder.Services.AddScoped<SmartPPC.Core.ML.Repositories.IModelMetricsRepository,
            SmartPPC.Api.Repositories.ModelMetricsRepository>();

        // Configure ML Forecasting Options
        builder.Services.Configure<SmartPPC.Api.Services.ForecastDataCollectionOptions>(
            builder.Configuration.GetSection(SmartPPC.Api.Services.ForecastDataCollectionOptions.SectionName));

        // Register Background Data Collection Service
        builder.Services.AddHostedService<SmartPPC.Api.Services.ForecastDataCollectionBackgroundService>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();


        var app = builder.Build();

        // Automatically apply database migrations on startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        }

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        // Authentication & Authorization must be between UseRouting and endpoints
        app.UseAuthentication();
        app.UseAuthorization();

        // Map authentication API endpoints
        app.MapAuthenticationEndpoints();

        app.MapControllers();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}
