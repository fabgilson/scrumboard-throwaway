using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.AspNetCore.Components;

namespace ScrumBoard.Tests.Util
{
    /// <summary>
    /// Empty component that can be used to replace any component
    /// </summary>
    /// <typeparam name="TComponent">Component type that is being mocked</typeparam>
    public class Dummy<TComponent> : ComponentBase
    {
        /// <summary>
        /// Parameters passed into the component
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
        
        /// <summary>
        /// Fetches the given parameter using a member access expression
        /// </summary>
        /// <param name="getter">Simple member access expression (e.g. x => x.Foo)</param>
        /// <typeparam name="T">Type of result</typeparam>
        /// <returns>Result of fetching param</returns>
        public T GetParam<T>(Expression<Func<TComponent, T>> getter)
        {
            var param = getter.Parameters.Single();
            var body = getter.Body;
            if (body is MemberExpression m)
            {
                if (m.Expression != param)
                {
                    throw new InvalidOperationException("Member access is not on the parameter");
                }
                return (T)Parameters[m.Member.Name];
            }
            else
            {
                throw new InvalidOperationException("Expression is not a member access");
            }
        }
    }
}