namespace EventBus
{
    public static class Bus<T> where T : IEvent
    {
        public delegate void Event(T arg);
        public static event Event OnEvent;

        // Raises an event of type T, notifying all subscribers.
        public static void Raise(T evt) => OnEvent?.Invoke(evt);
    }   
}