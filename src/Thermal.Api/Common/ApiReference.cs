using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Thermal.Api.Common;

/// <summary>
/// Referencia interactiva de la API con Scalar (en lugar de Swagger UI), solo en desarrollo. Muestra dos documentos:
/// el contrato <c>docs/api/thermal-v1.yaml</c> (fuente de verdad, incrustado en el ensamblado) y el OpenAPI que
/// ASP.NET Core genera desde el código, para compararlos.
/// </summary>
internal static class ApiReference
{
    public const string ScalarRoute = "/scalar";

    // Todos los documentos siguen el mismo patrón, así /scalar/{documentName} funciona con cualquiera de ellos.
    private const string DocumentRoutePattern = "/openapi/{documentName}.yaml";
    private const string ContractDocument = "thermal-v1";
    private const string GeneratedDocument = "v1";
    private const string ContractResource = "thermal-v1.yaml";
    private const string BearerScheme = "bearerAuth";

    /// <summary>OpenAPI generado desde el código, con el esquema Bearer para poder probar con un JWT.</summary>
    public static IServiceCollection AddApiReference(this IServiceCollection services) =>
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

    public static WebApplication MapApiReference(this WebApplication app)
    {
        app.MapOpenApi();                          // /openapi/v1.json
        app.MapOpenApi(DocumentRoutePattern);      // /openapi/v1.yaml
        app.MapGet(DocumentRoutePattern.Replace("{documentName}", ContractDocument, StringComparison.Ordinal), ServeContract)
            .AllowAnonymous()
            .ExcludeFromDescription();
        app.MapScalarApiReference(ScalarRoute, (options, context) =>
        {
            options
                .WithTitle("ThermalCalibration API")
                .WithOpenApiRoutePattern(DocumentRoutePattern)
                .AddDocument(ContractDocument, "Contrato (thermal-v1.yaml)", isDefault: true)
                .AddDocument(GeneratedDocument, "Generado desde el código")
                // Las pruebas desde Scalar van siempre a la instancia que sirve la página (compose, dotnet run o PC del laboratorio).
                .AddServer($"{context.Request.Scheme}://{context.Request.Host}", "Esta instancia")
                .AddPreferredSecuritySchemes(BearerScheme)
                .EnablePersistentAuthentication();
        }).AllowAnonymous();
        return app;
    }

    private static IResult ServeContract()
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ContractResource)
            ?? throw new InvalidOperationException($"Falta el recurso incrustado {ContractResource}.");
        return Results.Stream(stream, "application/yaml; charset=utf-8");
    }

    private sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT con el rol del usuario (Admin, Technician o Supervisor). En desarrollo: dotnet user-jwts.",
            };
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [] });
            return Task.CompletedTask;
        }
    }
}
