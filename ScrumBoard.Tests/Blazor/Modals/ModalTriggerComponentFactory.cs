using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Shared.Modals;
using ScrumBoard.TestAssets;

namespace ScrumBoard.Tests.Blazor.Modals
{
    public class ModalTriggerComponentFactory : IComponentFactory
    {
        public bool CanCreate(Type componentType) => typeof(ModalTrigger) == componentType;

        public IComponent Create(Type componentType)
        {
            return new DummyModalTrigger();
        }
    }
}