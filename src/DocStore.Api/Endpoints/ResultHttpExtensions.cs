using DocStore.Core.Common;

namespace DocStore.Api.Endpoints;

public static class ResultHttpExtensions
{
    public static IResult ToHttp(this Result result) =>
        result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);

    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : ToProblem(result.Error);

    private static IResult ToProblem(Error error) => Results.Problem(
        detail: error.Message,
        statusCode: error.Kind switch
        {
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        });
}
