using FluentValidation;

namespace SistemaAcademico.Api.Infrastructure;

/// <summary>
/// Filtro de endpoint que valida el cuerpo con FluentValidation antes de llegar al servicio.
/// Si falla, lanza ValidationException y el GlobalExceptionHandler responde 400 con los errores por campo.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        var argumento = context.Arguments.OfType<T>().FirstOrDefault();

        if (validator is not null && argumento is not null)
        {
            var resultado = await validator.ValidateAsync(argumento, context.HttpContext.RequestAborted);
            if (!resultado.IsValid) throw new ValidationException(resultado.Errors);
        }
        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder Validar<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>();
}
