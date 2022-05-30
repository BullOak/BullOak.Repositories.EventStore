namespace BullOak.Repositories.EventStore.Events
{
    using System;

    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string nameOfType) : base($"Type {nameOfType} not found in loaded assemblies.")
        { }
    }
}
