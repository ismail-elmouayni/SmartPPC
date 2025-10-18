using MudBlazor.Services;

namespace Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddMudServices();

        // Register Configuration Service
        builder.Services.AddScoped<SmartPPC.Api.Services.ConfigurationService>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        

        var app = builder.Build();

        app.MapDefaultEndpoints();
        //app.MapRazorPages();
        //app.MapControllers();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseAuthorization();
        app.UseRouting();

        app.MapControllers();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        

        app.Run();
    }
}
