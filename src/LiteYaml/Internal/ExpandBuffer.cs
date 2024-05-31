using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LiteYaml.Internal;

internal class ExpandBuffer<T>(int capacity)
{
    private const int MINIMUM_GROW = 4;
    private const int GROW_FACTOR = 200;

    T[] _buffer = new T[capacity];

    public int Length { get; private set; } = 0;

    public ref T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _buffer[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        return _buffer.AsSpan(0, Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int length)
    {
        if (length > _buffer.Length) {
            SetCapacity(_buffer.Length * 2);
        }
        return _buffer.AsSpan(0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Peek()
    {
        return ref _buffer[Length - 1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Pop()
    {
        if (Length == 0) {
            throw new InvalidOperationException("Cannot pop the empty buffer");
        }

        return ref _buffer[--Length];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPop([MaybeNullWhen(false)] out T value)
    {
        if (Length == 0) {
            value = default;
            return false;
        }

        value = Pop();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (Length >= _buffer.Length) {
            Grow();
        }

        _buffer[Length++] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetCapacity(int newCapacity)
    {
        if (_buffer.Length >= newCapacity) {
            return;
        }

        // var newBuffer = ArrayPool<T>.Shared.Rent(newCapacity);
        T[] newBuffer = new T[newCapacity];
        _buffer.AsSpan(0, Length).CopyTo(newBuffer);
        // ArrayPool<T>.Shared.Return(buffer);
        _buffer = newBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Grow()
    {
        int newCapacity = _buffer.Length * GROW_FACTOR / 100;
        if (newCapacity < _buffer.Length + MINIMUM_GROW) {
            newCapacity = _buffer.Length + MINIMUM_GROW;
        }
        SetCapacity(newCapacity);
    }
}

