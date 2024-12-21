namespace NpgsqlRest;

public static class NpgsqlRestMiddlewareExtensions
{
    public static IApplicationBuilder UseNpgsqlRest(this IApplicationBuilder builder, NpgsqlRestOptions options)
    {
        if (options.ConnectionString is null && options.DataSource is null && options.ServiceProviderMode == ServiceProviderObject.None)
        {
            throw new ArgumentException("ConnectionString and DataSource are null and ServiceProviderMode is set to None. You must specify connection with connection string, DataSource object or with ServiceProvider");
        }

        if (options.ConnectionString is not null && options.DataSource is not null && options.ServiceProviderMode == ServiceProviderObject.None)
        {
            throw new ArgumentException("Both ConnectionString and DataSource are provided. Please specify only one.");
        }

        if (options.Logger is not null)
        {
            NpgsqlRestMiddleware.SetLogger(options.Logger);
        }
        else if (builder is WebApplication app)
        {
            var factory = app.Services.GetRequiredService<ILoggerFactory>();
            NpgsqlRestMiddleware.SetLogger(factory is not null ? factory.CreateLogger(options.LoggerName ?? typeof(NpgsqlRestMiddlewareExtensions).Namespace ?? "NpgsqlRest") : app.Logger);
        }

        NpgsqlRestMiddleware.SetMetadata(NpgsqlRestMetadataBuilder.Build(options, NpgsqlRestMiddleware.Logger, builder));
        if (NpgsqlRestMiddleware.Metadata.Entries.Count == 0)
        {
            return builder;
        }
        NpgsqlRestMiddleware.SetOptions(options);
        NpgsqlRestMiddleware.SetServiceProvider(builder.ApplicationServices);

        return builder.UseMiddleware<NpgsqlRestMiddleware>();
    }
}