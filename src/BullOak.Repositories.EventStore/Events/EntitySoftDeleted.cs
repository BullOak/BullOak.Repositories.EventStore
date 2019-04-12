namespace BullOak.Repositories.EventStore.Events
{
    using System;

    public class EntitySoftDeleted
    {
    }

    internal static class DefaultSoftDeleteEvent
    {
        public static readonly Type Type = typeof(EntitySoftDeleted);
        public static readonly ItemWithType ItemWithType = new ItemWithType(new EntitySoftDeleted());
    }
}
