namespace BullOak.Repositories.EventStore.Streams;

using System;

public readonly record struct StoredEventPosition
{
    private readonly ulong value;

    public static readonly StoredEventPosition Start = new(0);

    public static readonly StoredEventPosition End = new(ulong.MaxValue);

    private StoredEventPosition(ulong value) =>
        this.value =
            value is <= long.MaxValue or ulong.MaxValue
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value));

    public static StoredEventPosition FromInt64(long value) =>
        value != -1L
            ? new StoredEventPosition(Convert.ToUInt64(value))
            : End;

    public long ToInt64() => Equals(End) ? -1 : Convert.ToInt64(value);
};
