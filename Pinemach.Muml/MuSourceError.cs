using System.Collections.Generic;

namespace Pinemach.Muml;

/// <summary>
/// Enumeration of possible errors that may occur when dealing with Muml
/// source text.
/// </summary>
public enum MuSourceErrorType {
    None = 0,
    UnexpectedCharacter,
    UnexpectedOpenBracket,
    UnexpectedOpenBrace,
    UnexpectedCloseBracket,
    UnexpectedCloseBrace,
    UnexpectedEquals,
    UnexpectedStringLiteral,
    UnexpectedNewlineInStringLiteral,
    UnterminatedStringLiteral,
    UnterminatedNestedBlockComment,
    UnterminatedAttributes,
    UnterminatedMembers,
    MalformedBracesIdentifier,
    MalformedStringEscapeSequence,
    ExpectedStringAfterFormatSpecifier,
}

/// <summary>
/// Represents an error encountered while parsing Muml source.
/// </summary>
public readonly struct MuSourceError {
    public readonly MuSourceErrorType Type;
    public readonly MuSourceSpan Span;
    
    public MuSourceError(MuSourceErrorType type, MuSourceLocation location) : this(
        type,
        new MuSourceSpan(location)
    ) {}
    
    public MuSourceError(MuSourceErrorType type, MuSourceSpan span) {
        this.Type = type;
        this.Span = span;
    }
    
    public override string ToString() => (
        $"{MuSourceError.TypeToString(this.Type)} ({this.Span})"
    );
    
    /// <summary>
    /// Get a string representation of a MuSourceErrorType.
    /// </summary>
    public static string TypeToString(MuSourceErrorType type) => type switch {
        MuSourceErrorType.UnexpectedCharacter => "UnexpectedCharacter",
        MuSourceErrorType.UnexpectedOpenBracket => "UnexpectedOpenBracket",
        MuSourceErrorType.UnexpectedOpenBrace => "UnexpectedOpenBrace",
        MuSourceErrorType.UnexpectedCloseBracket => "UnexpectedCloseBracket",
        MuSourceErrorType.UnexpectedCloseBrace => "UnexpectedCloseBrace",
        MuSourceErrorType.UnexpectedEquals => "UnexpectedEquals",
        MuSourceErrorType.UnexpectedStringLiteral => "UnexpectedStringLiteral",
        MuSourceErrorType.UnexpectedNewlineInStringLiteral => "UnexpectedNewlineInStringLiteral",
        MuSourceErrorType.UnterminatedStringLiteral => "UnterminatedStringLiteral",
        MuSourceErrorType.UnterminatedNestedBlockComment => "UnterminatedNestedBlockComment",
        MuSourceErrorType.UnterminatedAttributes => "UnterminatedAttributes",
        MuSourceErrorType.UnterminatedMembers => "UnterminatedMembers",
        MuSourceErrorType.MalformedBracesIdentifier => "MalformedBracesIdentifier",
        MuSourceErrorType.MalformedStringEscapeSequence => "MalformedStringEscapeSequence",
        MuSourceErrorType.ExpectedStringAfterFormatSpecifier => "ExpectedStringAfterFormatSpecifier",
        _ => null,
    };
}

/// <summary>
/// Represents the list of errors encountered while parsing Muml source.
/// </summary>
public class MuSourceErrors : List<MuSourceError> {
    public MuSourceErrors() : base() {}
    public MuSourceErrors(int capacity) : base(capacity) {}
    public MuSourceErrors(IEnumerable<MuSourceError> collection) : base(collection) {}
    
    public static MuSourceErrors From(IEnumerable<MuSourceError> errors) => (
        errors is MuSourceErrors list ? list :
        errors != null ? new(errors) :
        new()
    );
    
    public void Add(MuSourceErrorType type, MuSourceLocation location) {
        this.Add(new MuSourceError(type, location));
    }
    public void Add(MuSourceErrorType type, MuSourceSpan span) {
        this.Add(new MuSourceError(type, span));
    }
    
    public void AddUnexpectedCharacter(MuSourceSpan span) => this.Add(MuSourceErrorType.UnexpectedCharacter, span);
    public void AddUnexpectedCharacter(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnexpectedCharacter, loc);
    public void AddUnexpectedOpenBracket(MuSourceSpan span) => this.Add(MuSourceErrorType.UnexpectedOpenBracket, span);
    public void AddUnexpectedOpenBracket(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnexpectedOpenBracket, loc);
    public void AddUnexpectedOpenBrace(MuSourceSpan span) => this.Add(MuSourceErrorType.UnexpectedOpenBrace, span);
    public void AddUnexpectedOpenBrace(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnexpectedOpenBrace, loc);
    public void AddUnexpectedCloseBracket(MuSourceSpan span) => this.Add(MuSourceErrorType.UnexpectedCloseBracket, span);
    public void AddUnexpectedCloseBracket(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnexpectedCloseBracket, loc);
    public void AddUnexpectedCloseBrace(MuSourceSpan span) => this.Add(MuSourceErrorType.UnexpectedCloseBrace, span);
    public void AddUnexpectedCloseBrace(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnexpectedCloseBrace, loc);
    public void AddUnexpectedEquals(MuSourceSpan span) => this.Add(MuSourceErrorType.UnexpectedEquals, span);
    public void AddUnexpectedEquals(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnexpectedEquals, loc);
    public void AddUnexpectedStringLiteral(MuSourceSpan span) => this.Add(MuSourceErrorType.UnexpectedStringLiteral, span);
    public void AddUnexpectedStringLiteral(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnexpectedStringLiteral, loc);
    public void AddUnexpectedNewlineInStringLiteral(MuSourceSpan span) => this.Add(MuSourceErrorType.UnexpectedNewlineInStringLiteral, span);
    public void AddUnexpectedNewlineInStringLiteral(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnexpectedNewlineInStringLiteral, loc);
    public void AddUnterminatedStringLiteral(MuSourceSpan span) => this.Add(MuSourceErrorType.UnterminatedStringLiteral, span);
    public void AddUnterminatedStringLiteral(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnterminatedStringLiteral, loc);
    public void AddUnterminatedNestedBlockComment(MuSourceSpan span) => this.Add(MuSourceErrorType.UnterminatedNestedBlockComment, span);
    public void AddUnterminatedNestedBlockComment(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnterminatedNestedBlockComment, loc);
    public void AddUnterminatedAttributes(MuSourceSpan span) => this.Add(MuSourceErrorType.UnterminatedAttributes, span);
    public void AddUnterminatedAttributes(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnterminatedAttributes, loc);
    public void AddUnterminatedMembers(MuSourceSpan span) => this.Add(MuSourceErrorType.UnterminatedMembers, span);
    public void AddUnterminatedMembers(MuSourceLocation loc) => this.Add(MuSourceErrorType.UnterminatedMembers, loc);
    public void AddMalformedBracesIdentifier(MuSourceSpan span) => this.Add(MuSourceErrorType.MalformedBracesIdentifier, span);
    public void AddMalformedBracesIdentifier(MuSourceLocation loc) => this.Add(MuSourceErrorType.MalformedBracesIdentifier, loc);
    public void AddMalformedStringEscapeSequence(MuSourceSpan span) => this.Add(MuSourceErrorType.MalformedStringEscapeSequence, span);
    public void AddMalformedStringEscapeSequence(MuSourceLocation loc) => this.Add(MuSourceErrorType.MalformedStringEscapeSequence, loc);
    public void AddExpectedStringAfterFormatSpecifier(MuSourceSpan span) => this.Add(MuSourceErrorType.ExpectedStringAfterFormatSpecifier, span);
    public void AddExpectedStringAfterFormatSpecifier(MuSourceLocation loc) => this.Add(MuSourceErrorType.ExpectedStringAfterFormatSpecifier, loc);
    
    public override string ToString() => string.Join("\n", this);
}
