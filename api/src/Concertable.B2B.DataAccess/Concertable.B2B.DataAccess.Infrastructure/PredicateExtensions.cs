using System.Linq.Expressions;

namespace Concertable.B2B.DataAccess.Infrastructure;

// Splices bodies rather than invoking: an Expression.Invoke inside a query filter does not translate.
public static class PredicateExtensions
{
    extension<T>(Expression<Func<T, bool>> left)
    {
        public Expression<Func<T, bool>> Or(Expression<Func<T, bool>> right) =>
            Combine(left, right, Expression.OrElse);

        public Expression<Func<T, bool>> And(Expression<Func<T, bool>> right) =>
            Combine(left, right, Expression.AndAlso);
    }

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> combine)
    {
        var body = new ParameterReplacer(right.Parameters[0], left.Parameters[0]).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(combine(left.Body, body), left.Parameters);
    }

    private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
