using System.Buffers;

namespace LiteYaml.Internal;

static class StreamHelper
{
    public static async ValueTask<ReusableByteSequenceBuilder> ReadAsSequenceAsync(Stream stream, CancellationToken cancellation = default)
    {
        ReusableByteSequenceBuilder builder = ReusableByteSequenceBuilderPool.Rent();
        try {
            if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> arraySegment)) {
                cancellation.ThrowIfCancellationRequested();

                // Emulate that we had actually "read" from the stream.
                ms.Seek(arraySegment.Count, SeekOrigin.Current);

                builder.Add(arraySegment.AsMemory(), false);
                return builder;
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(65536); // initial 64K
            int offset = 0;
            do {
                if (offset == buffer.Length) {
                    builder.Add(buffer, returnToPool: true);
                    buffer = ArrayPool<byte>.Shared.Rent(NewArrayCapacity(buffer.Length));
                    offset = 0;
                }

                int bytesRead;
                try {
                    bytesRead = await stream
                        .ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellation)
                        .ConfigureAwait(false);
                }
                catch {
                    // buffer is not added in builder, so return here.
                    ArrayPool<byte>.Shared.Return(buffer);
                    throw;
                }

                offset += bytesRead;

                if (bytesRead == 0) {
                    builder.Add(buffer.AsMemory(0, offset), returnToPool: true);
                    break;
                }
            } while (true);
        }
        catch (Exception) {
            ReusableByteSequenceBuilderPool.Return(builder);
            throw;
        }

        return builder;
    }

    private const int ARRAY_MEX_LENGTH = 0x7FFFFFC7;

    private static int NewArrayCapacity(int size)
    {
        int newSize = unchecked(size * 2);
        if ((uint)newSize > ARRAY_MEX_LENGTH) {
            newSize = ARRAY_MEX_LENGTH;
        }

        return newSize;
    }
}

