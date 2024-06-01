using LiteYaml.Parser;
using NUnit.Framework;

namespace LiteYaml.Tests.Parser;

[TestFixture]
public class TagTest
{
    [Test]
    public void Equals()
    {
        Tag tag = new("!", "something");

        Assert.That(tag.Equals("!something"), Is.True);
        Assert.That(tag.Equals("!somethinga"), Is.False);
        Assert.That(tag.Equals("!somothing"), Is.False);
    }
}