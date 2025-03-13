using System.Collections.Generic;

namespace Pinemach.Muml;

/// <summary>
/// Formatting options for how to handle lines within a multi-line Muml
/// string.
/// </summary>
public enum MuTextFormatSpecifierBlock {
    /// <summary>
    /// Keep text as-is.
    /// This is also the default with no format specifier present.
    /// Also corresponds to <pre>|=</pre> and <pre>|^</pre> format specifiers.
    /// </summary>
    Keep, // "|=" or "|^"
    /// <summary>
    /// Strip leading whitespace from the string.
    /// Corresponds to the format specifier <pre>|;</pre>.
    /// </summary>
    Strip, // "|;"
    /// <summary>
    /// Deindent lines, based on the indentation of the first non-blank line.
    /// Corresponds to the format specifier <pre>|</pre>.
    /// </summary>
    Deindent, // "||"
    /// <summary>
    /// Fold lines, similar to Markdown, or YAML's folded multi-line strings.
    /// Corresponds to the format specifier <pre>|></pre>.
    /// </summary>
    Fold, // "|>"
}

/// <summary>
/// Formatting options for how to handle lines at the end of a multi-line
/// Muml string.
/// </summary>
public enum MuTextFormatSpecifierEnd {
    /// <summary>
    /// Don't remove any trailing whitespace.
    /// Append an asterisk <pre>*</pre> to use this behavior with
    /// a block format specifier such as <pre>||</pre>.
    /// This is the default with a <pre>|=</pre> specifier.
    /// </summary>
    KeepAll, // "*"
    /// <summary>
    /// Remove all but one trailing newline.
    /// This is the default with a <pre>||</pre> or <pre>|></pre> specifier.
    /// </summary>
    KeepOneLine, // "$"
    /// <summary>
    /// Don't remove any trailing newlines, but do trim trailing
    /// whitespace after the last newline.
    /// Append a plus <pre>+</pre> to use this behavior with
    /// a block format specifier such as <pre>||</pre>.
    /// This is also the default with no format specifier present.
    /// </summary>
    KeepAllLines, // "+"
    /// <summary>
    /// Strip all trailing whitespace.
    /// Append a minus <pre>-</pre> to use this behavior with
    /// a block format specifier such as <pre>||</pre>.
    /// This is the default with a <pre>|^</pre> or <pre>|;</pre> specifier.
    /// </summary>
    Strip, // "-"
}

/// <summary>
/// Format settings relevant for parsing multi-line strings.
/// Text format specifiers start with a vertical bar character.
/// </summary>
public struct MuTextFormatSpecifier {
    public static readonly MuTextFormatSpecifier Default = new(
        MuTextFormatSpecifierBlock.Keep,
        MuTextFormatSpecifierEnd.KeepAll
    );
    
    public MuTextFormatSpecifierBlock BlockLines;
    public MuTextFormatSpecifierEnd EndLines;
    public int FirstLineIndent;

    public MuTextFormatSpecifier(
        MuTextFormatSpecifierBlock blockLines,
        MuTextFormatSpecifierEnd endLines,
        int firstLineIndent = 0
    ) {
        this.BlockLines = blockLines;
        this.EndLines = endLines;
        this.FirstLineIndent = firstLineIndent;
    }
    
    /// <summary>
    /// Apply formatting settings to a string taken from Muml source.
    /// </summary>
    public string ApplyFormat(string text) {
        return MuTextFormatSpecifier.applyEndFormat(
            this.EndLines,
            MuTextFormatSpecifier.applyBlockFormat(this.BlockLines, this.FirstLineIndent, text)
        );
    }
    
    private static string applyBlockFormat(
        MuTextFormatSpecifierBlock format,
        int firstLineIndent,
        string text
    ) => format switch {
        MuTextFormatSpecifierBlock.Keep => text,
        MuTextFormatSpecifierBlock.Strip => text.TrimStart(' ', '\t', '\r', '\n'),
        MuTextFormatSpecifierBlock.Deindent => string.Join("", MuTextFormatSpecifier.iterLinesDeindented(text, firstLineIndent)),
        MuTextFormatSpecifierBlock.Fold => string.Join("", MuTextFormatSpecifier.iterLinesFolded(text, firstLineIndent)),
        _ => text,
    };
    
    private static string applyEndFormat(MuTextFormatSpecifierEnd format, string text) {
        if(format == MuTextFormatSpecifierEnd.KeepAll) {
            return text;
        }
        else if(format == MuTextFormatSpecifierEnd.KeepAllLines) {
            return text.TrimEnd(' ', '\t', '\r');
        }
        else if(format == MuTextFormatSpecifierEnd.Strip) {
            return text.TrimEnd(' ', '\t', '\r', '\n');
        }
        string trimmedText = text.TrimEnd(' ', '\t', '\r');
        if(trimmedText.EndsWith("\r\n")) {
            return trimmedText.TrimEnd(' ', '\t', '\r', '\n') + "\r\n";
        }
        else if(trimmedText.EndsWith('\n')) {
            return trimmedText.TrimEnd(' ', '\t', '\r', '\n') + '\n';
        }
        else {
            return trimmedText;
        }
    }
    
    private static IEnumerable<string> iterLines(string? text) {
        if(string.IsNullOrEmpty(text)) yield break;
        int i = 0;
        while(i < text.Length) {
            int newline = text.IndexOf('\n', i);
            if(newline < 0) {
                yield return text[i..];
                yield break;
            }
            newline++;
            yield return text[i..newline];
            i = newline;
        }
    }
    
    private static IEnumerable<string> iterLinesDeindented(string? text, int firstLineIndent = 0) {
        string? indent = null;
        foreach(string line in MuTextFormatSpecifier.iterLines(text)) {
            if(string.IsNullOrEmpty(line)) {
                if(indent != null) yield return "";
            }
            else if(indent == null) {
                int i = 0;
                while(i < line.Length && MuUtil.IsWhitespaceChar(line[i])) i++;
                if(i >= line.Length) continue;
                i -= firstLineIndent;
                indent = line[..i];
                yield return line[i..];
            }
            else {
                int i = 0;
                while(i < line.Length && i < indent.Length && line[i] == indent[i]) i++;
                yield return line[i..];
            }
        }
    }
    
    private static IEnumerable<string> iterLinesFolded(string? text, int firstLineIndent = 0) {
        IEnumerable<string> lines = MuTextFormatSpecifier.iterLinesDeindented(
            text,
            firstLineIndent
        );
        List<string> para = new();
        string? paraLineEnding = "\n";
        foreach(string line in lines) {
            (string? trimmedLine, string? lineEnding) = (
                MuTextFormatSpecifier.splitTrailingWhitespace(line)
            );
            if(string.IsNullOrEmpty(trimmedLine)) {
                if(para.Count > 0) {
                    yield return string.Join(" ", para) + paraLineEnding;
                    para.Clear();
                }
                else {
                    yield return line;
                }
            }
            else {
                if(MuUtil.IsWhitespaceChar(trimmedLine[0]) && para.Count > 0) {
                    yield return string.Join(" ", para) + paraLineEnding;
                    para.Clear();
                }
                para.Add(trimmedLine);
                paraLineEnding = lineEnding;
            }
        }
        if(para.Count > 0) {
            yield return string.Join(" ", para) + paraLineEnding;
            para.Clear();
        }
    }
    
    private static (string?, string?) splitTrailingWhitespace(string text) {
        if(string.IsNullOrEmpty(text)) return (null, null);
        for(int i = text.Length - 1; i >= 0; i--) {
            if(!MuUtil.IsWhitespaceChar(text[i])) {
                return (text[..(i + 1)], text[(i + 1)..]);
            }
        }
        return (null, text);
    }
}
