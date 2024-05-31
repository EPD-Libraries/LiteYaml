using LiteYaml.Internal;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace LiteYaml.Tests
{
    [TestFixture]
    public class StreamHelperTest
    {
        [Test]
        public async Task ReadAsSequenceAsync_MemoryStream()
        {
            MemoryStream memoryStream = new([(byte)'a', (byte)'b', (byte)'c']);
            ReusableByteSequenceBuilder builder = await StreamHelper.ReadAsSequenceAsync(memoryStream);
            try {
                System.Buffers.ReadOnlySequence<byte> sequence = builder.Build();
                Assert.That(sequence.IsSingleSegment, Is.True);
                Assert.That(sequence.Length, Is.EqualTo(3));
                Assert.That(sequence.FirstSpan[0], Is.EqualTo((byte)'a'));
                Assert.That(sequence.FirstSpan[1], Is.EqualTo((byte)'b'));
                Assert.That(sequence.FirstSpan[2], Is.EqualTo((byte)'c'));
            }
            finally {
                ReusableByteSequenceBuilderPool.Return(builder);
            }
        }

        [Test]
        public async Task ReadAsSequenceAsync_FileStream()
        {
            string tempFilePath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFilePath, new string('a', 1000));

            await using FileStream fileStream = File.OpenRead(tempFilePath);
            ReusableByteSequenceBuilder builder = await StreamHelper.ReadAsSequenceAsync(fileStream);
            try {
                System.Buffers.ReadOnlySequence<byte> sequence = builder.Build();
                Assert.That(sequence.Length, Is.EqualTo(1000));
                foreach (System.ReadOnlyMemory<byte> readOnlyMemory in sequence) {
                    foreach (byte b in readOnlyMemory.Span.ToArray()) {
                        Assert.That(b, Is.EqualTo('a'));
                    }
                }
            }
            finally {
                ReusableByteSequenceBuilderPool.Return(builder);
            }
        }
    }
}