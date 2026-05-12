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
            var factory = sp.GetRequiredService<ConnectionMultiplexerFactory>();
            return factory.GetConnection("MAIN");
        });

        builder.Services.AddSingleton<ConnectionMultiplexerFactory>();

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
