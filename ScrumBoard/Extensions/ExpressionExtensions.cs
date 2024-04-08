using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ScrumBoard.Extensions
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Joins two expressions using a logical and operator
        /// </summary>
        /// <param name="left">Left expression to join</param>
        /// <param name="right">Right expression to join</param>
        /// <typeparam name="T">Parameter type of both expression</typeparam>
        /// <returns>Merged expression</returns>
        public static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
            => Join<T, bool, bool>(left, right, Expression.AndAlso);
        
        /// <summary>
        /// Joins two expressions using a logical or operator
        /// </summary>
        /// <param name="left">Left expression to join</param>
        /// <param name="right">Right expression to join</param>
        /// <typeparam name="T">Parameter type of both expression</typeparam>
        /// <returns>Merged expression</returns>
        public static Expression<Func<T, bool>> OrElse<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
            => Join<T, bool, bool>(left, right, Expression.OrElse);
        
        /// <summary>
        /// Joins two expressions using an equality operator
        /// </summary>
        /// <param name="left">Left expression to join</param>
        /// <param name="right">Right expression to join</param>
        /// <typeparam name="TInput">Input parameter type of both expression</typeparam>
        /// <typeparam name="TOutput">Output type of both expression</typeparam>
        /// <returns>Merged expression</returns>
        public static Expression<Func<TInput, bool>> JoinEquals<TInput, TOutput>(
            this Expression<Func<TInput, TOutput>> left,
            Expression<Func<TInput, TOutput>> right)
            => Join<TInput, TOutput, bool>(left, right, Expression.Equal);

        
        private static Expression<Func<TInput, TCombinedOutput>> Join<TInput, TOutput, TCombinedOutput>(Expression<Func<TInput, TOutput>> left,
            Expression<Func<TInput, TOutput>> right, Func<Expression, Expression, BinaryExpression> joiner)
        {
            var parameter = Expression.Parameter(typeof (TInput));

            var leftVisitor = new ReplaceExpressionVisitor(left.Parameters.Single(), parameter);
            var leftBody = leftVisitor.Visit(left.Body);

            var rightVisitor = new ReplaceExpressionVisitor(right.Parameters.Single(), parameter);
            var rightBody = rightVisitor.Visit(right.Body);

            return Expression.Lambda<Func<TInput, TCombinedOutput>>(joiner(leftBody, rightBody), parameter);
        }

        
        private class ReplaceExpressionVisitor
            : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            /// <summary>
            /// Visitor that recursively substitutes a term in an expression with another term
            /// </summary>
            /// <param name="oldValue">Expression value to replace</param>
            /// <param name="newValue">Updated expression value</param>
            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _oldValue)
                    return _newValue;
                return base.Visit(node);
            }
        }
    }
}
