namespace Charmander.Api.Repositories;

public static class RepositoryExtensions
{
  public static IServiceCollection AddDeviceTokenRepository(
      this IServiceCollection services,
      string connectionString = "Data Source=devices.db")
  {
    services.AddSingleton<IDeviceTokenRepository>(_ => new DeviceTokenRepository(connectionString));
    return services;
  }
}
