using System.Reflection;
using Microsoft.OpenApi.Models;

namespace DryCleaning.Api.Infrastructure.Swagger;

public static class SwaggerSetup
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "E Dry Cleaners API",
                Version = "v1",
                Description =
                    "Marketplace API for E Dry Cleaners – Customer, Service Provider, Driver and Admin.\n\n" +
                    "**Sign in:** call `POST /api/auth/login`, copy `accessToken`, click **Authorize** and paste it.\n\n" +
                    "Demo users (Development seed, password `Demo@12345`): admin@edrycleaners.co.za, " +
                    "thandi@sparkledrycleaners.co.za (provider), bongani@drivers.edrycleaners.co.za (driver), sipho@example.co.za (customer)."
            });

            var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xmlFile))
                options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the accessToken returned by /api/auth/login (without the 'Bearer ' prefix)."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            options.SupportNonNullableReferenceTypes();
        });

        return services;
    }
}
