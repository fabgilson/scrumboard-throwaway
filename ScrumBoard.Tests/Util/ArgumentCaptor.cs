using System.Collections.Generic;
using FluentAssertions;
using Moq;

namespace ScrumBoard.Tests.Util 
{
    /// <summary>
    /// Equivalent of Mockito ArgumentCaptor in C#
    /// </summary>
    public class ArgumentCaptor<T>
    {
        public List<T> Values { get; private set; } = new();
        public T Value {
            get {
                Values.Should().HaveCount(1);
                return Values[0];
            }
        }
        
        /// <summary>
        /// Create a matching condition that always succeeds 
        /// the value passed into the matcher will be accessible through Values or Value
        /// </summary>
        public T Capture()
        {
            return It.Is<T>(t => SaveValue(t));
        }

        private bool SaveValue(T t)
        {
            Values.Add(t);
            return true;
        }
    }
}