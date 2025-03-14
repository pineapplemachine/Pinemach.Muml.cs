# Pinemach.Muml: Micro Markup Language for C#

Muml (or _μml_) is the Micro Markup Language. It is intended as an alternative to XML with a simplified, less verbose, and more human-readable syntax.

This library implements an API for parsing, serializing, navigating, and manipulating Muml documents in C#. It works with UTF-8 and ASCII string encodings. It does not interpret or coerce data types, but presents everything as strings.

Every Muml document is a self-contained unit. A Muml parser will never access other files, send network requests, or execute arbitrary code in the process of parsing a document.

```muml
# This is an example Muml document

h1 "Hello, world!" [style=bold micro=yes]
p |> '''
    Muml is the Micro Markup Language.
    It's similar to XML, but simplified and less verbose.
'''
ul "When to use Muml" {
    li | You want something like XML but more human-friendly
    li | You want it small, simple, and robust
    li | You want it easy and simple to work with in code
}
a "Homepage" [
    href="https://github.com/pineapplemachine/muml.cs"
]
```

The most important semantic different from XML is the absence of text nodes. With Muml, you must attach text to an element with an explicit name.

```xml
<body>
    <h1>Hello, world!</h1>
    In XML, it's permitted to put text between elements.
    This creates text nodes, distinct from element nodes.
    <span>Neat!</span>
</body>
```

```muml
body {
    h1 | Hello, world!
    text | In Muml, text must be associated with an element.
    span | Gotcha!
}
```

## C# API

This package can be installed with NuGet, e.g. via `nuget install Pinemach.Muml` in a CLI.

**Complete API documentation** for `Pinemach.Muml` is here:

Here's a basic usage example:

```cs
using Pinemach.Muml;

public class Example {
    public static void Main() {
        // Parse Muml document from a file
        var doc = MuDocument.Load(path);
        
        // Manipulate Muml document
        var el = new MuElement("div");
        el.AddAttribute("hello", "world");
        doc.AddMember(el);
        
        // Get Muml source from a MuDocument
        string mumlSrc = doc.ToString();
        Console.WriteLine(mumlSrc);
        
        // Save MuDocument to a file
        doc.Save("mydocument.muml");
    }
}
```

## Document structure

A Muml **document** is an ordered list of one or more member **elements**.

An element has:
- A **name**, always.
- Any number of **values**, each of which is a string.
- Any number of **attributes**, each of which is a `name=value` pair.
- Any number of **members**, each of which is another Muml element.

Here is what an element with one of every component looks like:

```muml
name=value "text" [attrName=attrValue] {memberName}
```

The order and amount of items is not strict, except that the element's name must come first. Values, text, attributes, and members are always associated with the last named element.

### Element name - `MuElement.Name`

Element names are written as either an identifier string `like-so`, or a quoted string inside braces `{"like so"}`. This is the only mandatory part of an element to include.

```muml
name
{"name with spaces and special characters $?[]"}
```

### Element values - `MuElement.Values`

Element values are indicated with an equals sign `=`.

### Element text - `MuElement.Text`

Element text cannot be written as an identifier, but must be written as a quoted string.

### Element attributes - `MuElement.Attributes`

Element attributes are `name=value` attribute assignments enclosed within a pair of brackets `[]`.

Attribute names are not required to be unique.

These are also valid attribute assignments:

```muml
element [attrName]
element [attrName=]
element [=attrValue]
```

Note that in this C# implementation, an omitted attribute name or value is represented as `null` rather than as an empty string.

### Element members - `MuElement.Members`

Element members is a sequence of any number of other elements, enclosed within a pair of braces `{}`.

### Document metadata - `MuDocument.Header` & `MuDocument.Values`

If text or values appear at the beginning of a document, before the first element, then they are interpreted as document metadata.

```muml
"Document header"
=value1
=value2

first-element
```

## Comments

Muml has three options for comments: `# line comments`, `### fenced comments ###`, and `#[ block comments #]`.

A fenced begins with at least three hashes `#` and ends the next time that same number of consecutive hashes is found again.

```muml
# Line comment
h1 | Hello

### Fenced comment ###
span | world

###
Fenced comments can be multi-line.
###
div [style='cool']

#[ Block comment #]
h2 | Lorem

#[
    Block comments can be #[ nested #]
    and can span multiple lines.
#]
span | ipsum
```

## Strings

Strings in Muml are divided into the two categories of **quoted strings** and **identifier strings**.

The distinction between identifier strings and quoted strings is that element names _must_ be written as identifier strings and element text _must_ be written as quoted strings. In all other places, they may be used interchangeably.

```muml
element-name "text associated with the element"
element-name "text" "and more text"
element-name another-element-name
element-name {"another element name"}
```

### String syntax

There are several ways to write quoted strings in Muml.

- Double quoted, `"like this"`.  
- Single quoted, `'like this'`.  
- Backtick enclosed, `` `like this` ``.  
- Double quote fenced, `"""like this"""`.  
- Single quote fenced, `'''like this'''`.  
- Backtick fenced, `` ```like this``` ``.  
- To end of line, `| like this`.

And there are two ways to write identifier strings.

- Plain, `like-this`.
- Braced, `{"like this"}`.

A string that is made up entirely of non-whitespace characters and non-metacharacters (everything but ` \t\r\n``'"()[]{}|&;=#`) can be written as a **plain identifier**, without any enclosing quotes, `like-so`.

Other strings can be written as **braced identifiers** by adding braces around a quoted string, `{"like this"}`.

**Double quoted** and **single quoted** strings may not span across multiple lines. They use backslash escape sequences for special characters, like `\n` or `\"` or `\\`.

**Backtick enclosed** strings may span multiple lines. The only escape sequence recognized inside a backtick string is two consecutive backticks ` `` ` representing a single escaped backtick.

**Double quote fenced** and **single quote fenced** strings are allowed to span multiple lines. They use backslash escape sequences for special characters. They begin with three or more consecutive quote characters and end with the same.

**Backtick fenced** strings may span multiple lines. They begin with three or more consecutive backtick characters and end with the same. They do not recognize any escape sequences whatosever.

**To end of line** strings count as quoted strings when distinguishing between element names and text. They begin with a vertical bar `|` follow by at least one whitespace character. They end at the next newline. The resulting string has any preceding or trailing whitespace stripped from it. These strings do not recognize any escape sequences.

### Format specifiers

Non-identifier strings can be written with a preceding **string format specifier** which affects how multi-line strings should be parsed, like `|` or `|>`.

They are also allowed inside braced identifiers, e.g. `{|; 'name'}`.

See **String format specifiers** for more information about format specifiers.

```muml
element |> '''
    This is folded text,
    like when writing
    `key: > ...` in YAML.
'''
```

### Omitted and empty strings

Note that Muml does distinguish between omitted strings and explicitly defined empty strings. These are represented as `null` and the empty string `""` respectively, in this C# API.

```muml
element # omitted text (null)
element "" # explicit empty text ("")
```

## String escape sequences

Muml recognizes these standard escape sequences:

- `\0` - Null character (0x00)  
- `\a` - Alert (0x07)  
- `\b` - Backspace (0x08)  
- `\t` - Tab (0x09)  
- `\n` - Line feed (0x0a)  
- `\v` - Vertical tab (0x0b)  
- `\f` - Form feed (0x0c)  
- `\r` - Carriage return (0x0d)  
- `\e` - Escape character (0x1b)  
- `\"` - Double quote, same character  
- `\'` - Single quote, same character
- `\\` - Backslash, same character
- `\xhh` - UTF-8 code unit
- `\uhhhh` - UTF-16 code unit
- `\Uhhhhhhhh` - Unicode code point

This C# implementation produces an error for any series of `\xhh` escape sequences that does not represent valid [WTF-8](https://simonsapin.github.io/wtf-8/) text. (This means UTF-8 plus legal surrogate pair characters.)

Muml implementations for languages that use UTF-8 encoded strings internally are encouraged to provide a configuration option to accept arbitrary `\xhh` byte sequences, but should produce an error by default.

## String format specifiers

Strings can optionally be preceded by a format specifier, which is a special series of characters starting with a vertical bar `|`. Format specifiers are intended mainly for use with multi-line strings. They behave similarly to [block scalar headers](https://yaml-multiline.info/) in YAML, with some expanded options.

Muml retains the same line endings as whatever was found in the source when deindenting or folding strings. (This means LF/CRLF is preserved.)

Format specifiers have one mandatory part and two optional parts: Block format, ending format, and first-line indentation.

The block format is one of these:

- `||` - Deindent: Deindent based on indentation of the first non-blank line. Second part defaults to _Keep One_.  
- `|>` - Fold: Deindent lines and fold paragraphs, similar to Markdown. Second part defaults to _Keep One_.  
- `|;` - Strip: Strip leading whitespace. Second part defaults to _Strip_ as well.  
- `|^` - Keep: No change to leading whitespace. Second part defaults to _Strip_.  
- `|=` - Keep: No change to leading whitespace. Second part defaults to _Keep All_.  

The ending format is one of these:

- `$` - Keep One. Remove all trailing whitespace, except one newline if there was any.
- `+` - Keep Lines. Keep all trailing lines, but trim trailing whitespace on the last line.
- `*` - Keep All. Don't remove any trailing whitespace.
- `-` - Strip. Remove any and all trailing whitespace.

The third part, the first-line indentation, is zero or more period `.` characters. This may be relevant when deindenting or folding lines. Each period `.` corresponds to one whitespace character by which the first non-blank line is deliberately indented further than the subsequent lines.

With all of this put together, the format specifier `||+..` for example would deindent lines, keep trailing newlines, and indicate that the first line is intentionally indented by two more characters than the second line.

```muml
e ||+.. '''
      Lorem ipsum dolor sit amet, consectetur adipiscing
    elit, sed do eiusmod tempor incididunt ut labore et
    dolore magna aliqua.

      Ut enim ad minim veniam, quis nostrud exercitation
    ullamco laboris nisi ut aliquip ex ea commodo consequat.
    
'''
```

```
  Lorem ipsum dolor sit amet, consectetur adipiscing
elit, sed do eiusmod tempor incididunt ut labore et
dolore magna aliqua.

  Ut enim ad minim veniam, quis nostrud exercitation
ullamco laboris nisi ut aliquip ex ea commodo consequat.

```

With **deindent** formatting, all lines after the first not-blank line have matching whitespace removed from their beginning.

```muml
p || '''
    Lorem ipsum dolor sit amet, consectetur adipiscing
    elit, sed do eiusmod tempor incididunt ut labore et
    dolore magna aliqua.
    
    Ut enim ad minim veniam, quis nostrud exercitation
    ullamco laboris nisi ut aliquip ex ea commodo consequat.
'''
```

```
Lorem ipsum dolor sit amet, consectetur adipiscing
elit, sed do eiusmod tempor incididunt ut labore et
dolore magna aliqua.

Ut enim ad minim veniam, quis nostrud exercitation
ullamco laboris nisi ut aliquip ex ea commodo consequat.
```

With **fold** formatting, lines separated by only a single newline are treated as being part of a same paragraph.

```muml
p |> '''
    Lorem ipsum dolor sit amet, consectetur adipiscing
    elit, sed do eiusmod tempor incididunt ut labore et
    dolore magna aliqua.
    
    Ut enim ad minim veniam, quis nostrud exercitation
    ullamco laboris nisi ut aliquip ex ea commodo consequat.
'''
```

```
Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.
```

### Repeated element items

The items following an element name are not required to be unique.

Multiple values are all attached to an element in order.

```muml
name =value1 =value2 =value3
```

Multiple text items are all concatenated together in order.

If the new text item is blank, a newline is appended to the previous text. Otherwise, it's concatenated with the previous text, and a single space ` ` is added between them if there would not otherwise be any whitespace in between.

```muml
# These two elements are equivalent
name "Hello," "World!"
name "Hello, World!"

# So are these
name "One two three\nFour five"
name
    | One two
    | three
    |
    | Four
    | five
```

Multiple attribute lists are concatenated in order.

```muml
# These two elements are equivalent
name [x=1 y=2] [z=3 w=4] []
name [x=1 y=2 z=3 w=4]
```

Multiple member lists are concatenated in order.

```muml
# These two elements are equivalent
name {a b} {} {c d}
name {a b c d}
```

## Miscellanea

_Muml_ should be pronounced like _myu-mul_.

The ampersand `&`, semicolon `;`, comma `,`, and parentheses `()` are currently unused metacharacters, reserved for future use.
