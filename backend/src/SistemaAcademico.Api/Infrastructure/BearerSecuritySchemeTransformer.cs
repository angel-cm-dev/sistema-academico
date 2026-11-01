using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SistemaAcademico.Api.Infrastructure;

/// <summary>
/// Declara el esquema Bearer en el documento OpenAPI para que Scalar muestre
/// el campo "Authorization" y permita probar los endpoints protegidos.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "Sistema Académico API";
        document.Info.Description = "Matrícula, notas por competencia, asistencia, pensiones y libretas. " +
                                    "Obtén un token en POST /api/auth/login (admin@demo.pe / Demo123!).";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Pega aquí el token JWT devuelto por /api/auth/login."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
        return Task.CompletedTask;
    }
}
