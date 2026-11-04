namespace RayanPortfolio.Server.Services;

public static class ApiErrors
{
    public static IResult BadRequest(string message) => Error(message, StatusCodes.Status400BadRequest);

    public static IResult Unauthorized(string message = "يلزم تسجيل الدخول للوصول إلى الإدارة.") =>
        Error(message, StatusCodes.Status401Unauthorized);

    public static IResult Forbidden(string message = "هذا الحساب غير مصرح له بالدخول.") =>
        Error(message, StatusCodes.Status403Forbidden);

    public static IResult NotFound(string message = "العنصر المطلوب غير موجود.") =>
        Error(message, StatusCodes.Status404NotFound);

    public static IResult NotConfigured(string message = "الخدمة غير مهيأة بعد.") =>
        Error(message, StatusCodes.Status503ServiceUnavailable);

    public static IResult ServerError() =>
        Error("تعذّر إكمال الطلب حاليًا. حاول مرة أخرى لاحقًا.", StatusCodes.Status500InternalServerError);

    private static IResult Error(string message, int statusCode) =>
        Results.Json(new { error = message }, statusCode: statusCode);
}
