mergeInto(LibraryManager.library, {
  BombSwapHarnessReport: function (eventNamePointer) {
    var eventName = UTF8ToString(eventNamePointer);
    var events = globalThis.__BOMBSWAP_HARNESS_EVENTS__;
    if (!Array.isArray(events)) {
      events = [];
      globalThis.__BOMBSWAP_HARNESS_EVENTS__ = events;
    }

    events.push({ name: eventName, timestamp: Date.now() });
    if (!globalThis.__BOMBSWAP_HARNESS_EXPORT_READY__) {
      globalThis.__BOMBSWAP_HARNESS_EXPORT_READY__ = true;
      var notifyEventsAvailable = globalThis.BombSwapHarnessNotifyEventsAvailable;
      if (typeof notifyEventsAvailable === "function") {
        notifyEventsAvailable();
      }
    }
  },
});
