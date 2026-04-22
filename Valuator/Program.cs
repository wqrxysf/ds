using StackExchange.Redis;
using Microsoft.AspNetCore.SignalR;
using Valuator.Hubs;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse("localhost:6379");
            return ConnectionMultiplexer.Connect(configuration);
        });

        builder.Services.AddRazorPages();

        builder.Services.AddSignalR();

        builder.Services.AddHostedService<RankUpdateService>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.MapHub<RankHub>("/rankHub");

        app.Run();
    }
}
