using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Moq;

namespace ScrumBoard.Tests.Util
{
    public class SingletonComponentFactory<T> : IComponentFactory where T : class, IComponent
    {
        private T _component;
        private bool _isUsed = false;

        public SingletonComponentFactory(T component)
        {
            _component = component;
        }

        public bool CanCreate(Type componentType)
            => componentType == typeof(T);
  
        public IComponent Create(Type componentType)
        {
            _isUsed.Should().BeFalse();
            _isUsed = true;
            return _component;
        }
    }

    public static class ComponentMockFactoryExtensions
    {
        /// <summary>
        /// Adds a new dummy factory that will replace every usage of the provided component type 
        /// </summary>
        /// <typeparam name="TComponent">Component type to replace</typeparam>
        public static ICollection<IComponentFactory> AddMockComponent<TComponent>(this ICollection<IComponentFactory> factories, Mock<TComponent> component) where TComponent : class, IComponent
        {
            factories.Add(new SingletonComponentFactory<TComponent>(component.Object));
            return factories;
        }
    }
}