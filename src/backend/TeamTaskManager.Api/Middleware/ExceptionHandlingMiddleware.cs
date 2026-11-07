using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Api.Contracts;

namespace TeamTaskManager.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "İşlenmeyen hata. TraceId: {TraceId}", context.TraceIdentifier);
            var (statusCode, message) = exception switch
            {
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Kayıt başka bir işlem tarafından güncellendi. Lütfen veriyi yenileyip tekrar deneyin."),
                DbUpdateException => (StatusCodes.Status409Conflict, "Veri kaydedilirken bir bütünlük çakışması oluştu."),
                ArgumentException => (StatusCodes.Status400BadRequest, "Geçersiz istek."),
                _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu.")
            };

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var errors = environment.IsDevelopment() && statusCode == StatusCodes.Status500InternalServerError
                ? new[] { exception.Message, $"TraceId: {context.TraceIdentifier}" }
                : new[] { $"TraceId: {context.TraceIdentifier}" };
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message, errors));
        }
    }
}
