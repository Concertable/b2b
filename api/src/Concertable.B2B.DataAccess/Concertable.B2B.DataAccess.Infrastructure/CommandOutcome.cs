using System.Linq.Expressions;
using Reunion;

namespace Concertable.B2B.DataAccess.Infrastructure;

internal static class CommandOutcome
{
    public static bool IsFailure<TResult>(TResult result) =>
        FailureDetector<TResult>.IsFailure(result);

    private static class FailureDetector<TResult>
    {
        public static readonly Func<TResult, bool> IsFailure = Create();

        private static Func<TResult, bool> Create()
        {
            var type = typeof(TResult);
            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (definition != typeof(Result)
                && definition != typeof(Result<>)
                && definition != typeof(Result<,>)
                && definition != typeof(UnitResult<>))
                return _ => false;

            var result = Expression.Parameter(type, "result");
            return Expression.Lambda<Func<TResult, bool>>(
                    Expression.Property(result, nameof(Result.IsFailure)),
                    result)
                .Compile();
        }
    }
}
