#nullable enable
using CommunityToolkit.Maui.Exceptions;
using System.Reflection;
using System.Reflection.Emit;

// Inspired by AsyncAwaitBestPractices.WeakEventManagerService: https://github.com/brminnick/AsyncAwaitBestPractices
namespace CommunityToolkit.Maui.Helpers
{
    static class EventManagerService
    {
        internal static void AddEventHandler(in string eventName, in object? handlerTarget, in MethodInfo methodInfo, in Dictionary<string, List<Subscription>> eventHandlers)
        {
            var doesContainSubscriptions = eventHandlers.TryGetValue(eventName, out var targets);
            if (!doesContainSubscriptions || targets == null)
            {
                targets = new List<Subscription>();
                eventHandlers.Add(eventName, targets);
            }

            if (handlerTarget == null)
                targets.Add(new Subscription(null, methodInfo));
            else
                targets.Add(new Subscription(new WeakReference(handlerTarget), methodInfo));
        }

        internal static void RemoveEventHandler(in string eventName, in object? handlerTarget, in MemberInfo methodInfo, in Dictionary<string, List<Subscription>> eventHandlers)
        {
            var doesContainSubscriptions = eventHandlers.TryGetValue(eventName, out var subscriptions);
            if (!doesContainSubscriptions || subscriptions == null)
                return;

            for (var n = subscriptions.Count; n > 0; n--)
            {
                var current = subscriptions[n - 1];

                if (current.Subscriber?.Target != handlerTarget
                    || current.Handler.Name != methodInfo?.Name)
                {
                    continue;
                }

                subscriptions.Remove(current);
                break;
            }
        }

        internal static void HandleEvent(in string eventName, in object? sender, in object? eventArgs, in Dictionary<string, List<Subscription>> eventHandlers)
        {
            AddRemoveEvents(eventName, eventHandlers, out var toRaise);

            for (var i = 0; i < toRaise.Count; i++)
            {
                try
                {
                    var (instance, eventHandler) = toRaise[i];
                    if (eventHandler.IsLightweightMethod())
                    {
                        var method = TryGetDynamicMethod(eventHandler);
                        method?.Invoke(instance, new[] { sender, eventArgs });
                    }
                    else
                    {
                        eventHandler.Invoke(instance, new[] { sender, eventArgs });
                    }
                }
                catch (TargetParameterCountException e)
                {
                    throw new InvalidHandleEventException("Parameter count mismatch. If invoking an `event Action` use `HandleEvent(string eventName)` or if invoking an `event Action<T>` use `HandleEvent(object eventArgs, string eventName)`instead.", e);
                }
            }
        }

        internal static void HandleEvent(in string eventName, in object? actionEventArgs, in Dictionary<string, List<Subscription>> eventHandlers)
        {
            AddRemoveEvents(eventName, eventHandlers, out var toRaise);

            for (var i = 0; i < toRaise.Count; i++)
            {
                try
                {
                    var (instance, eventHandler) = toRaise[i];
                    if (eventHandler.IsLightweightMethod())
                    {
                        var method = TryGetDynamicMethod(eventHandler);
                        method?.Invoke(instance, new[] { actionEventArgs });
                    }
                    else
                    {
                        eventHandler.Invoke(instance, new[] { actionEventArgs });
                    }
                }
                catch (TargetParameterCountException e)
                {
                    throw new InvalidHandleEventException("Parameter count mismatch. If invoking an `event EventHandler` use `HandleEvent(object? sender, TEventArgs eventArgs, string eventName)` or if invoking an `event Action` use `HandleEvent(string eventName)`instead.", e);
                }
            }
        }

        internal static void HandleEvent(in string eventName, in Dictionary<string, List<Subscription>> eventHandlers)
        {
            AddRemoveEvents(eventName, eventHandlers, out var toRaise);

            for (var i = 0; i < toRaise.Count; i++)
            {
                try
                {
                    var (instance, eventHandler) = toRaise[i];
                    if (eventHandler.IsLightweightMethod())
                    {
                        var method = TryGetDynamicMethod(eventHandler);
                        method?.Invoke(instance, null);
                    }
                    else
                    {
                        eventHandler.Invoke(instance, null);
                    }
                }
                catch (TargetParameterCountException e)
                {
                    throw new InvalidHandleEventException("Parameter count mismatch. If invoking an `event EventHandler` use `HandleEvent(object? sender, TEventArgs eventArgs, string eventName)` or if invoking an `event Action<T>` use `HandleEvent(object eventArgs, string eventName)`instead.", e);
                }
            }
        }

        static void AddRemoveEvents(in string eventName, in Dictionary<string, List<Subscription>> eventHandlers, out List<(object? Instance, MethodInfo EventHandler)> toRaise)
        {
            var toRemove = new List<Subscription>();
            toRaise = new List<(object?, MethodInfo)>();

            var doesContainEventName = eventHandlers.TryGetValue(eventName, out var target);
            if (doesContainEventName && target != null)
            {
                for (var i = 0; i < target.Count; i++)
                {
                    var subscription = target[i];
                    var isStatic = subscription.Subscriber == null;

                    if (isStatic)
                    {
                        toRaise.Add((null, subscription.Handler));
                        continue;
                    }

                    var subscriber = subscription.Subscriber?.Target;

                    if (subscriber == null)
                        toRemove.Add(subscription);
                    else
                        toRaise.Add((subscriber, subscription.Handler));
                }

                for (var i = 0; i < toRemove.Count; i++)
                {
                    var subscription = toRemove[i];
                    target.Remove(subscription);
                }
            }
        }

        static DynamicMethod? TryGetDynamicMethod(in MethodInfo rtDynamicMethod)
        {
            var typeInfoRTDynamicMethod = typeof(DynamicMethod).GetTypeInfo().GetDeclaredNestedType("RTDynamicMethod");
            var typeRTDynamicMethod = typeInfoRTDynamicMethod?.AsType();

            if (typeInfoRTDynamicMethod != null && typeInfoRTDynamicMethod.IsAssignableFrom(rtDynamicMethod.GetType().GetTypeInfo()))
                return (DynamicMethod?)typeRTDynamicMethod?.GetRuntimeFields()?.FirstOrDefault(f => f?.Name is "m_owner")?.GetValue(rtDynamicMethod);
            else
                return null;
        }

        static bool IsLightweightMethod(this MethodBase method)
        {
            var typeInfoRTDynamicMethod = typeof(DynamicMethod).GetTypeInfo().GetDeclaredNestedType("RTDynamicMethod");
            return method is DynamicMethod || (typeInfoRTDynamicMethod?.IsAssignableFrom(method.GetType().GetTypeInfo()) ?? false);
        }
    }
}