using System.Runtime.CompilerServices;

namespace LiteYaml.Internal;

internal class InsertionQueue<T>
{
    const int MINIMUM_GROW = 4;
    const int GROW_FACTOR = 200;

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    T[] _array;
    int _headIndex;
    int _tailIndex;

    public InsertionQueue(int capacity)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(capacity, nameof(capacity));
#else
        if (capacity < 0) {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }
#endif

        _array = new T[capacity];
        _headIndex = _tailIndex = Count = 0;
    }

    public void Clear()
    {
        _headIndex = _tailIndex = Count = 0;
    }

    public T Peek()
    {
        if (Count == 0)
            ThrowForEmptyQueue();
        return _array[_headIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        if (Count == _array.Length) {
            Grow();
        }

        _array[_tailIndex] = item;
        MoveNext(ref _tailIndex);
        Count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Dequeue()
    {
        if (Count == 0)
            ThrowForEmptyQueue();

        var removed = _array[_headIndex];
        MoveNext(ref _headIndex);
        Count--;
        return removed;
    }

    public void Insert(int posTo, T item)
    {
        if (Count == _array.Length) {
            Grow();
        }

        MoveNext(ref _tailIndex);
        Count++;

        for (var pos = Count - 1; pos > posTo; pos--) {
            var index = (_headIndex + pos) % _array.Length;
            var indexPrev = index == 0 ? _array.Length - 1 : index - 1;
            _array[index] = _array[indexPrev];
        }
        _array[(posTo + _headIndex) % _array.Length] = item;
    }

    private void Grow()
    {
        var newCapacity = (int)((long)_array.Length * GROW_FACTOR / 100);
        if (newCapacity < _array.Length + MINIMUM_GROW) {
            newCapacity = _array.Length + MINIMUM_GROW;
        }
        SetCapacity(newCapacity);
    }

    private void SetCapacity(int capacity)
    {
        var newArray = new T[capacity];
        if (Count > 0) {
            if (_headIndex < _tailIndex) {
                Array.Copy(_array, _headIndex, newArray, 0, Count);
            }
            else {
                Array.Copy(_array, _headIndex, newArray, 0, _array.Length - _headIndex);
                Array.Copy(_array, 0, newArray, _array.Length - _headIndex, _tailIndex);
            }
        }

        _array = newArray;
        _headIndex = 0;
        _tailIndex = Count == capacity ? 0 : Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveNext(ref int index)
    {
        index = (index + 1) % _array.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowForEmptyQueue()
    {
        throw new InvalidOperationException("EmptyQueue");
    }
}

