# LiteYaml

[![GitHub license](https://img.shields.io/github/license/hadashiA/LiteYaml)](./LICENSE)
![Unity 2022.2+](https://img.shields.io/badge/unity-2021.3+-000.svg)
[![NuGet](https://img.shields.io/nuget/v/LiteYaml.svg)](https://www.nuget.org/packages/LiteYaml)
[![openupm](https://img.shields.io/npm/v/jp.hadashikick.vyaml?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/jp.hadashikick.vyaml/)

Lightweight yaml parser and emitter stripped from [VYaml](https://github.com/hadashiA/VYaml)

- [LiteYaml](#liteyaml)
  - [Parser](#parser)
  - [Emitter](#emitter)
    - [Emit string in various formats](#emit-string-in-various-formats)
    - [Emit sequences and other structures](#emit-sequences-and-other-structures)
  - [YAML 1.2 spec support status](#yaml-12-spec-support-status)
    - [Implicit primitive type conversion of scalar](#implicit-primitive-type-conversion-of-scalar)
    - [https://yaml.org/spec/1.2.2/](#httpsyamlorgspec122)
  - [Credits](#credits)
  - [Author](#author)
  - [License](#license)

## Parser

`YamlParser` struct provides access to the complete meta-information of yaml.

- `YamlParser.Read()` reads through to the next syntax on yaml. (If end of stream then return false.)
- `YamlParser.ParseEventType` indicates the state of the currently read yaml parsing result.
- How to access scalar value:
    - `YamlParser.GetScalarAs*` families take the result of converting a scalar at the current position to a specified type.
    - `YamlParser.TryGetScalarAs*` families return true and take a result if the current position is a scalar and of the specified type.
    - `YamlParser.ReadScalarAs*` families is similar to GetScalarAs*, but advances the present position to after the scalar read.
- How to access meta information:
    - `YamlParser.TryGetTag(out Tag tag)` 
    - `YamlParser.TryGetCurrentAnchor(out Anchor anchor)`

Basic example:

```csharp
var parser = YamlParser.FromBytes(utf8Bytes);

// YAML contains more than one `Document`. 
// Here we skip to before first document content.
parser.SkipAfter(ParseEventType.DocumentStart);

// Scanning...
while (parser.Read())
{
    // If the current syntax is Scalar, 
    if (parser.CurrentEventType == ParseEventType.Scalar)
    {
        var intValue = parser.GetScalarAsInt32();
        var stringValue = parser.GetScalarAsString();
        // ...
        
        if (parser.TryGetCurrentTag(out var tag))
        {
            // Check for the tag...
        }
        
        if (parser.TryGetCurrentAnchor(out var anchor))
        {
            // Check for the anchor...
        }        
    }
    
    // If the current syntax is Sequence (Like a list in yaml)
    else if (parser.CurrentEventType == ParseEventType.SequenceStart)
    {
        // We can check for the tag...
        // We can check for the anchor...
        
        parser.Read(); // Skip SequenceStart

        // Read to end of sequence
        while (!parser.End && parser.CurrentEventType != ParseEventType.SequenceEnd)
        {
             // A sequence element may be a scalar or other...
             if (parser.CurrentEventType = ParseEventType.Scalar)
             {
                 // ...
             }
             // ...
             // ...
             else
             {
                 // We can skip current element. (It could be a scalar, or alias, sequence, mapping...)
                 parser.SkipCurrentNode();
             }
        }
        parser.Read(); // Skip SequenceEnd.
    }
    
    // If the current syntax is Mapping (like a Dictionary in yaml)
    else if (parser.CurrentEventType == ParseEventType.MappingStart)
    {
        // We can check for the tag...
        // We can check for the anchor...
        
        parser.Read(); // Skip MappingStart

        // Read to end of mapping
        while (parser.CurrentEventType != ParseEventType.MappingEnd)
        {
             // After Mapping start, key and value appear alternately.
             
             var key = parser.ReadScalarAsString();  // if key is scalar
             var value = parser.ReadScalarAsString(); // if value is scalar
             
             // Or we can skip current key/value. (It could be a scalar, or alias, sequence, mapping...)
             // parser.SkipCurrentNode(); // skip key
             // parser.SkipCurrentNode(); // skip value
        }
        parser.Read(); // Skip MappingEnd.
    }
    
    // Alias
    else if (parser.CurrentEventType == ParseEventType.Alias)
    {
        // If Alias is used, the previous anchors must be holded somewhere.
        // In the High level Deserialize API, `YamlDeserializationContext` does exactly this. 
    }
}
```

See [test code](https://github.com/hadashiA/LiteYaml/blob/master/LiteYaml.Tests/Parser/SpecTest.cs) for more information.
The above test covers various patterns for the order of `ParsingEvent`.

## Emitter

`Utf8YamlEmitter` struct provides to write YAML formatted string.

Basic usage:

``` csharp
var buffer = new ArrayBufferWriter();
var emitter = new Utf8YamlEmitter(buffer); // It needs buffer implemented `IBufferWriter<byte>`

emitter.BeginMapping(); // Mapping is a collection like Dictionary in YAML
{
    emitter.WriteString("key1");
    emitter.WriteString("value-1");
    
    emitter.WriteString("key2");
    emitter.WriteInt32(222);
    
    emitter.WriteString("key3");
    emitter.WriteFloat(3.333f);
}
emitter.EndMapping();
```

``` csharp
// If you want to expand a string in memory, you can do this.
System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan); 
```

``` yaml
key1: value-1
key2: 222
key3: 3.333
```

### Emit string in various formats

By default, WriteString() automatically determines the format of a scalar. 

Multi-line strings are automatically format as a literal scalar:

``` csharp
emitter.WriteString("Hello,\nWorld!\n");
```

``` yaml
|
  Hello,
  World!
```

Special characters contained strings are automatically quoted.

``` csharp
emitter.WriteString("&aaaaa ");
```

``` yaml
"&aaaaa "
```

Or you can specify the style explicitly:

``` csharp
emitter.WriteString("aaaaaaa", ScalarStyle.Literal);
```

``` yaml
|-
  aaaaaaaa
```

### Emit sequences and other structures

e.g:

``` csharp
emitter.BeginSequence();
{
    emitter.BeginSequence(SequenceStyle.Flow);
    {
        emitter.WriteInt32(100);
        emitter.WriteString("&hoge");
        emitter.WriteString("bra");
    }
    emitter.EndSequence();
    
    emitter.BeginMapping();
    {
        emitter.WriteString("key1");
        emitter.WriteString("item1");
        
        emitter.WriteString("key2");
        emitter.BeginSequence();
        {
            emitter.WriteString("nested-item1")
            emitter.WriteString("nested-item2")
            emitter.BeginMapping();
            {
                emitter.WriteString("nested-key1")
                emitter.WriteInt32(100)
            }
            emitter.EndMapping();
        }
        emitter.EndSequence();
    }
    emitter.EndMapping();
}
emitter.EndMapping();
```

``` yaml
- [100, "&hoge", bra]
- key1: item1
  key2:
  - nested-item1
  - nested-item2
  - nested-key1: 100
```
    
## YAML 1.2 spec support status

### Implicit primitive type conversion of scalar

The following is the default implicit type interpretation.

Basically, it follows YAML Core Schema.
https://yaml.org/spec/1.2.2/#103-core-schema

| Support            | Regular expression                                                    | Resolved to type     |
| :----------------- | :-------------------------------------------------------------------- | :------------------- |
| :white_check_mark: | `null \| Null \| NULL \| ~`                                           | null                 |
| :white_check_mark: | `/* Empty */`                                                         | null                 |
| :white_check_mark: | `true \| True \| TRUE \| false \| False \| FALSE`                     | boolean              |
| :white_check_mark: | `[-+]? [0-9]+`                                                        | int  (Base 10)       |
| :white_check_mark: | `0o [0-7]+`                                                           | int (Base 8)         |
| :white_check_mark: | `0x [0-9a-fA-F]+`                                                     | int (Base 16)        |
| :white_check_mark: | `[-+]? ( \. [0-9]+ \| [0-9]+ ( \. [0-9]* )? ) ( [eE] [-+]? [0-9]+ )?` | float                |
| :white_check_mark: | `[-+]? ( \.inf \| \.Inf \| \.INF )`                                   | float (Infinity)     |
| :white_check_mark: | `\.nan \| \.NaN \| \.NAN`                                             | float (Not a number) |

### https://yaml.org/spec/1.2.2/

Following is the results of the [test](https://github.com/hadashiA/LiteYaml/blob/master/LiteYaml.Tests/Parser/SpecTest.cs) for the examples from the  [yaml spec page](https://yaml.org/spec/1.2.2/).

- 2.1. Collections
  - :white_check_mark: Example 2.1 Sequence of Scalars (ball players)
  - :white_check_mark: Example 2.2 Mapping Scalars to Scalars (player statistics)
  - :white_check_mark: Example 2.3 Mapping Scalars to Sequences (ball clubs in each league)
  - :white_check_mark: Example 2.4 Sequence of Mappings (players statistics)
  - :white_check_mark: Example 2.5 Sequence of Sequences
  - :white_check_mark: Example 2.6 Mapping of Mappings
- 2.2. Structures
  - :white_check_mark: Example 2.7 Two Documents in a Stream (each with a leading comment)
  - :white_check_mark: Example 2.8 Play by Play Feed from a Game
  - :white_check_mark: Example 2.9 Single Document with Two Comments
  - :white_check_mark: Example 2.10 Node for Sammy Sosa appears twice in this document
  - :white_check_mark: Example 2.11 Mapping between Sequences
  - :white_check_mark: Example 2.12 Compact Nested Mapping
- 2.3. Scalars
  - :white_check_mark: Example 2.13 In literals, newlines are preserved
  - :white_check_mark: Example 2.14 In the folded scalars, newlines become spaces
  - :white_check_mark: Example 2.15 Folded newlines are preserved for more indented and blank lines
  - :white_check_mark: Example 2.16 Indentation determines scope
  - :white_check_mark: Example 2.17 Quoted Scalars
  - :white_check_mark: Example 2.18 Multi-line Flow Scalars
- 2.4. Tags
  - :white_check_mark: Example 2.19 Integers
  - :white_check_mark: Example 2.20 Floating Point
  - :white_check_mark: Example 2.21 Miscellaneous
  - :white_check_mark: Example 2.22 Timestamps
  - :white_check_mark: Example 2.23 Various Explicit Tags
  - :white_check_mark: Example 2.24 Global Tags
  - :white_check_mark: Example 2.25 Unordered Sets
  - :white_check_mark: Example 2.26 Ordered Mappings
- 2.5. Full Length Example
  - :white_check_mark: Example 2.27 Invoice
  - :white_check_mark: Example 2.28 Log File
- 5.2. Character Encodings
  - :white_check_mark: Example 5.1 Byte Order Mark
  - :white_check_mark: Example 5.2 Invalid Byte Order Mark
- 5.3. Indicator Characters
  - :white_check_mark: Example 5.3 Block Structure Indicators
  - :white_check_mark: Example 5.4 Flow Collection Indicators
  - :white_check_mark: Example 5.5 Comment Indicator
  - :white_check_mark: Example 5.6 Node Property Indicators
  - :white_check_mark: Example 5.7 Block Scalar Indicators
  - :white_check_mark: Example 5.8 Quoted Scalar Indicators
  - :white_check_mark: Example 5.9 Directive Indicator
  - :white_check_mark: Example 5.10 Invalid use of Reserved Indicators
- 5.4. Line Break Characters
  - :white_check_mark: Example 5.11 Line Break Characters
  - :white_check_mark: Example 5.12 Tabs and Spaces
  - :white_check_mark: Example 5.13 Escaped Characters
  - :white_check_mark: Example 5.14 Invalid Escaped Characters
- 6.1. Indentation Spaces
  - :white_check_mark: Example 6.1 Indentation Spaces
  - :white_check_mark: Example 6.2 Indentation Indicators
- 6.2. Separation Spaces
  - :white_check_mark: Example 6.3 Separation Spaces
- 6.3. Line Prefixes
  - :white_check_mark: Example 6.4 Line Prefixes
- 6.4. Empty Lines
  - :white_check_mark: Example 6.5 Empty Lines
- 6.5. Line Folding
  - :white_check_mark: Example 6.6 Line Folding
  - :white_check_mark: Example 6.7 Block Folding
  - :white_check_mark: Example 6.8 Flow Folding
- 6.6. Comments
  - :white_check_mark: Example 6.9 Separated Comment
  - :white_check_mark: Example 6.10 Comment Lines
  - :white_check_mark: Example 6.11 Multi-Line Comments
- 6.7. Separation Lines
  - :white_check_mark: Example 6.12 Separation Spaces
- 6.8. Directives
  - :white_check_mark: Example 6.13 Reserved Directives
  - :white_check_mark: Example 6.14 YAML directive
  - :white_check_mark: Example 6.15 Invalid Repeated YAML directive
  - :white_check_mark: Example 6.16 TAG directive
  - :white_check_mark: Example 6.17 Invalid Repeated TAG directive
  - :white_check_mark: Example 6.18 Primary Tag Handle
  - :white_check_mark: Example 6.19 Secondary Tag Handle
  - :white_check_mark: Example 6.20 Tag Handles
  - :white_check_mark: Example 6.21 Local Tag Prefix
  - :white_check_mark: Example 6.22 Global Tag Prefix
- 6.9. Node Properties
  - :white_check_mark: Example 6.23 Node Properties
  - :white_check_mark: Example 6.24 Verbatim Tags
  - :white_check_mark: Example 6.25 Invalid Verbatim Tags
  - :white_check_mark: Example 6.26 Tag Shorthands
  - :white_check_mark: Example 6.27 Invalid Tag Shorthands
  - :white_check_mark: Example 6.28 Non-Specific Tags
  - :white_check_mark: Example 6.29 Node Anchors
- 7.1. Alias Nodes
  - :white_check_mark: Example 7.1 Alias Nodes
- 7.2. Empty Nodes
  - :white_check_mark: Example 7.2 Empty Content
  - :white_check_mark: Example 7.3 Completely Empty Flow Nodes
- 7.3. Flow Scalar Styles
  - :white_check_mark: Example 7.4 Double Quoted Implicit Keys
  - :white_check_mark: Example 7.5 Double Quoted Line Breaks
  - :white_check_mark: Example 7.6 Double Quoted Lines
  - :white_check_mark: Example 7.7 Single Quoted Characters
  - :white_check_mark: Example 7.8 Single Quoted Implicit Keys
  - :white_check_mark: Example 7.9 Single Quoted Lines
  - :white_check_mark: Example 7.10 Plain Characters
  - :white_check_mark: Example 7.11 Plain Implicit Keys
  - :white_check_mark: Example 7.12 Plain Lines
- 7.4. Flow Collection Styles
  - :white_check_mark: Example 7.13 Flow Sequence
  - :white_check_mark: Example 7.14 Flow Sequence Entries
  - :white_check_mark: Example 7.15 Flow Mappings
  - :white_check_mark: Example 7.16 Flow Mapping Entries
  - :white_check_mark: Example 7.17 Flow Mapping Separate Values
  - :white_check_mark: Example 7.18 Flow Mapping Adjacent Values
  - :white_check_mark: Example 7.20 Single Pair Explicit Entry
  - :x: Example 7.21 Single Pair Implicit Entries
  - :white_check_mark: Example 7.22 Invalid Implicit Keys
  - :white_check_mark: Example 7.23 Flow Content
  - :white_check_mark: Example 7.24 Flow Nodes
- 8.1. Block Scalar Styles
  - :white_check_mark: Example 8.1 Block Scalar Header
  - :x: Example 8.2 Block Indentation Indicator
  - :white_check_mark: Example 8.3 Invalid Block Scalar Indentation Indicators
  - :white_check_mark: Example 8.4 Chomping Final Line Break
  - :white_check_mark: Example 8.5 Chomping Trailing Lines
  - :white_check_mark: Example 8.6 Empty Scalar Chomping
  - :white_check_mark: Example 8.7 Literal Scalar
  - :white_check_mark: Example 8.8 Literal Content
  - :white_check_mark: Example 8.9 Folded Scalar
  - :white_check_mark: Example 8.10 Folded Lines
  - :white_check_mark: Example 8.11 More Indented Lines
  - :white_check_mark: Example 8.12 Empty Separation Lines
  - :white_check_mark: Example 8.13 Final Empty Lines
  - :white_check_mark: Example 8.14 Block Sequence
  - :white_check_mark: Example 8.15 Block Sequence Entry Types
  - :white_check_mark: Example 8.16 Block Mappings
  - :white_check_mark: Example 8.17 Explicit Block Mapping Entries
  - :white_check_mark: Example 8.18 Implicit Block Mapping Entries
  - :white_check_mark: Example 8.19 Compact Block Mappings
  - :white_check_mark: Example 8.20 Block Node Types
  - :white_check_mark: Example 8.21 Block Scalar Nodes
  - :white_check_mark: Example 8.22 Block Collection Nodes

## Credits

LiteYaml is a stripped back version of [VYaml](https://github.com/hadashiA/VYaml) with minor updates.

## Author

[@hadashiA](https://github.com/hadashiA)

## License

[MIT](https://github.com/EPD-Libraries/LiteYaml/blob/master/License.md)
