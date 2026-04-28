using System;
using System.Collections.Generic;

namespace ChronoDrop.Core
{
    /// <summary>
    /// Static, allocation-free event bus. Replaces singleton managers.
    /// Usage: EventBus.Raise(new PlayerDiedEvent(depth));
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            if (_handlers.TryGetValue(type, out Delegate existing))
                _handlers[type] = Delegate.Combine(existing, handler);
            else
                _handlers[type] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            if (!_handlers.TryGetValue(type, out Delegate existing))
                return;

            Delegate updated = Delegate.Remove(existing, handler);
            if (updated == null)
                _handlers.Remove(type);
            else
                _handlers[type] = updated;
        }

        public static void Raise<T>(T evt)
        {
            if (_handlers.TryGetValue(typeof(T), out Delegate handler))
                ((Action<T>)handler).Invoke(evt);
        }

        // Call on scene unload to prevent stale subscriptions
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
