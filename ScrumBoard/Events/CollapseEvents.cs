using System;
using Microsoft.AspNetCore.Components;

namespace ScrumBoard.Events
{
    [EventHandler("oncollapseshow", typeof(CollapseEventArgs))]
    [EventHandler("oncollapsehide", typeof(CollapseEventArgs))]
    [EventHandler("oncollapseshown", typeof(CollapseEventArgs))]
    [EventHandler("oncollapsehidden", typeof(CollapseEventArgs))]
    public static class EventHandlers
    {
        // Empty class that when loaded will register the event handlers for bootstrap collapse
    }

    public class CollapseEventArgs : EventArgs
    {
        // No attributes included
    }
}
