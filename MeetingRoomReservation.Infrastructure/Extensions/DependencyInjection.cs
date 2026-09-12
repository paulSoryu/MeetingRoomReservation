using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Infrastructure.Data;
using MeetingRoomReservation.Infrastructure.Data.Repositories;
using MeetingRoomReservation.Infrastructure.Data.Seeders;
using MeetingRoomReservation.Infrastructure.Realtime;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.SignalR.Management;

namespace MeetingRoomReservation.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IServiceManager>(_ => (IServiceManager)new ServiceManagerBuilder()
            .WithOptions(option => option.ConnectionString = configuration["Azure:SignalR:ConnectionString"])
            .BuildServiceManager());

        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton<IBookingNotifier, SignalRBookingNotifier>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
