using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Thermal.Api.Common;
using Thermal.Application;
using Thermal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers(options =>
    {
        // Los errores de validación usan los nombres JSON (camelCase), como el contrato.
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
        // Un JSON ilegible ya se informa en su propiedad; evita el error extra "The request field is required".
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        // additionalProperties: false en los schemas de request.
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        // Enumeraciones como texto (limitMode: "Range" | "Band"), igual que el contrato.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    });

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context => ProblemTitles.Translate(context.ProblemDetails));
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// JWT provisional hasta decidir el mecanismo de autenticación (c4-containers §3.1).
// La configuración se lee de "Authentication:Schemes:Bearer"; en desarrollo la crea `dotnet user-jwts`.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
// 401, 403, 404 y 415 sin cuerpo también responden con Problem Details.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
