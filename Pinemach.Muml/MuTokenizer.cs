using System;
using System.IO;
using System.Text;

namespace Pinemach.Muml;

/// <summary>
/// Enumeration of recognized Muml token types.
/// </summary>
public enum MuTokenType {
    None = 0,
    Identifier,
    String,
    Equals,
    BeginAttributes,
    EndAttributes,
    BeginMembers,
    EndMembers,
    LineComment,
    FencedComment,
    NestedBlockComment,
}

/// <summary>
/// Represents a single token within Muml source.
/// </summary>
public readonly struct MuToken {
    public static readonly MuToken None = new(MuTokenType.None, MuSourceSpan.None, null);
    
    public readonly MuTokenType Type;
    public readonly MuSourceSpan Span;
    public readonly string Text;
    
    public readonly MuSourceLocation Location { get => this.Span.Start; }
    
    public static MuToken Identifier(MuSourceSpan span, string text) => new(MuTokenType.Identifier, span, text);
    public static MuToken String(MuSourceSpan span, string text) => new(MuTokenType.String, span, text);
    public static MuToken Equals(MuSourceSpan span, string text) => new(MuTokenType.Equals, span, text);
    public static MuToken BeginAttributes(MuSourceSpan span, string text) => new(MuTokenType.BeginAttributes, span, text);
    public static MuToken EndAttributes(MuSourceSpan span, string text) => new(MuTokenType.EndAttributes, span, text);
    public static MuToken BeginMembers(MuSourceSpan span, string text) => new(MuTokenType.BeginMembers, span, text);
    public static MuToken EndMembers(MuSourceSpan span, string text) => new(MuTokenType.EndMembers, span, text);
    public static MuToken LineComment(MuSourceSpan span, string text) => new(MuTokenType.LineComment, span, text);
    public static MuToken FencedComment(MuSourceSpan span, string text) => new(MuTokenType.FencedComment, span, text);
    public static MuToken NestedBlockComment(MuSourceSpan span, string text) => new(MuTokenType.NestedBlockComment, span, text);
    
    public static MuToken Equals(MuSourceSpan span) => MuToken.Equals(span, "=");
    public static MuToken BeginAttributes(MuSourceSpan span) => MuToken.BeginAttributes(span, "[");
    public static MuToken EndAttributes(MuSourceSpan span) => MuToken.EndAttributes(span, "]");
    public static MuToken BeginMembers(MuSourceSpan span) => MuToken.BeginMembers(span, "{");
    public static MuToken EndMembers(MuSourceSpan span) => MuToken.EndMembers(span, "}");
    
    public MuToken(MuTokenType type, MuSourceSpan span, string text) {
        this.Type = type;
        this.Span = span;
        this.Text = text;
    }
    
    public override string ToString() => (
        $"{this.Span.ToLString()} {this.Type} {MuUtil.ToQuotedString(this.Text)}"
    );
    
    public bool IsValid() => this.Type != MuTokenType.None;
    
    public bool IsComment() => this.Type switch {
        MuTokenType.LineComment => true,
        MuTokenType.FencedComment => true,
        MuTokenType.NestedBlockComment => true,
        _ => false,
    };
    public MuCommentType GetCommentType() => this.Type switch {
        MuTokenType.LineComment => MuCommentType.Line,
        MuTokenType.FencedComment => MuCommentType.Fenced,
        MuTokenType.NestedBlockComment => MuCommentType.NestedBlock,
        _ => MuCommentType.None,
    };
    
    public bool IsIdentifier() => this.Type == MuTokenType.Identifier;
    public bool IsString() => this.Type == MuTokenType.String;
    public bool IsEquals() => this.Type == MuTokenType.Equals;
    public bool IsBeginAttributes() => this.Type == MuTokenType.BeginAttributes;
    public bool IsEndAttributes() => this.Type == MuTokenType.EndAttributes;
    public bool IsBeginMembers() => this.Type == MuTokenType.BeginMembers;
    public bool IsEndMembers() => this.Type == MuTokenType.EndMembers;
    public bool IsLineComment() => this.Type == MuTokenType.LineComment;
    public bool IsFencedComment() => this.Type == MuTokenType.FencedComment;
    public bool IsNestedBlockComment() => this.Type == MuTokenType.NestedBlockComment;
}

/// <summary>
/// Implements parsing logic for separating an input text stream into
/// Muml tokens. This is used by the MuParser.
/// </summary>
public class MuTokenizer : IDisposable {
    public readonly MuSourceErrors Errors = new();
    public bool IsOk() => (this.Errors?.Count ?? 0) == 0;
    
    private readonly TextReader reader;
    private MuSourceLocation location;
    private MuSourceLocation tokenStartLocation;
    private MuToken queuedToken = MuToken.None;
    
    public MuTokenizer(TextReader reader) : this(null, reader) {}
    public MuTokenizer(string source) : this(null, new StringReader(source)) {}
    public MuTokenizer(string fileName, string source) : this(fileName, new StringReader(source)) {}
    public MuTokenizer(string fileName, TextReader reader) {
        this.location = new MuSourceLocation(fileName);
        this.tokenStartLocation = this.location;
        this.reader = reader;
    }
    
    /// <summary>
    /// Parse and return the next token in the input text.
    /// Returns MuToken.None when there were no more tokens to parse.
    /// </summary>
    public MuToken NextToken() {
        // Check for a queued token
        if(this.queuedToken.IsValid()) {
            MuToken token = this.queuedToken;
            this.queuedToken = MuToken.None;
            return token;
        }
        // Get next non-whitespace character
        this.skipWhitespace();
        this.tokenStartLocation = this.location;
        int ch = this.chNext();
        // Reserved metacharacter '&', ';', '(', ')'
        bool unexpectedReservedMeta = false;
        while(ch is '&' or ';' or '(' or ')') {
            if(!unexpectedReservedMeta) {
                unexpectedReservedMeta = true;
                this.Errors.AddUnexpectedCharacter(this.tokenStartLocation);
            }
            this.skipWhitespace();
            ch = this.chNext();
        }
        // End of input reached
        if(ch < 0) {
            return MuToken.None;
        }
        // Comment
        if(ch == '#') {
            return this.parseComment();
        }
        // Plain identifier string
        else if(MuUtil.IsIdentifierChar(ch)) {
            return this.parseIdentifier(ch);
        }
        // Quoted string
        else if(ch == '|' || MuUtil.IsQuoteChar(ch)) {
            return this.parseStringLiteral(ch);
        }
        // Begin members list OR braced identifier string
        else if(ch == '{') {
            MuSourceLocation braceEndLocation = this.location;
            this.skipWhitespace();
            if(this.chPeek() == '|' || MuUtil.IsQuoteChar(this.chPeek())) {
                return this.parseBracesIdentifier(braceEndLocation);
            }
            return MuToken.BeginMembers(
                this.tokenStartLocation.SpanTo(braceEndLocation)
            );
        }
        // End members list
        else if(ch == '}') {
            return MuToken.EndMembers(this.getTokenSpan());
        }
        // Begin attributes list
        else if(ch == '[') {
            return MuToken.BeginAttributes(this.getTokenSpan());
        }
        // End attributes list
        else if(ch == ']') {
            return MuToken.EndAttributes(this.getTokenSpan());
        }
        // Add value (outside attributes list) or assign attribute (in list)
        else if(ch == '=') {
            return MuToken.Equals(this.getTokenSpan());
        }
        // This branch should be unreachable
        else {
            return MuToken.None;
        }
    }
    
    private MuToken parseComment() {
        if(this.chPeek() == '#') {
            this.chNext();
            if(this.chPeek() == '#') {
                this.chNext();
                return this.parseFencedComment();
            }
            else {
                return this.parseLineComment("#");
            }
        }
        else if(this.chPeek() == '[') {
            this.chNext();
            return this.parseNestedBlockComment();
        }
        else {
            return this.parseLineComment();
        }
    }
    
    private MuToken parseLineComment(string initial = null) {
        StringBuilder sb = new(initial);
        while(true) {
            int ch = this.chNext();
            if(ch < 0 || ch == '\n') break;
            sb.Append((char) ch);
        }
        return MuToken.LineComment(
            this.getTokenSpan(),
            sb.ToString()
        );
    }
    
    private MuToken parseFencedComment() {
        StringBuilder sb = new();
        int fenceLength = 3;
        while(this.chPeek() == '#') {
            fenceLength++;
            this.chNext();
        }
        int run = 0;
        while(true) {
            int ch = this.chNext();
            if(ch == '#') {
                run++;
                if(run >= fenceLength) {
                    sb.Length -= (run - 1);
                    break;
                }
            }
            else if(ch < 0) {
                break;
            }
            else {
                run = 0;
            }
            sb.Append((char) this.chNext());
        }
        return MuToken.FencedComment(
            this.getTokenSpan(),
            sb.ToString()
        );
    }
    
    private MuToken parseNestedBlockComment() {
        StringBuilder sb = new();
        int nest = 1;
        while(true) {
            int ch = this.chNext();
            if(ch < 0) {
                break;
            }
            else if(ch == '#') {
                if(this.chPeek() == '[') {
                    nest++;
                }
                else if(this.chPeek() == ']') {
                    this.chNext();
                    nest--;
                    if(nest <= 0) break;
                }
            }
            sb.Append((char) ch);
        }
        if(nest > 0) {
            this.Errors.AddUnterminatedNestedBlockComment(this.tokenStartLocation);
        }
        return MuToken.NestedBlockComment(
            this.getTokenSpan(),
            sb.ToString()
        );
    }
    
    private MuToken parseIdentifier(int initial = -1) {
        StringBuilder sb = new();
        if(initial >= 0) sb.Append((char) initial);
        while(MuUtil.IsIdentifierChar(this.chPeek())) {
            sb.Append((char) this.chNext());
        }
        return MuToken.Identifier(
            this.getTokenSpan(),
            sb.ToString()
        );
    }
    
    private MuToken parseBracesIdentifier(MuSourceLocation braceEndLocation) {
        MuSourceLocation stringStartLocation = this.location;
        string text = this.parseStringLiteralText();
        MuSourceLocation stringEndLocation = this.location;
        this.skipWhitespace();
        if(this.chPeek() == '}') {
            this.chNext();
        }
        else {
            this.queuedToken = MuToken.String(
                stringStartLocation.SpanTo(stringEndLocation),
                text
            );
            return MuToken.BeginMembers(
                this.tokenStartLocation.SpanTo(braceEndLocation)
            );
        }
        return MuToken.Identifier(
            this.getTokenSpan(),
            text
        );
    }
    
    private MuToken parseStringLiteral(int initial) {
        return MuToken.String(
            this.getTokenSpan(),
            this.parseStringLiteralText(initial)
        );
    }
    
    private MuTextFormatSpecifier parseStringFormatSpecifier() {
        MuTextFormatSpecifier format = new (
            MuTextFormatSpecifierBlock.Keep,
            MuTextFormatSpecifierEnd.KeepAll
        );
        // Block format
        if(this.chPeek() == '=') {
            format.BlockLines = MuTextFormatSpecifierBlock.Keep;
            format.EndLines = MuTextFormatSpecifierEnd.KeepAll;
            this.chNext();
        }
        else if(this.chPeek() == '^') {
            format.BlockLines = MuTextFormatSpecifierBlock.Keep;
            format.EndLines = MuTextFormatSpecifierEnd.Strip;
            this.chNext();
        }
        else if(this.chPeek() == ';') {
            format.BlockLines = MuTextFormatSpecifierBlock.Strip;
            format.EndLines = MuTextFormatSpecifierEnd.Strip;
            this.chNext();
        }
        else if(this.chPeek() == '|') {
            format.BlockLines = MuTextFormatSpecifierBlock.Deindent;
            format.EndLines = MuTextFormatSpecifierEnd.KeepOneLine;
            this.chNext();
        }
        else if(this.chPeek() == '>') {
            format.BlockLines = MuTextFormatSpecifierBlock.Fold;
            format.EndLines = MuTextFormatSpecifierEnd.KeepOneLine;
            this.chNext();
        }
        // End format
        if(this.chPeek() == '+') {
            format.EndLines = MuTextFormatSpecifierEnd.KeepAllLines;
            this.chNext();
        }
        else if(this.chPeek() == '$') {
            format.EndLines = MuTextFormatSpecifierEnd.KeepOneLine;
            this.chNext();
        }
        else if(this.chPeek() == '*') {
            format.EndLines = MuTextFormatSpecifierEnd.KeepAll;
            this.chNext();
        }
        else if(this.chPeek() == '-') {
            format.EndLines = MuTextFormatSpecifierEnd.Strip;
            this.chNext();
        }
        // First line indentation
        while(this.chPeek() == '.') {
            format.FirstLineIndent++;
            this.chNext();
        }
        return format;
    }
    
    private string parseStringLiteralText(int initial = -1) {
        int chQuote = initial >= 0 ? initial : this.chNext();
        if(chQuote == '|') {
            int chFormatPeek = this.chPeek();
            if(chFormatPeek is ' ' or '\t' or '\r' or '\n') {
                return this.parseStringLiteralTextLine();
            }
            MuTextFormatSpecifier format = this.parseStringFormatSpecifier();
            this.skipWhitespace();
            chQuote = this.chPeek();
            if(!MuUtil.IsQuoteChar(chQuote)) {
                this.Errors.AddExpectedStringAfterFormatSpecifier(
                    this.tokenStartLocation
                );
                return null;
            }
            this.chNext();
            return format.ApplyFormat(
                this.parseStringLiteralTextBody(chQuote)
            );
        }
        if(MuUtil.IsQuoteChar(chQuote)) {
            return this.parseStringLiteralTextBody(chQuote);
        }
        return null;
    }
    
    private string parseStringLiteralTextLine() {
        if(this.chPeek() == '\n') {
            this.skipWhitespace();
            return "";
        }
        this.skipWhitespace();
        StringBuilder sb = new();
        int whitespaceRun = 0;
        while(true) {
            int ch = this.chNext();
            if(ch < 0 || ch == '\n') {
                break;
            }
            if(MuUtil.IsWhitespaceChar(ch)) {
                whitespaceRun++;
            }
            else {
                whitespaceRun = 0;
            }
            sb.Append((char) ch);
        }
        sb.Length -= whitespaceRun;
        return sb.ToString();
    }
    
    private string parseStringLiteralTextBody(int chQuote) {
        bool doubleQuote = this.chPeek() == chQuote;
        int fenceLength = 0;
        if(doubleQuote) {
            this.chNext();
            if(this.chPeek() == chQuote) {
                fenceLength = 3;
                this.chNext();
                while(this.chPeek() == chQuote) {
                    fenceLength++;
                    this.chNext();
                }
            }
            else {
                return "";
            }
        }
        if(fenceLength >= 3) {
            return this.parseStringLiteralTextFenced(chQuote, fenceLength);
        }
        else if(chQuote == '"') {
            return this.parseStringLiteralTextQuoted(chQuote);
        }
        else if(chQuote == '\'') {
            return this.parseStringLiteralTextQuoted(chQuote);
        }
        else if(chQuote == '`') {
            return this.parseStringLiteralTextBacktick();
        }
        return null;
    }
    
    private string parseStringLiteralTextFenced(int chQuote, int fenceLength) {
        StringBuilder sb = new();
        int chQuoteCount = 0;
        while(true) {
            int ch = this.chNext();
            if(ch < 0) {
                this.Errors.AddUnterminatedStringLiteral(this.tokenStartLocation);
                return null;
            }
            if(ch == chQuote) {
                chQuoteCount++;
                if(chQuoteCount >= fenceLength) {
                    break;
                }
                sb.Append((char) ch);
            }
            else {
                chQuoteCount = 0;
                sb.Append((char) ch);
            }
        }
        string text = sb.ToString()[..^(fenceLength - 1)];
        if(chQuote == '`') {
            return text;
        }
        else {
            (int badEscIndex, string escText) = MuUtil.UnescapeQuotedString(text);
            if(badEscIndex >= 0) {
                this.Errors.AddMalformedStringEscapeSequence(this.tokenStartLocation);
            }
            return escText;
        }
    }
    
    private string parseStringLiteralTextQuoted(int chQuote) {
        StringBuilder sb = new();
        bool escape = false;
        while(true) {
            int ch = this.chNext();
            if(ch < 0) {
                this.Errors.AddUnterminatedStringLiteral(this.tokenStartLocation);
                break;
            }
            if(escape) {
                escape = false;
                sb.Append((char) ch);
            }
            else if(ch == '\\') {
                escape = true;
                sb.Append((char) ch);
            }
            else if(ch == chQuote) {
                break;
            }
            else if(ch == '\n') {
                this.Errors.AddUnexpectedNewlineInStringLiteral(this.tokenStartLocation);
                break;
            }
            else {
                sb.Append((char) ch);
            }
        }
        string text = sb.ToString();
        (int badEscIndex, string escText) = MuUtil.UnescapeQuotedString(text);
        if(badEscIndex >= 0) {
            this.Errors.AddMalformedStringEscapeSequence(this.tokenStartLocation);
        }
        return escText;
    }
    
    private string parseStringLiteralTextBacktick() {
        StringBuilder sb = new();
        while(true) {
            int ch = this.chNext();
            if(ch < 0) {
                this.Errors.AddUnterminatedStringLiteral(this.tokenStartLocation);
                return null;
            }
            if(ch == '`') {
                if(this.chPeek() == '`') {
                    this.chNext();
                }
                else {
                    break;
                }
            }
            sb.Append((char) ch);
        }
        return sb.ToString();
    }
    
    private MuSourceSpan getTokenSpan() => (
        this.tokenStartLocation.SpanTo(this.location)
    );
    
    private void skipWhitespace() {
        while(MuUtil.IsWhitespaceChar(this.chPeek())) {
            this.chNext();
        }
    }
    
    private int chPeek() => this.reader.Peek();
    private int chNext() {
        int ch = this.reader.Read();
        this.location.Index++;
        if(ch == '\n') {
            this.location.LineNumber++;
            this.location.LineStartIndex = this.location.Index;
        }
        return ch;
    }
    
    /// <summary>
    /// Dispose of the underlying TextReader object.
    /// </summary>
    public void Dispose() {
        this.reader?.Dispose();
        GC.SuppressFinalize(this);
    }
}
