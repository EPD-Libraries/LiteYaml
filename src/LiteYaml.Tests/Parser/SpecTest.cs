using LiteYaml.Internal;
using LiteYaml.Parser;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteYaml.Tests.Parser;

[TestFixture]
public class SpecTest
{
    [Test]
    public void Ex2_01_SeqScalars()
    {
        AssertParseEvents(SpecExamples.EX2_1, [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, "Ken Griffey"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_02_MappingScalarsToScalars()
    {
        AssertParseEvents(SpecExamples.EX2_2,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.Scalar, 65),
            Expect(ParseEventType.Scalar, "avg"),
            Expect(ParseEventType.Scalar, 0.278),
            Expect(ParseEventType.Scalar, "rbi"),
            Expect(ParseEventType.Scalar, 147),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd)
        ]);
    }

    [Test]
    public void Ex2_03_MappingScalarsToSequences()
    {
        AssertParseEvents(SpecExamples.EX2_3,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "american"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Boston Red Sox"),
            Expect(ParseEventType.Scalar, "Detroit Tigers"),
            Expect(ParseEventType.Scalar, "New York Yankees"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.Scalar, "national"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "New York Mets"),
            Expect(ParseEventType.Scalar, "Chicago Cubs"),
            Expect(ParseEventType.Scalar, "Atlanta Braves"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_04_SequenceOfMappings()
    {
        AssertParseEvents(SpecExamples.EX2_4,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "name"),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.Scalar, 65),
            Expect(ParseEventType.Scalar, "avg"),
            Expect(ParseEventType.Scalar, 0.278),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "name"),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.Scalar, 63),
            Expect(ParseEventType.Scalar, "avg"),
            Expect(ParseEventType.Scalar, 0.288),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_05_SequenceOfSequences()
    {
        AssertParseEvents(SpecExamples.EX2_5,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "name"),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.Scalar, "avg"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, 65),
            Expect(ParseEventType.Scalar, 0.278),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, 63),
            Expect(ParseEventType.Scalar, 0.288),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_06_MappingOfMappings()
    {
        AssertParseEvents(SpecExamples.EX2_6,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.Scalar, 65),
            Expect(ParseEventType.Scalar, "avg"),
            Expect(ParseEventType.Scalar, 0.278),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.Scalar, 63),
            Expect(ParseEventType.Scalar, "avg"),
            Expect(ParseEventType.Scalar, 0.288),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_07_TwoDocumentsInAStream()
    {
        AssertParseEvents(SpecExamples.EX2_7,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, "Ken Griffey"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),

            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Chicago Cubs"),
            Expect(ParseEventType.Scalar, "St Louis Cardinals"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_08_PlayByPlayFeed()
    {
        AssertParseEvents(SpecExamples.EX2_8,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "time"),
            Expect(ParseEventType.Scalar, "20:03:20"),
            Expect(ParseEventType.Scalar, "player"),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, "action"),
            Expect(ParseEventType.Scalar, "strike (miss)"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),

            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "time"),
            Expect(ParseEventType.Scalar, "20:03:47"),
            Expect(ParseEventType.Scalar, "player"),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, "action"),
            Expect(ParseEventType.Scalar, "grand slam"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_09_SingleDocumentWithTwoComments()
    {
        AssertParseEvents(SpecExamples.EX2_9,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.Scalar, "rbi"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, "Ken Griffey"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_10_SimpleAnchor()
    {
        AssertParseEvents(SpecExamples.EX2_10,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.Scalar, "rbi"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.Scalar, "Ken Griffey"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_11_MappingBetweenSequences()
    {
        AssertParseEvents(SpecExamples.EX2_11,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Detroit Tigers"),
            Expect(ParseEventType.Scalar, "Chicago cubs"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "2001-07-23"),
            Expect(ParseEventType.SequenceEnd),

            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "New York Yankees"),
            Expect(ParseEventType.Scalar, "Atlanta Braves"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "2001-07-02"),
            Expect(ParseEventType.Scalar, "2001-08-12"),
            Expect(ParseEventType.Scalar, "2001-08-14"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_12_CompactNestedMapping()
    {
        AssertParseEvents(SpecExamples.EX2_12,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "item"),
            Expect(ParseEventType.Scalar, "Super Hoop"),
            Expect(ParseEventType.Scalar, "quantity"),
            Expect(ParseEventType.Scalar, 1),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "item"),
            Expect(ParseEventType.Scalar, "Basketball"),
            Expect(ParseEventType.Scalar, "quantity"),
            Expect(ParseEventType.Scalar, 4),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "item"),
            Expect(ParseEventType.Scalar, "Big Shoes"),
            Expect(ParseEventType.Scalar, "quantity"),
            Expect(ParseEventType.Scalar, 1),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_13_InLiteralsNewlinesArePreserved()
    {
        AssertParseEvents(SpecExamples.EX2_13,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "\\//||\\/||\n// ||  ||__\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_14_InFoldedScalarsNewlinesBecomeSpaces()
    {
        AssertParseEvents(SpecExamples.EX2_14,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "Mark McGwire's year was crippled by a knee injury.\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_15_FoldedNewlinesArePreservedForMoreIndentedAndBlankLines()
    {
        AssertParseEvents(SpecExamples.EX2_15,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "Sammy Sosa completed another fine season with great stats.\n" +
                                          "\n" +
                                          "  63 Home Runs\n" +
                                          "  0.288 Batting Average\n" +
                                          "\n" +
                                          "What a year!\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_16_IndentationDeterminesScope()
    {
        AssertParseEvents(SpecExamples.EX2_16,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "name"),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, "accomplishment"),
            Expect(ParseEventType.Scalar, "Mark set a major league home run record in 1998.\n"),
            Expect(ParseEventType.Scalar, "stats"),
            Expect(ParseEventType.Scalar, "65 Home Runs\n0.278 Batting Average\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_17_QuotedScalars()
    {
        AssertParseEvents(SpecExamples.EX2_17,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "unicode"),
            Expect(ParseEventType.Scalar, "Sosa did fine.\u263A"),
            Expect(ParseEventType.Scalar, "control"),
            Expect(ParseEventType.Scalar, "\b1998\t1999\t2000\n"),
            Expect(ParseEventType.Scalar, "hex esc"),
            Expect(ParseEventType.Scalar, "\r\n is \r\n"),
            Expect(ParseEventType.Scalar, "single"),
            Expect(ParseEventType.Scalar, "\"Howdy!\" he cried."),
            Expect(ParseEventType.Scalar, "quoted"),
            Expect(ParseEventType.Scalar, " # Not a 'comment'."),
            Expect(ParseEventType.Scalar, "tie-fighter"),
            Expect(ParseEventType.Scalar, "|\\-*-/|"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_18_MultiLIneFlowScalars()
    {
        AssertParseEvents(SpecExamples.EX2_18,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "plain"),
            Expect(ParseEventType.Scalar, "This unquoted scalar spans many lines."),
            Expect(ParseEventType.Scalar, "quoted"),
            Expect(ParseEventType.Scalar, "So does this quoted scalar.\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_19_Integers()
    {
        AssertParseEvents(SpecExamples.EX2_19,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "canonical"),
            Expect(ParseEventType.Scalar, 12345),
            Expect(ParseEventType.Scalar, "decimal"),
            Expect(ParseEventType.Scalar, +12345),
            Expect(ParseEventType.Scalar, "octal"),
            Expect(ParseEventType.Scalar, 12), // 0o14
            Expect(ParseEventType.Scalar, "hexadecimal"),
            Expect(ParseEventType.Scalar, 0xC),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_20_FloatingPoint()
    {
        AssertParseEvents(SpecExamples.EX2_20,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "canonical"),
            Expect(ParseEventType.Scalar, 1230.15),
            Expect(ParseEventType.Scalar, "exponential"),
            Expect(ParseEventType.Scalar, 1230.15),
            Expect(ParseEventType.Scalar, "fixed"),
            Expect(ParseEventType.Scalar, 1230.15),
            Expect(ParseEventType.Scalar, "negative infinity"),
            Expect(ParseEventType.Scalar, double.NegativeInfinity),
            Expect(ParseEventType.Scalar, "not a number"),
            Expect(ParseEventType.Scalar, double.NaN),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_21_Miscellaneous()
    {
        AssertParseEvents(SpecExamples.EX2_21,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "booleans"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, true),
            Expect(ParseEventType.Scalar, false),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.Scalar, "string"),
            Expect(ParseEventType.Scalar, "012345"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_22_Timestamps()
    {
        AssertParseEvents(SpecExamples.EX2_22,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "canonical"),
            Expect(ParseEventType.Scalar, "2001-12-15T02:59:43.1Z"),
            Expect(ParseEventType.Scalar, "iso8601"),
            Expect(ParseEventType.Scalar, "2001-12-14t21:59:43.10-05:00"),
            Expect(ParseEventType.Scalar, "spaced"),
            Expect(ParseEventType.Scalar, "2001-12-14 21:59:43.10 -5"),
            Expect(ParseEventType.Scalar, "date"),
            Expect(ParseEventType.Scalar, "2002-12-14"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_23_VariousExplicitTags()
    {
        AssertParseEvents(SpecExamples.EX2_23,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "not-date"),
            Expect(ParseEventType.Scalar, "2002-04-28"),
            Expect(ParseEventType.Scalar, "picture"),
            Expect(ParseEventType.Scalar, "R0lGODlhDAAMAIQAAP//9/X\n" +
                                          "17unp5WZmZgAAAOfn515eXv\n" +
                                          "Pz7Y6OjuDg4J+fn5OTk6enp\n" +
                                          "56enmleECcgggoBADs=\n"),
            Expect(ParseEventType.Scalar, "application specific tag"),
            Expect(ParseEventType.Scalar, "The semantics of the tag\n" +
                                          "above may be different for\n" +
                                          "different documents.\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_24_GlobalTags()
    {
        AssertParseEvents(SpecExamples.EX2_24,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "center"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "x"),
            Expect(ParseEventType.Scalar, 73),
            Expect(ParseEventType.Scalar, "y"),
            Expect(ParseEventType.Scalar, 129),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.Scalar, "radius"),
            Expect(ParseEventType.Scalar, 7),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "start"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.Scalar, "finish"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "x"),
            Expect(ParseEventType.Scalar, 89),
            Expect(ParseEventType.Scalar, "y"),
            Expect(ParseEventType.Scalar, 102),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "start"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.Scalar, "color"),
            Expect(ParseEventType.Scalar, "0xFFEEBB"),
            Expect(ParseEventType.Scalar, "text"),
            Expect(ParseEventType.Scalar, "Pretty vector drawing."),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_25_UnorderedSets()
    {
        AssertParseEvents(SpecExamples.EX2_25,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "Ken Griffey"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_26_OrderedMappings()
    {
        AssertParseEvents(SpecExamples.EX2_26,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Mark McGwire"),
            Expect(ParseEventType.Scalar, 65),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Sammy Sosa"),
            Expect(ParseEventType.Scalar, 63),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Ken Griffey"),
            Expect(ParseEventType.Scalar, 58),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_27_Invoice()
    {
        AssertParseEvents(SpecExamples.EX2_27,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "invoice"),
            Expect(ParseEventType.Scalar, 34843),
            Expect(ParseEventType.Scalar, "date"),
            Expect(ParseEventType.Scalar, "2001-01-23"),
            Expect(ParseEventType.Scalar, "bill-to"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "given"),
            Expect(ParseEventType.Scalar, "Chris"),
            Expect(ParseEventType.Scalar, "family"),
            Expect(ParseEventType.Scalar, "Dumars"),
            Expect(ParseEventType.Scalar, "address"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "lines"),
            Expect(ParseEventType.Scalar, "458 Walkman Dr.\nSuite #292\n"),
            Expect(ParseEventType.Scalar, "city"),
            Expect(ParseEventType.Scalar, "Royal Oak"),
            Expect(ParseEventType.Scalar, "state"),
            Expect(ParseEventType.Scalar, "MI"),
            Expect(ParseEventType.Scalar, "postal"),
            Expect(ParseEventType.Scalar, 48046),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.Scalar, "ship-to"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.Scalar, "product"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "sku"),
            Expect(ParseEventType.Scalar, "BL394D"),
            Expect(ParseEventType.Scalar, "quantity"),
            Expect(ParseEventType.Scalar, 4),
            Expect(ParseEventType.Scalar, "description"),
            Expect(ParseEventType.Scalar, "Basketball"),
            Expect(ParseEventType.Scalar, "price"),
            Expect(ParseEventType.Scalar, 450.00),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "sku"),
            Expect(ParseEventType.Scalar, "BL4438H"),
            Expect(ParseEventType.Scalar, "quantity"),
            Expect(ParseEventType.Scalar, 1),
            Expect(ParseEventType.Scalar, "description"),
            Expect(ParseEventType.Scalar, "Super Hoop"),
            Expect(ParseEventType.Scalar, "price"),
            Expect(ParseEventType.Scalar, 2392.00),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.Scalar, "tax"),
            Expect(ParseEventType.Scalar, 251.42),
            Expect(ParseEventType.Scalar, "total"),
            Expect(ParseEventType.Scalar, 4443.52),
            Expect(ParseEventType.Scalar, "comments"),
            Expect(ParseEventType.Scalar, "Late afternoon is best. Backup contact is Nancy Billsmer @ 338-4338."),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex2_28_LogFile()
    {
        AssertParseEvents(SpecExamples.EX2_28,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Time"),
            Expect(ParseEventType.Scalar, "2001-11-23 15:01:42 -5"),
            Expect(ParseEventType.Scalar, "User"),
            Expect(ParseEventType.Scalar, "ed"),
            Expect(ParseEventType.Scalar, "Warning"),
            Expect(ParseEventType.Scalar, "This is an error message for the log file"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),

            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Time"),
            Expect(ParseEventType.Scalar, "2001-11-23 15:02:31 -5"),
            Expect(ParseEventType.Scalar, "User"),
            Expect(ParseEventType.Scalar, "ed"),
            Expect(ParseEventType.Scalar, "Warning"),
            Expect(ParseEventType.Scalar, "A slightly different error message."),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),

            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Date"),
            Expect(ParseEventType.Scalar, "2001-11-23 15:03:17 -5"),
            Expect(ParseEventType.Scalar, "User"),
            Expect(ParseEventType.Scalar, "ed"),
            Expect(ParseEventType.Scalar, "Fatal"),
            Expect(ParseEventType.Scalar, "Unknown variable \"bar\""),
            Expect(ParseEventType.Scalar, "Stack"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "file"),
            Expect(ParseEventType.Scalar, "TopClass.py"),
            Expect(ParseEventType.Scalar, "line"),
            Expect(ParseEventType.Scalar, 23),
            Expect(ParseEventType.Scalar, "code"),
            Expect(ParseEventType.Scalar, "x = MoreObject(\"345\\n\")\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "file"),
            Expect(ParseEventType.Scalar, "MoreClass.py"),
            Expect(ParseEventType.Scalar, "line"),
            Expect(ParseEventType.Scalar, 58),
            Expect(ParseEventType.Scalar, "code"),
            Expect(ParseEventType.Scalar, "foo = bar"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex5_01_ByteOrderMarkNoContent()
    {
        AssertParseEvents(SpecExamples.EX5_1,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex5_02_InvalidByteOrderMarkInContent()
    {
        AssertParseEventsThenThrows<YamlTokenizerException>(SpecExamples.EX5_2,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            // TODO should probably throw after this token:
            // Expect(ParseEventType.Scalar, "Invalid use of BOM"),
        ],
        exceptionLike: "^BOM must be at the beginning of the stream");
    }

    [Test]
    public void Ex5_03_BlockStructureIndicators()
    {
        AssertParseEvents(SpecExamples.EX5_3,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "sequence"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "one"),
            Expect(ParseEventType.Scalar, "two"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.Scalar, "mapping"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "sky"),
            Expect(ParseEventType.Scalar, "blue"),
            Expect(ParseEventType.Scalar, "sea"),
            Expect(ParseEventType.Scalar, "green"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex5_04_FlowStructureIndicators()
    {
        AssertParseEvents(SpecExamples.EX5_4,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "sequence"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "one"),
            Expect(ParseEventType.Scalar, "two"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.Scalar, "mapping"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "sky"),
            Expect(ParseEventType.Scalar, "blue"),
            Expect(ParseEventType.Scalar, "sea"),
            Expect(ParseEventType.Scalar, "green"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex5_06_NodePropertyIndicators()
    {
        AssertParseEvents(SpecExamples.EX5_6,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "anchored"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.Scalar, "alias"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex5_07_BlockScalarIndicators()
    {
        AssertParseEvents(SpecExamples.EX5_7,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "literal"),
            Expect(ParseEventType.Scalar, "some\ntext\n"),
            Expect(ParseEventType.Scalar, "folded"),
            Expect(ParseEventType.Scalar, "some text\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex5_08_QuotedScalarIndicators()
    {
        AssertParseEvents(SpecExamples.EX5_8,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "single"),
            Expect(ParseEventType.Scalar, "text"),
            Expect(ParseEventType.Scalar, "double"),
            Expect(ParseEventType.Scalar, "text"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);

    }

    [Test]
    public void Ex5_11_LineBreakCharacters()
    {
        AssertParseEvents(SpecExamples.EX5_11,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "Line break (no glyph)\nLine break (glyphed)\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex5_12_TabsAndSpaces()
    {
        AssertParseEvents(SpecExamples.EX5_12,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "quoted"),
            Expect(ParseEventType.Scalar, "Quoted \t"),
            Expect(ParseEventType.Scalar, "block"),
            Expect(ParseEventType.Scalar, "void main() {\n" +
                                          "\tprintf(\"Hello, world!\\n\");\n" +
                                          "}\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex5_13_EscapedCharacters()
    {
        AssertParseEvents(SpecExamples.EX5_13,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "Fun with \\"),
            Expect(ParseEventType.Scalar, "\" \u0007 \b \u001b \f"),
            Expect(ParseEventType.Scalar, "\n \r \t \u000b \u0000"),
            Expect(ParseEventType.Scalar, "\u0020 \u00a0 \u0085 \u2028 \u2029 A A A"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_01_IndentationSpaces()
    {
        AssertParseEvents(SpecExamples.EX6_1,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Not indented"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "By one space"),
            Expect(ParseEventType.Scalar, "By four\n  spaces\n"),
            Expect(ParseEventType.Scalar, "Flow style"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "By two"),
            Expect(ParseEventType.Scalar, "Also by two"),
            Expect(ParseEventType.Scalar, "Still by two"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);

    }

    [Test]
    public void Ex6_02_IndentationIndicators()
    {
        AssertParseEvents(SpecExamples.EX6_2,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "a"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "b"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "c"),
            Expect(ParseEventType.Scalar, "d"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_03_SeparationSpaces()
    {
        AssertParseEvents(SpecExamples.EX6_3,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "baz"),
            Expect(ParseEventType.Scalar, "baz"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_04_LinePrefixes()
    {
        AssertParseEvents(SpecExamples.EX6_4,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "plain"),
            Expect(ParseEventType.Scalar, "text lines"),
            Expect(ParseEventType.Scalar, "quoted"),
            Expect(ParseEventType.Scalar, "text lines"),
            Expect(ParseEventType.Scalar, "block"),
            Expect(ParseEventType.Scalar, "text\n \tlines\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_05_EmptyLines()
    {
        AssertParseEvents(SpecExamples.EX6_5,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "Folding"),
            Expect(ParseEventType.Scalar, "Empty line\nas a line feed"),
            Expect(ParseEventType.Scalar, "Chomping"),
            Expect(ParseEventType.Scalar, "Clipped empty lines\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_06_LineFolding()
    {
        AssertParseEvents(SpecExamples.EX6_6,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "trimmed\n\n\nas space"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_07_BlockFolding()
    {
        AssertParseEvents(SpecExamples.EX6_7,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "foo \n\n\t bar\n\nbaz\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_08_FlowFolding()
    {
        AssertParseEvents(SpecExamples.EX6_8,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, " foo\nbar\nbaz "),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_09_SeparatedComment()
    {
        AssertParseEvents(SpecExamples.EX6_9,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "key"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_10_CommentLines()
    {
        AssertParseEvents(SpecExamples.EX6_10,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_11_MultilineComments()
    {
        AssertParseEvents(SpecExamples.EX6_11,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "key"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_12_SeparationSpaces()
    {
        AssertParseEvents(SpecExamples.EX6_12,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "first"),
            Expect(ParseEventType.Scalar, "Sammy"),
            Expect(ParseEventType.Scalar, "last"),
            Expect(ParseEventType.Scalar, "Sosa"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "hr"),
            Expect(ParseEventType.Scalar, 65),
            Expect(ParseEventType.Scalar, "avg"),
            Expect(ParseEventType.Scalar, 0.278),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_13_ReservedDirectives()
    {
        AssertParseEvents(SpecExamples.EX6_13,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_14_YamlDirective()
    {
        AssertParseEvents(SpecExamples.EX6_14,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_16_YamlDirective()
    {
        AssertParseEvents(SpecExamples.EX6_16,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_18_PrimaryTagHandle()
    {
        AssertParseEvents(SpecExamples.EX6_18,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.DocumentEnd),

            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_19_SecondaryTagHandle()
    {
        AssertParseEvents(SpecExamples.EX6_19,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "1 - 3"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_20_TagHandles()
    {
        AssertParseEvents(SpecExamples.EX6_20,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_21_LocalTagPrefix()
    {
        AssertParseEvents(SpecExamples.EX6_21,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "fluorescent"),
            Expect(ParseEventType.DocumentEnd),

            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "green"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);

    }

    [Test]
    public void Ex6_22_GlocalTagPrefix()
    {
        AssertParseEvents(SpecExamples.EX6_22,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_23_NodeProperties()
    {
        AssertParseEvents(SpecExamples.EX6_23,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.Scalar, "baz"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_24_VerbatimTags()
    {
        AssertParseEvents(SpecExamples.EX6_24,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, "baz"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_26_TagShortHands()
    {
        AssertParseEvents(SpecExamples.EX6_26,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.Scalar, "baz"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    [Ignore("Not Supported")]
    public void Ex6_27_InvalidTagShorthands()
    {
        Assert.Throws<YamlParserException>(() => {
            YamlParser parser = YamlParser.FromBytes(Encoding.UTF8.GetBytes(SpecExamples.EX6_27B));
            while (parser.Read()) {
            }
        });
    }

    [Test]
    public void Ex6_28_NonSpecificTags()
    {
        AssertParseEvents(SpecExamples.EX6_28,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "12"),
            Expect(ParseEventType.Scalar, 12),
            Expect(ParseEventType.Scalar, 12),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex6_29_NodeAnchors()
    {
        AssertParseEvents(SpecExamples.EX6_29,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "First occurrence"),
            Expect(ParseEventType.Scalar, "Value"),
            Expect(ParseEventType.Scalar, "Second occurrence"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_01_AliasNodes()
    {
        AssertParseEvents(SpecExamples.EX7_1,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "First occurrence"),
            Expect(ParseEventType.Scalar, "Foo"),
            Expect(ParseEventType.Scalar, "Second occurrence"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.Scalar, "Override anchor"),
            Expect(ParseEventType.Scalar, "Bar"),
            Expect(ParseEventType.Scalar, "Reuse anchor"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_02_EmptyNodes()
    {
        AssertParseEvents(SpecExamples.EX7_2,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_03_CompletelyEmptyNodes()
    {
        AssertParseEvents(SpecExamples.EX7_3,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_04_DoubleQuotedImplicitKeys()
    {
        AssertParseEvents(SpecExamples.EX7_4,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "implicit block key"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "implicit flow key"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_05_DoubleQuotedLineBreaks()
    {
        AssertParseEvents(SpecExamples.EX7_5,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "folded to a space,\nto a line feed, or \t \tnon-content"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_06_DoubleQuotedLines()
    {
        AssertParseEvents(SpecExamples.EX7_6,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, " 1st non-empty\n2nd non-empty 3rd non-empty "),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_07_SingleQuotedCharacters()
    {
        AssertParseEvents(SpecExamples.EX7_7,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "here's to \"quotes\""),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_08_SingleQuotedImplicitKeys()
    {
        AssertParseEvents(SpecExamples.EX7_8,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "implicit block key"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "implicit flow key"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_09_SingleQuotedLines()
    {
        AssertParseEvents(SpecExamples.EX7_9,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, " 1st non-empty\n2nd non-empty 3rd non-empty "),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_10_PlainCharacters()
    {
        AssertParseEvents(SpecExamples.EX7_10,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "::vector"),
            Expect(ParseEventType.Scalar, ": - ()"),
            Expect(ParseEventType.Scalar, "Up, up, and away!"),
            Expect(ParseEventType.Scalar, -123),
            Expect(ParseEventType.Scalar, "http://example.com/foo#bar"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "::vector"),
            Expect(ParseEventType.Scalar, ": - ()"),
            Expect(ParseEventType.Scalar, "Up, up, and away!"),
            Expect(ParseEventType.Scalar, -123),
            Expect(ParseEventType.Scalar, "http://example.com/foo#bar"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_11_PlainImplicitKeys()
    {
        AssertParseEvents(SpecExamples.EX7_11,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "implicit block key"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "implicit flow key"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_12_PlainLines()
    {
        AssertParseEvents(SpecExamples.EX7_12,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "1st non-empty\n2nd non-empty 3rd non-empty"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_13_FlowSequence()
    {
        AssertParseEvents(SpecExamples.EX7_13,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "one"),
            Expect(ParseEventType.Scalar, "two"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "three"),
            Expect(ParseEventType.Scalar, "four"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_14_FlowSequenceEntries()
    {
        AssertParseEvents(SpecExamples.EX7_14,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "double quoted"),
            Expect(ParseEventType.Scalar, "single quoted"),
            Expect(ParseEventType.Scalar, "plain text"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "nested"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "single"),
            Expect(ParseEventType.Scalar, "pair"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_15_FlowMappings()
    {
        AssertParseEvents(SpecExamples.EX7_15,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "one"),
            Expect(ParseEventType.Scalar, "two"),
            Expect(ParseEventType.Scalar, "three"),
            Expect(ParseEventType.Scalar, "four"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "five"),
            Expect(ParseEventType.Scalar, "six"),
            Expect(ParseEventType.Scalar, "seven"),
            Expect(ParseEventType.Scalar, "eight"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_16_FlowMappingEntries()
    {
        AssertParseEvents(SpecExamples.EX7_16,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "explicit"),
            Expect(ParseEventType.Scalar, "entry"),
            Expect(ParseEventType.Scalar, "implicit"),
            Expect(ParseEventType.Scalar, "entry"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_17_FlowMappingSeparateValues()
    {
        AssertParseEvents(SpecExamples.EX7_17,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "unquoted"),
            Expect(ParseEventType.Scalar, "separate"),
            Expect(ParseEventType.Scalar, "http://foo.com"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "omitted value"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "omitted key"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_18_FlowMappingAdjacentValues()
    {
        AssertParseEvents(SpecExamples.EX7_18,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "adjacent"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.Scalar, "readable"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.Scalar, "empty"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_19_SinglePairFlowMappings()
    {
        AssertParseEvents(SpecExamples.EX7_19,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_20_SinglePairExplicitEntry()
    {
        AssertParseEvents(SpecExamples.EX7_20,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo bar"),
            Expect(ParseEventType.Scalar, "baz"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    [Ignore("")]
    public void Ex7_21_SinglePairImplicitEntries()
    {
        AssertParseEvents(SpecExamples.EX7_21,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "YAML"),
            Expect(ParseEventType.Scalar, "separate"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "empty key entry"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "JSON"),
            Expect(ParseEventType.Scalar, "like"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.Scalar, "adjacent"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_22_InvalidImplicitKeys()
    {
        Assert.Throws<YamlParserException>(() => {
            YamlParser parser = YamlParser.FromBytes(Encoding.UTF8.GetBytes(SpecExamples.EX7_22));
            while (parser.Read()) {
            }
        });
    }

    [Test]
    public void Ex7_23_FlowContent()
    {
        AssertParseEvents(SpecExamples.EX7_23,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "a"),
            Expect(ParseEventType.Scalar, "b"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "a"),
            Expect(ParseEventType.Scalar, "b"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.Scalar, "a"),
            Expect(ParseEventType.Scalar, "b"),
            Expect(ParseEventType.Scalar, "c"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex7_24_FlowNodes()
    {
        AssertParseEvents(SpecExamples.EX7_24,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "a"),
            Expect(ParseEventType.Scalar, "b"),
            Expect(ParseEventType.Scalar, "c"),
            Expect(ParseEventType.Alias),
            Expect(ParseEventType.Scalar, ""),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_01_BlockScalarHeader()
    {
        AssertParseEvents(SpecExamples.EX8_1,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "literal\n"),
            Expect(ParseEventType.Scalar, " folded\n"),
            Expect(ParseEventType.Scalar, "keep\n\n"),
            Expect(ParseEventType.Scalar, " strip"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    [Ignore("")]
    public void Ex8_02_BlockIndentationHeader()
    {
        AssertParseEvents(SpecExamples.EX8_2,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "detected\n"),
            Expect(ParseEventType.Scalar, "\n\n# detected\n"),
            Expect(ParseEventType.Scalar, " explicit\n"),
            Expect(ParseEventType.Scalar, "\t\ndetected\n"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_03_InvalidBlockScalarIndentationIndicators()
    {
        Assert.Throws<YamlParserException>(() => {
            YamlParser parser = YamlParser.FromBytes(Encoding.UTF8.GetBytes(SpecExamples.EX8_3A));
            while (parser.Read()) {
            }
        });

        Assert.Throws<YamlParserException>(() => {
            YamlParser parser = YamlParser.FromBytes(Encoding.UTF8.GetBytes(SpecExamples.EX8_3B));
            while (parser.Read()) {
            }
        });

        Assert.Throws<YamlParserException>(() => {
            YamlParser parser = YamlParser.FromBytes(Encoding.UTF8.GetBytes(SpecExamples.EX8_3C));
            while (parser.Read()) {
            }
        });
    }

    [Test]
    public void Ex8_04_ChompingFinalLineBreak()
    {
        AssertParseEvents(SpecExamples.EX8_4,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "strip"),
            Expect(ParseEventType.Scalar, "text"),
            Expect(ParseEventType.Scalar, "clip"),
            Expect(ParseEventType.Scalar, "text\n"),
            Expect(ParseEventType.Scalar, "keep"),
            Expect(ParseEventType.Scalar, "text\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_05_ChompingTrailingLines()
    {
        AssertParseEvents(SpecExamples.EX8_5,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "strip"),
            Expect(ParseEventType.Scalar, "# text"),
            Expect(ParseEventType.Scalar, "clip"),
            Expect(ParseEventType.Scalar, "# text\n"),
            Expect(ParseEventType.Scalar, "keep"),
            Expect(ParseEventType.Scalar, "# text\n\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_06_EmptyScalarChomping()
    {
        AssertParseEvents(SpecExamples.EX8_6,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "strip"),
            Expect(ParseEventType.Scalar, ""),
            Expect(ParseEventType.Scalar, "clip"),
            Expect(ParseEventType.Scalar, ""),
            Expect(ParseEventType.Scalar, "keep"),
            Expect(ParseEventType.Scalar, "\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_07_LiteralScalar()
    {
        AssertParseEvents(SpecExamples.EX8_7,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "literal\n\ttext\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_08_LiteralContent()
    {
        AssertParseEvents(SpecExamples.EX8_8,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "\n\nliteral\n \n\ntext\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_09_FoldedScalar()
    {
        AssertParseEvents(SpecExamples.EX8_9,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar, "folded text\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_10_FoldedLines()
    {
        AssertParseEvents(SpecExamples.EX8_10,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar,
                "\nfolded line\nnext line\n  * bullet\n\n  * list\n  * lines\n\nlast line\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_11_MoreIndentedLines()
    {
        AssertParseEvents(SpecExamples.EX8_11,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar,
                "\nfolded line\nnext line\n  * bullet\n\n  * list\n  * lines\n\nlast line\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_12_EmptySeparationLines()
    {
        AssertParseEvents(SpecExamples.EX8_12,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar,
                "\nfolded line\nnext line\n  * bullet\n\n  * list\n  * lines\n\nlast line\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_13_FinalEmptyLines()
    {
        AssertParseEvents(SpecExamples.EX8_13,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.Scalar,
                "\nfolded line\nnext line\n  * bullet\n\n  * list\n  * lines\n\nlast line\n"),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_14_BlockSequence()
    {
        AssertParseEvents(SpecExamples.EX8_14,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "block sequence"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "one"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "two"),
            Expect(ParseEventType.Scalar, "three"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_15_BlockSequenceEntryTypes()
    {
        AssertParseEvents(SpecExamples.EX8_15,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "block node\n"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "one"),
            Expect(ParseEventType.Scalar, "two"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "one"),
            Expect(ParseEventType.Scalar, "two"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_16_BlockMappings()
    {
        AssertParseEvents(SpecExamples.EX8_16,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "block mapping"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "key"),
            Expect(ParseEventType.Scalar, "value"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_17_ExplicitBlockMappingEntries()
    {
        AssertParseEvents(SpecExamples.EX8_17,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "explicit key"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "block key\n"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "one"),
            Expect(ParseEventType.Scalar, "two"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_18_ImplicitBlockMappingEntries()
    {
        AssertParseEvents(SpecExamples.EX8_18,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "plain key"),
            Expect(ParseEventType.Scalar, "in-line value"),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, null),
            Expect(ParseEventType.Scalar, "quoted key"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "entry"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_19_CompactBlockMappings()
    {
        AssertParseEvents(SpecExamples.EX8_19,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "sun"),
            Expect(ParseEventType.Scalar, "yellow"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "earth"),
            Expect(ParseEventType.Scalar, "blue"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "moon"),
            Expect(ParseEventType.Scalar, "white"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_20_BlockNodeTypes()
    {
        AssertParseEvents(SpecExamples.EX8_20,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "flow in block"),
            Expect(ParseEventType.Scalar, "Block scalar\n"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_21_BlockScalarNodes()
    {
        AssertParseEvents(SpecExamples.EX8_21,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "literal"),
            Expect(ParseEventType.Scalar, "value\n"),
            Expect(ParseEventType.Scalar, "folded"),
            Expect(ParseEventType.Scalar, "value\n"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    [Test]
    public void Ex8_22_BlockCollectionNodes()
    {
        AssertParseEvents(SpecExamples.EX8_22,
        [
            Expect(ParseEventType.StreamStart),
            Expect(ParseEventType.DocumentStart),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "sequence"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "entry"),
            Expect(ParseEventType.SequenceStart),
            Expect(ParseEventType.Scalar, "nested"),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.SequenceEnd),
            Expect(ParseEventType.Scalar, "mapping"),
            Expect(ParseEventType.MappingStart),
            Expect(ParseEventType.Scalar, "foo"),
            Expect(ParseEventType.Scalar, "bar"),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.MappingEnd),
            Expect(ParseEventType.DocumentEnd),
            Expect(ParseEventType.StreamEnd),
        ]);
    }

    class TestParseResult(
        ParseEventType type,
        Scalar? scalar = null,
        Type? expectScalarDataType = null)
    {
        public ParseEventType Type { get; } = type;
        public Scalar? Scalar { get; } = scalar;
        public Type ExpectScalarDataType { get; } = expectScalarDataType ?? typeof(string);

        public override string ToString()
        {
            return Scalar != null
                ? $"{Type} \"{Scalar}\" ({ExpectScalarDataType.Name})"
                : $"{Type}";
        }
    }

    static TestParseResult Expect(ParseEventType type)
    {
        return new TestParseResult(type);
    }

    static TestParseResult Expect(ParseEventType type, string? scalarValue)
    {
        if (scalarValue is null) {
            return new TestParseResult(type, Scalar.Null);
        }
        byte[] bytes = Encoding.UTF8.GetBytes(scalarValue);
        return new TestParseResult(type, new Scalar(bytes), typeof(string));
    }

    static TestParseResult Expect<TScalar>(ParseEventType type, TScalar scalarValue)
    {
        if (scalarValue is null) {
            return new TestParseResult(type, Scalar.Null);
        }
        byte[] bytes = Encoding.UTF8.GetBytes(scalarValue is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : scalarValue.ToString()!);
        return new TestParseResult(type, new Scalar(bytes), typeof(TScalar));
    }

    static void AssertParseEventsThenThrows<TException>(string yaml, IReadOnlyList<TestParseResult> expects, string exceptionLike)
        where TException : Exception
    {
        TException? ex = Assert.Throws<TException>(
            code: () => {
                YamlParser parser = YamlParser.FromBytes(Encoding.UTF8.GetBytes(yaml));
                try {
                    AssertParseResults(ref parser, expects);
                }
                catch (TException e) {
                    throw new InvalidOperationException("Unexpected exception when parsing.", e);
                }

                parser.Read();
            },
            message: $"Final parser.Read() has not thrown exception, but expected {typeof(TException).Name} with message like '{exceptionLike}'");
        if (!Regex.IsMatch(ex?.Message ?? "", exceptionLike)) {
            Assert.Fail($"Expected exception message pattern: '{exceptionLike}'\n" +
                        $"But was: '{ex?.Message}'");
        }
    }

    static void AssertParseEvents(string yaml, IReadOnlyList<TestParseResult> expects)
    {
        YamlParser parser = YamlParser.FromBytes(Encoding.UTF8.GetBytes(yaml));

        AssertParseResults(ref parser, expects);

        if (parser.Read()) {
            Assert.Fail($"Extra event happened: {parser.CurrentEventType} `{parser.GetScalarAsString()}`");
        }
    }

    static void AssertParseResults(ref YamlParser parser, IReadOnlyList<TestParseResult> expects)
    {
        for (int i = 0; i < expects.Count; i++) {
            TestParseResult expect = expects[i];
            if (!parser.Read()) {
                Assert.Fail($"End of stream, but expected: {expect.Type} {expect.Scalar} at {i}");
            }
            if (parser.CurrentEventType != expect.Type) {
                Assert.Fail($"Expected: {expect} at {i}\n" +
                            $"  But was: {parser.CurrentEventType}");
            }
            if (expect.Scalar != null) {
                if (expect.ExpectScalarDataType == typeof(int)) {
                    expect.Scalar.TryGetInt32(out int expectValue);
                    if (!parser.TryGetScalarAsInt32(out int actualValue) || expectValue != actualValue) {
                        Assert.Fail($"Expected {expectValue} ({expect}) at {i}\n" +
                                    $"  But was: {actualValue}");
                    }
                }
                else if (expect.ExpectScalarDataType == typeof(float) ||
                         expect.ExpectScalarDataType == typeof(double)) {
                    expect.Scalar.TryGetDouble(out double expectValue);
                    if (!parser.TryGetScalarAsDouble(out double actualValue) || Math.Abs(expectValue - actualValue) > 0.001) {
                        Assert.Fail($"Expected {expectValue} of {expect} at {i}\n" +
                                    $"  But was: {actualValue}");
                    }
                }
                else if (expect.ExpectScalarDataType == typeof(bool)) {
                    expect.Scalar.TryGetBool(out bool expectValue);
                    if (!parser.TryGetScalarAsBool(out bool actualValue) || actualValue != expectValue) {
                        Assert.Fail($"Expected {expectValue} of {expect} at {i}\n" +
                                    $"  But was: {actualValue} of {parser.GetScalarAsString()}");
                    }
                }
                else if (expect.Scalar.IsNull()) {
                    if (!parser.IsNullScalar() && parser.GetScalarAsString() != null) {
                        Assert.Fail($"Expected null of {expect} at {i}\n" +
                                    $"  But was {parser.CurrentEventType} \"{parser.GetScalarAsString()}\"");
                    }
                }
                else {
                    if (parser.GetScalarAsString() != expect.Scalar.ToString()) {
                        Assert.Fail($"Expected {expect} at {i}\n" +
                                    $"  But was: {parser.CurrentEventType}  \"{parser.GetScalarAsString()}\"");
                    }
                }
            }
        }
    }
}
