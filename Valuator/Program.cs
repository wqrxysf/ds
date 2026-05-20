using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Valuator.Hubs;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
        
        builder.Services.AddSingleton<ConnectionMultiplexerFactory>();

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var factory = sp.GetRequiredService<ConnectionMultiplexerFactory>();
            return factory.GetConnection("MAIN");
        });

        builder.Services.AddSingleton<IUserService, UserService>();

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

        builder.Services.AddAuthorization();

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

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapRazorPages();

        app.MapHub<RankHub>("/rankHub");

        app.Run();
    }
}
