using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace ScrumBoard.Tests.Util
{
    public class ComponentDummyFactory : IComponentFactory
    {
        private readonly Type _componentToDouble;
  
        public ComponentDummyFactory(Type componentToDouble)
        {
            _componentToDouble = componentToDouble;
        }
  
        public bool CanCreate(Type componentType)
            => componentType == _componentToDouble;
  
        public IComponent Create(Type componentType)
            => (IComponent)Activator.CreateInstance(typeof(Dummy<>).MakeGenericType(componentType))!;
    }

    public static class ComponentFactoriesExtensions
    {
        /// <summary>
        /// Adds a new dummy factory that will replace every usage of the provided component type 
        /// </summary>
        /// <typeparam name="TComponent">Component type to replace</typeparam>
        public static ICollection<IComponentFactory> AddDummyFactoryFor<TComponent>(this ICollection<IComponentFactory> factories)
        {
            factories.Add(new ComponentDummyFactory(typeof(TComponent)));
            return factories;
        }
    }
}