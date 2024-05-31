using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace LiteYaml.Internal;

static class ReusableByteSequenceBuilderPool
{
    static readonly ConcurrentQueue<ReusableByteSequenceBuilder> _queue = new();

    public static ReusableByteSequenceBuilder Rent()
    {
        if (_queue.TryDequeue(out var builder)) {
            return builder;
        }

        return new ReusableByteSequenceBuilder();
    }

    public static void Return(ReusableByteSequenceBuilder builder)
    {
        builder.Reset();
        _queue.Enqueue(builder);
    }
}

class ReusableByteSequenceSegment : ReadOnlySequenceSegment<byte>
{
    bool _returnToPool;

    public ReusableByteSequenceSegment()
    {
        _returnToPool = false;
    }

    public void SetBuffer(ReadOnlyMemory<byte> buffer, bool returnToPool)
    {
        Memory = buffer;
        _returnToPool = returnToPool;
    }

    public void Reset()
    {
        if (_returnToPool) {
            if (MemoryMarshal.TryGetArray(Memory, out var segment) && segment.Array != null) {
                ArrayPool<byte>.Shared.Return(segment.Array);
            }
        }
        Memory = default;
        RunningIndex = 0;
        Next = null;
    }

    public void SetRunningIndexAndNext(long runningIndex, ReusableByteSequenceSegment? nextSegment)
    {
        RunningIndex = runningIndex;
        Next = nextSegment;
    }
}

class ReusableByteSequenceBuilder
{
    readonly Stack<ReusableByteSequenceSegment> _segmentPool = new();
    readonly List<ReusableByteSequenceSegment> _segments = [];

    public void Add(ReadOnlyMemory<byte> buffer, bool returnToPool)
    {
        if (!_segmentPool.TryPop(out var segment)) {
            segment = new ReusableByteSequenceSegment();
        }

        segment.SetBuffer(buffer, returnToPool);
        _segments.Add(segment);
    }

    public bool TryGetSingleMemory(out ReadOnlyMemory<byte> memory)
    {
        if (_segments.Count == 1) {
            memory = _segments[0].Memory;
            return true;
        }
        memory = default;
        return false;
    }

    public ReadOnlySequence<byte> Build()
    {
        if (_segments.Count == 0) {
            return ReadOnlySequence<byte>.Empty;
        }

        if (_segments.Count == 1) {
            return new ReadOnlySequence<byte>(_segments[0].Memory);
        }

        long running = 0;

        for (var i = 0; i < _segments.Count; i++) {
            var next = i < _segments.Count - 1 ? _segments[i + 1] : null;
            _segments[i].SetRunningIndexAndNext(running, next);
            running += _segments[i].Memory.Length;
        }
        var firstSegment = _segments[0];
        var lastSegment = _segments[^1];
        return new ReadOnlySequence<byte>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);
    }

    public void Reset()
    {
        foreach (var item in _segments) {
            item.Reset();
            _segmentPool.Push(item);
        }
        _segments.Clear();
    }
}

