using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;
using System.Linq;

namespace ScrumBoard.Tests.Util
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class EnumDataAttribute : DataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the EnumDataAttribute class.
        /// </summary>
        /// <param name="class">The enum for which members are selected.</param>
        public EnumDataAttribute(Type @class) => this.Class = @class;

        /// <summary>Gets the enum type to get values from.</summary>
        public Type Class { get; private set; }

        /// <inheritdoc />
        public override IEnumerable<object[]> GetData(MethodInfo testMethod) => Enum.GetValues(Class).Cast<object>().Select(value => new[] { value });
    }
}
