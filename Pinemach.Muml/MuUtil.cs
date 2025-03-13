using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinemach.Muml;

public enum MuTextType {
    DoubleQuote, // "text"
    SingleQuote, // 'text'
    Backtick, // `text`
    DoubleQuoteFence, // """text"""
    SingleQuoteFence, // '''text'''
    BacktickFence, // ```text```
    Default = DoubleQuote,
}

/// <summary>
/// Contains an assortment of utility and helper functions related
/// to Muml functionality.
/// </summary>
public static class MuUtil {
    /// <summary>
    /// Check whether a character is a Muml metacharacter.
    /// The metacharacters are ASCII <pre>#=|;&amp;()[]{}`'"</pre>.
    /// This includes hashes, equals, open and close braces, open and close
    /// brackets, backticks, single quotes, and double quotes.
    /// </summary>
    public static bool IsMetaChar(int ch) => (
        ch is '#' or '=' or '|' or '&' or ';' or '(' or ')' or '[' or ']' or '{' or '}' or '`' or '"' or '\''
    );
    
    /// <summary>
    /// Check whether a character is a Muml string literal quote character.
    /// The quote characters are ASCII <pre>`'"</pre>, i.e. backticks,
    /// single quotes, and double quotes.
    /// </summary>
    public static bool IsQuoteChar(int ch) => (
        ch is '`' or '"' or '\''
    );
    
    /// <summary>
    /// Check whether a character is an ASCII whitespace character.
    /// </summary>
    public static bool IsWhitespaceChar(int ch) => (
        ch is ' ' or '\t' or '\r' or '\n'
    );
    
    /// <summary>
    /// Check whether a character is valid in a Muml identifier.
    /// A Muml identifier may contain any characters except for whitespace
    /// and metacharacters.
    /// </summary>
    public static bool IsIdentifierChar(int ch) => (
        ch >= 0 && !MuUtil.IsMetaChar(ch) && !MuUtil.IsWhitespaceChar(ch)
    );
    
    /// <summary>
    /// Count the most number of times that a given character appears
    /// consecutively in the given text.
    /// </summary>
    internal static int CountMaxConsecutiveChars(string text, char ch) {
        int run = 0;
        int maxRun = 0;
        for(int i = 0; i < text.Length; i++) {
            if(text[i] == ch) {
                run++;
            }
            else if(run != 0) {
                maxRun = run > maxRun ? run : maxRun;
                run = 0;
            }
        }
        return run > maxRun ? run : maxRun;
    }
    
    /// <summary>
    /// Helper to get an integer value from a hexadecimal digit character.
    /// </summary>
    internal static int ParseHexDigit(int digit) {
        if(digit >= '0' && digit <= '9') return digit - '0';
        else if(digit >= 'a' && digit <= 'f') return digit - 'a' + 10;
        else if(digit >= 'A' && digit <= 'F') return digit - 'A' + 10;
        else return -1;
    }
    
    /// <summary>
    /// Get an escape sequence string corresponding to a given character.
    /// Returns null when the character has no corresponding escape sequence.
    /// </summary>
    public static string GetCharEscape(
        int ch,
        bool escDoubleQuote = false,
        bool escSingleQuote = false
    ) => ch switch {
        0x00 => @"\0",
        0x01 => @"\x01",
        0x02 => @"\x02",
        0x03 => @"\x03",
        0x04 => @"\x04",
        0x05 => @"\x05",
        0x06 => @"\x06",
        0x07 => @"\a",
        0x08 => @"\b",
        0x09 => @"\t",
        0x0a => @"\n",
        0x0b => @"\v",
        0x0c => @"\f",
        0x0d => @"\r",
        0x0e => @"\x0e",
        0x0f => @"\x0f",
        0x10 => @"\x10",
        0x11 => @"\x11",
        0x12 => @"\x12",
        0x13 => @"\x13",
        0x14 => @"\x14",
        0x15 => @"\x15",
        0x16 => @"\x16",
        0x17 => @"\x17",
        0x18 => @"\x18",
        0x19 => @"\x19",
        0x1a => @"\x1a",
        0x1b => @"\e",
        0x1c => @"\x1c",
        0x1d => @"\x1d",
        0x1e => @"\x1e",
        0x1f => @"\x1f",
        0x7f => @"\x7f",
        '\\' => @"\\",
        '"' => escDoubleQuote ? @"\""" : null,
        '\'' => escSingleQuote ? @"\'" : null,
        _ => null,
    };
    
    /// <summary>
    /// Unescape a string literal using backslash escape sequences.
    /// The returned int represents the first index of a malformed escape
    /// sequence, or -1 if there were no malformed escape sequences.
    /// </summary>
    public static (int, string) UnescapeQuotedString(string text) {
        if(string.IsNullOrEmpty(text)) return (-1, "");
        StringBuilder sb = new(text.Length);
        int i = 0;
        int j = 0;
        int invalidEscapeIndex = -1;
        for(; i < text.Length; i++) {
            (int length, string esc) = MuUtil.getQuotedStringUnescape(text, i);
            if(length > 0) {
                if(i > j) sb.Append(text[j..i]);
                sb.Append(esc);
                i += length;
                j = i;
                i--;
            }
            else if(length < 0 && invalidEscapeIndex < 0) {
                invalidEscapeIndex = i;
            }
        }
        if(i > j) sb.Append(text[j..i]);
        return (invalidEscapeIndex, sb.ToString());
    }
    
    /// <summary>
    /// Helper used by UnescapeQuotedString to identify and handle individual
    /// escape sequences within a string.
    /// If the returned int is -1, this indicates an invalid escape sequence.
    /// </summary>
    private static (int, string) getQuotedStringUnescape(string text, int index) {
        if(index >= text.Length || text[index] != '\\') {
            return (0, "\0");
        }
        else if(index >= text.Length - 1) {
            return (-1, "\0");
        }
        char chInitial = text[index + 1];
        if(chInitial == 'x') {
            return MuUtil.getQuotedStringUnescapeUtf8(text, index);
        }
        else if(chInitial == 'u') {
            int hex = MuUtil.getQuotedStringUnescapeHex4(text, index);
            return hex >= 0 ? (6, ((char) hex).ToString()) : (-1, "\0");
        }
        else if(chInitial == 'U') {
            int hex = MuUtil.getQuotedStringUnescapeHex8(text, index);
            if(hex < 0) return (-1, "\0");
            string utf16 = MuUnicode.EncodeUtf16CodePoint(hex);
            return string.IsNullOrEmpty(utf16) ? (-1, "\0") : (10, utf16);
        }
        return chInitial switch {
            '0' => (2, ((char) 0x00).ToString()),
            'a' => (2, ((char) 0x07).ToString()),
            'b' => (2, ((char) 0x08).ToString()),
            't' => (2, ((char) 0x09).ToString()),
            'n' => (2, ((char) 0x0a).ToString()),
            'v' => (2, ((char) 0x0b).ToString()),
            'f' => (2, ((char) 0x0c).ToString()),
            'r' => (2, ((char) 0x0d).ToString()),
            'e' => (2, ((char) 0x1b).ToString()),
            '\\' => (2, "\\"),
            '\'' => (2, "'"),
            '"' => (2, "\""),
            _ => (-1, "\0"),
        };
    }
    
    private static (int, string) getQuotedStringUnescapeUtf8(string text, int index) {
        // First code unit
        int hex1 = MuUtil.getQuotedStringUnescapeHex2(text, index);
        if(hex1 < 0) return (-1, "\0");
        int len = MuUnicode.GetUtf8CodePointLength(hex1);
        if(len is < 0 or > 4) return (-1, "\0");
        else if(len == 1) {
            return (4, ((char) hex1).ToString());
        };
        // Second code unit
        int hex2 = MuUtil.getQuotedStringUnescapeHex2(text, index + 4);
        if(hex2 < 0) return (-1, "\0");
        if(len == 2) {
            int point2 = MuUnicode.DecodeUtf8CodePoint(hex1, hex2);
            return (8, ((char) point2).ToString());
        }
        // Third code unit
        int hex3 = MuUtil.getQuotedStringUnescapeHex2(text, index + 8);
        if(hex3 < 0) return (-1, "\0");
        if(len == 3) {
            int point3 = MuUnicode.DecodeUtf8CodePoint(hex1, hex2, hex3);
            return (12, ((char) point3).ToString());
        }
        // Fourth code unit
        int hex4 = MuUtil.getQuotedStringUnescapeHex2(text, index + 12);
        if(hex4 < 0) return (-1, "\0");
        int point4 = MuUnicode.DecodeUtf8CodePoint(hex1, hex2, hex3, hex4);
        string utf16 = MuUnicode.EncodeUtf16CodePoint(point4);
        return string.IsNullOrEmpty(utf16) ? (-1, "\0") : (16, utf16);
    }
    
    private static int getQuotedStringUnescapeHex2(string text, int index) {
        if(index >= text.Length - 3) return -1;
        int h = MuUtil.ParseHexDigit(text[index + 2]);
        int l = MuUtil.ParseHexDigit(text[index + 3]);
        return h >= 0 && l >= 0 ? ((h << 4) | l) : -1;
    }
    
    private static int getQuotedStringUnescapeHex4(string text, int index) {
        if(index >= text.Length - 5) return -1;
        int b3 = MuUtil.ParseHexDigit(text[index + 2]);
        int b2 = MuUtil.ParseHexDigit(text[index + 3]);
        int b1 = MuUtil.ParseHexDigit(text[index + 4]);
        int b0 = MuUtil.ParseHexDigit(text[index + 5]);
        if(b0 < 0 || b1 < 0 || b2 < 0 || b3 < 0) return -1;
        return b0 | (b1 << 4) | (b2 << 8) | (b3 << 12);
    }
    
    private static int getQuotedStringUnescapeHex8(string text, int index) {
        if(index >= text.Length - 9) return -1;
        int b7 = MuUtil.ParseHexDigit(text[index + 2]);
        int b6 = MuUtil.ParseHexDigit(text[index + 3]);
        int b5 = MuUtil.ParseHexDigit(text[index + 4]);
        int b4 = MuUtil.ParseHexDigit(text[index + 5]);
        int b3 = MuUtil.ParseHexDigit(text[index + 6]);
        int b2 = MuUtil.ParseHexDigit(text[index + 7]);
        int b1 = MuUtil.ParseHexDigit(text[index + 8]);
        int b0 = MuUtil.ParseHexDigit(text[index + 9]);
        if(
            b0 < 0 || b1 < 0 || b2 < 0 || b3 < 0 ||
            b4 < 0 || b5 < 0 || b6 < 0 || b7 < 0
        ) {
            return -1;
        }
        return (
            b0 | (b1 << 4) | (b2 << 8) | (b3 << 12) |
            (b4 << 16) | (b5 << 20) | (b6 << 24) | (b7 << 28)
        );
    }
    
    /// <summary>
    /// Unescape a string literal using backtick escapes.
    /// </summary>
    public static string UnescapeBacktickString(string text) {
        if(string.IsNullOrEmpty(text)) return "";
        StringBuilder sb = new(text.Length);
        int i = 0;
        int j = 0;
        for(; i < text.Length - 1; i++) {
            if(text[i] == '`' && text[i + 1] == '`') {
                if(i > j) sb.Append(text[j..i]);
                sb.Append('`');
                i++;
                j = i + 1;
            }
        }
        if(i > j) sb.Append(text[j..i]);
        return sb.ToString();
    }
    
    /// <summary>
    /// Check whether a given string forms a valid Muml identifier.
    /// A Muml identifier is a non-empty string with no whitespace or
    /// metacharacters.
    /// </summary>
    public static bool IsIdentifierString(string text) {
        if(string.IsNullOrEmpty(text)) return false;
        foreach(char ch in text) {
            if(!MuUtil.IsIdentifierChar(ch)) return false;
        }
        return true;
    }
    
    /// <summary>
    /// Format text as a string literal enclosed in double quotes.
    /// Special characters within the string are escaped using backslashes.
    /// </summary>
    public static string ToQuotedString(string text) => MuUtil.ToQuotedStringDoubleQuoted(text);
    
    /// <summary>
    /// Format text as a string literal of the given type.
    /// If the string is impossible to represent as a literal of the given
    /// type, then a fallback format will be used instead.
    /// </summary>
    public static string ToQuotedString(string text, MuTextType textType) => textType switch {
        // TODO: fence lengths, fallbacks when not well formed. formatting?
        MuTextType.DoubleQuote => MuUtil.ToQuotedStringDoubleQuoted(text),
        MuTextType.SingleQuote => MuUtil.ToQuotedStringSingleQuoted(text),
        MuTextType.Backtick => MuUtil.ToQuotedStringBackticks(text),
        MuTextType.DoubleQuoteFence => MuUtil.ToQuotedStringDoubleQuoteFence(text),
        MuTextType.SingleQuoteFence => MuUtil.ToQuotedStringSingleQuoteFence(text),
        MuTextType.BacktickFence => MuUtil.ToQuotedStringBacktickFence(text),
        _ => MuUtil.ToQuotedStringDoubleQuoted(text),
    };
    
    /// <summary>
    /// If the given text forms a valid Muml identifier, then return it as-is.
    /// Otherwise, format it as a double quoted string literal.
    /// </summary>
    public static string ToIdentifierString(string text) => (
        !MuUtil.IsIdentifierString(text) ?
        MuUtil.ToQuotedStringDoubleQuoted(text) :
        text
    );
    
    /// <summary>
    /// If the given text forms a valid Muml identifier, then return it as-is.
    /// Otherwise, try to format it as a string literal of the given type.
    /// </summary>
    public static string ToIdentifierString(string text, MuTextType textType) => (
        !MuUtil.IsIdentifierString(text) ?
        MuUtil.ToQuotedString(text, textType) :
        text
    );
    
    /// <summary>
    /// Format text as a string literal enclosed in double quotes.
    /// Special characters within the string are escaped using backslashes.
    /// </summary>
    public static string ToQuotedStringDoubleQuoted(string text) {
        if(string.IsNullOrEmpty(text)) return "\"\"";
        return MuUtil.toQuotedStringBody(text, chQuote: '"');
    }
    
    /// <summary>
    /// Format text as a string literal enclosed in single quotes.
    /// Special characters within the string are escaped using backslashes.
    /// </summary>
    public static string ToQuotedStringSingleQuoted(string text) {
        if(string.IsNullOrEmpty(text)) return "''";
        return MuUtil.toQuotedStringBody(text, chQuote: '\'');
    }
    
    /// <summary>
    /// Format text as a string literal enclosed in backticks.
    /// Backticks within the text are escaped as double backticks.
    /// </summary>
    public static string ToQuotedStringBackticks(string text) {
        if(string.IsNullOrEmpty(text)) return "``";
        StringBuilder sb = new(4 + text.Length + (text.Length / 8));
        if(text[0] == '`') {
            sb.Append(MuUtil.IsWhitespaceChar(text[^1]) ? "|;*` " : "|;` ");
        }
        else {
            sb.Append('`');
        }
        int i = 0;
        int j = 0;
        for(; i < text.Length; i++) {
            if(text[i] == '`') {
                if(i > j) sb.Append(text[j..i]);
                sb.Append("``");
                j = i + 1;
            }
        }
        if(i > j) sb.Append(text[j..i]);
        sb.Append('`');
        return sb.ToString();
    }
    
    /// <summary>
    /// Used by ToQuotedStringDoubleQuoted and ToQuotedStringSingleQuoted.
    /// </summary>
    private static string toQuotedStringBody(
        string text,
        int chQuote = -1,
        string fence = null
    ) {
        StringBuilder sb = new(4 + text.Length + (text.Length / 8));
        if(fence != null) sb.Append(fence);
        if(chQuote >= 0) sb.Append((char) chQuote);
        int i = 0;
        int j = 0;
        for(; i < text.Length; i++) {
            string esc = MuUtil.GetCharEscape(
                text[i],
                escDoubleQuote: chQuote == '"',
                escSingleQuote: chQuote == '\''
            );
            if(esc != null) {
                if(i > j) sb.Append(text[j..i]);
                sb.Append(esc);
                j = i + 1;
            }
            else if(!string.IsNullOrEmpty(fence) && (
                i < text.Length - fence.Length &&
                text[i..(i+fence.Length)] == fence
            )) {
                if(i > j) sb.Append(text[j..i]);
                sb.Append('\\');
                sb.Append(fence);
                i += fence.Length - 1;
                j = i + 1;
            }
            else if(!string.IsNullOrEmpty(fence) && (
                (i == 0 && text[i] == fence[^1]) ||
                (i == text.Length - 1 && text[i] == fence[0])
            )) {
                if(i > j) sb.Append(text[j..i]);
                sb.Append('\\');
                sb.Append(text[i]);
                j = i + 1;
            }
        }
        if(i > j) sb.Append(text[j..i]);
        if(fence != null) sb.Append(fence);
        if(chQuote >= 0) sb.Append((char) chQuote);
        return sb.ToString();
    }
    
    /// <summary>
    /// Format text as a string literal enclosed in double quote fences.
    /// Special characters within the string are escaped using backslashes.
    /// </summary>
    public static string ToQuotedStringDoubleQuoteFence(string text) {
        if(string.IsNullOrEmpty(text)) return "\"\"";
        int quoteLength = MuUtil.CountMaxConsecutiveChars(text, '"');
        int fenceLength = Math.Max(3, 1 + quoteLength);
        if(fenceLength >= 8) fenceLength = 3;
        string fence = new string('"', fenceLength);
        return MuUtil.toQuotedStringBody(text, fence: fence);
    }
    
    /// <summary>
    /// Format text as a string literal enclosed in single quote fences.
    /// Special characters within the string are escaped using backslashes.
    /// </summary>
    public static string ToQuotedStringSingleQuoteFence(string text) {
        if(string.IsNullOrEmpty(text)) return "''";
        int quoteLength = MuUtil.CountMaxConsecutiveChars(text, '\'');
        int fenceLength = Math.Max(3, 1 + quoteLength);
        if(fenceLength >= 8) fenceLength = 3;
        string fence = new string('\'', fenceLength);
        return MuUtil.toQuotedStringBody(text, fence: fence);
    }
    
    /// <summary>
    /// Format text as a string literal enclosed in backtick fences.
    /// Backticks within backtick fences can't be escaped.
    /// </summary>
    public static string ToQuotedStringBacktickFence(string text) {
        if(string.IsNullOrEmpty(text)) return "``";
        int quoteLength = MuUtil.CountMaxConsecutiveChars(text, '`');
        int fenceLength = Math.Max(3, 1 + quoteLength);
        string fence = new string('`', fenceLength);
        bool startBacktick = (text[0] == '`');
        bool endBacktick = (text[^1] == '`');
        if(startBacktick && endBacktick) {
            return "|;" + fence + ' ' + text + ' ' + fence;
        }
        else if(startBacktick) {
            string format = MuUtil.IsWhitespaceChar(text[^1]) ? "|;*" : "|;";
            return format + fence + ' ' + text + fence;
        }
        else if(endBacktick) {
            string format = MuUtil.IsWhitespaceChar(text[0]) ? "|^" : "|;";
            return format + fence + text + ' ' + fence;
        }
        else {
            return fence + text + fence;
        }
    }
    
    /// <summary>
    /// Used by ToQuotedStringDoubleQuoteFence and ToQuotedStringSingleQuoteFence.
    /// </summary>
    private static string toQuotedStringFenceBody(string text, string fence) {
        // TODO update this
        StringBuilder sb = new();
        sb.Append('\'');
        int i = 0;
        int j = 0;
        for(; i < text.Length; i++) {
            if(i < text.Length - 2 && text[i..(i+2)] == fence) {
                if(i > j) sb.Append(text[j..i]);
                sb.Append("\\" + fence);
                i += 2;
                j = i + 1;
            }
        }
        if(i > j) sb.Append(text[j..i]);
        sb.Append('\'');
        return sb.ToString();
    }
    
    /// <summary>
    /// Helper to check sequence equality. Considers null and empty sequences equal.
    /// </summary>
    internal static bool SequencesEqual<T>(IEnumerable<T> seq1, IEnumerable<T> seq2) => (
        MuUtil.SequencesEqual(seq1, seq2, EqualityComparer<T>.Default)
    );
    internal static bool SequencesEqual<T>(IEnumerable<T> seq1, IEnumerable<T> seq2, IEqualityComparer<T> comparer) {
        if(seq1 == null) return seq2 == null || !seq2.Any();
        else if(seq2 == null) return !seq1.Any();
        else return seq1.SequenceEqual(seq2, comparer);
    }
}
