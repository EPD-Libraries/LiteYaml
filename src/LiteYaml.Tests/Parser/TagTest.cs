using NUnit.Framework;
using LiteYaml.Parser;

namespace LiteYaml.Tests.Parser
{
    [TestFixture]
    public class TagTest
    {
        [Test]
        public void Equals()
        {
            var tag = new Tag("!", "something");

            Assert.That(tag.Equals("!something"), Is.True);
            Assert.That(tag.Equals("!somethinga"), Is.False);
            Assert.That(tag.Equals("!somothing"), Is.False);
        }
    }
}