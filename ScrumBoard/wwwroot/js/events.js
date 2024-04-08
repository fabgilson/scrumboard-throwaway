for (let eventName of ['show', 'hide', 'shown', 'hidden']) {
    Blazor.registerCustomEventType('collapse' + eventName, {
        browserEventName: eventName + '.bs.collapse',
        createEventArgs() {
            return {};
        }
    });
}