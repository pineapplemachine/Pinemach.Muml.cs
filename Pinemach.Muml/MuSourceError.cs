using System;
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
    ExpectedValueAfterEquals,
}

/// <summary>
/// Represents an error encountered while parsing Muml source.
/// </summary>
public readonly struct MuSourceError : IComparable {
    public static readonly MuSourceError None = new(MuSourceErrorType.None, MuSourceSpan.None);
    
    public readonly MuSourceErrorType Type;
    public readonly MuSourceSpan Span;
    
    public readonly MuSourceLocation Location { get => this.Span.Start; }
    
    public MuSourceError(MuSourceErrorType type, MuSourceLocation location) : this(
        type,
        new MuSourceSpan(location)
    ) {}
    public MuSourceError(MuSourceErrorType type, MuSourceSpan span) {
        this.Type = type;
        this.Span = span;
    }
    
    public static MuSourceError UnexpectedCharacter(MuSourceSpan span) => new(MuSourceErrorType.UnexpectedCharacter, span);
    public static MuSourceError UnexpectedCharacter(MuSourceLocation loc) => new(MuSourceErrorType.UnexpectedCharacter, loc);
    public static MuSourceError UnexpectedOpenBracket(MuSourceSpan span) => new(MuSourceErrorType.UnexpectedOpenBracket, span);
    public static MuSourceError UnexpectedOpenBracket(MuSourceLocation loc) => new(MuSourceErrorType.UnexpectedOpenBracket, loc);
    public static MuSourceError UnexpectedOpenBrace(MuSourceSpan span) => new(MuSourceErrorType.UnexpectedOpenBrace, span);
    public static MuSourceError UnexpectedOpenBrace(MuSourceLocation loc) => new(MuSourceErrorType.UnexpectedOpenBrace, loc);
    public static MuSourceError UnexpectedCloseBracket(MuSourceSpan span) => new(MuSourceErrorType.UnexpectedCloseBracket, span);
    public static MuSourceError UnexpectedCloseBracket(MuSourceLocation loc) => new(MuSourceErrorType.UnexpectedCloseBracket, loc);
    public static MuSourceError UnexpectedCloseBrace(MuSourceSpan span) => new(MuSourceErrorType.UnexpectedCloseBrace, span);
    public static MuSourceError UnexpectedCloseBrace(MuSourceLocation loc) => new(MuSourceErrorType.UnexpectedCloseBrace, loc);
    public static MuSourceError UnexpectedEquals(MuSourceSpan span) => new(MuSourceErrorType.UnexpectedEquals, span);
    public static MuSourceError UnexpectedEquals(MuSourceLocation loc) => new(MuSourceErrorType.UnexpectedEquals, loc);
    public static MuSourceError UnexpectedStringLiteral(MuSourceSpan span) => new(MuSourceErrorType.UnexpectedStringLiteral, span);
    public static MuSourceError UnexpectedStringLiteral(MuSourceLocation loc) => new(MuSourceErrorType.UnexpectedStringLiteral, loc);
    public static MuSourceError UnexpectedNewlineInStringLiteral(MuSourceSpan span) => new(MuSourceErrorType.UnexpectedNewlineInStringLiteral, span);
    public static MuSourceError UnexpectedNewlineInStringLiteral(MuSourceLocation loc) => new(MuSourceErrorType.UnexpectedNewlineInStringLiteral, loc);
    public static MuSourceError UnterminatedStringLiteral(MuSourceSpan span) => new(MuSourceErrorType.UnterminatedStringLiteral, span);
    public static MuSourceError UnterminatedStringLiteral(MuSourceLocation loc) => new(MuSourceErrorType.UnterminatedStringLiteral, loc);
    public static MuSourceError UnterminatedNestedBlockComment(MuSourceSpan span) => new(MuSourceErrorType.UnterminatedNestedBlockComment, span);
    public static MuSourceError UnterminatedNestedBlockComment(MuSourceLocation loc) => new(MuSourceErrorType.UnterminatedNestedBlockComment, loc);
    public static MuSourceError UnterminatedAttributes(MuSourceSpan span) => new(MuSourceErrorType.UnterminatedAttributes, span);
    public static MuSourceError UnterminatedAttributes(MuSourceLocation loc) => new(MuSourceErrorType.UnterminatedAttributes, loc);
    public static MuSourceError UnterminatedMembers(MuSourceSpan span) => new(MuSourceErrorType.UnterminatedMembers, span);
    public static MuSourceError UnterminatedMembers(MuSourceLocation loc) => new(MuSourceErrorType.UnterminatedMembers, loc);
    public static MuSourceError MalformedBracesIdentifier(MuSourceSpan span) => new(MuSourceErrorType.MalformedBracesIdentifier, span);
    public static MuSourceError MalformedBracesIdentifier(MuSourceLocation loc) => new(MuSourceErrorType.MalformedBracesIdentifier, loc);
    public static MuSourceError MalformedStringEscapeSequence(MuSourceSpan span) => new(MuSourceErrorType.MalformedStringEscapeSequence, span);
    public static MuSourceError MalformedStringEscapeSequence(MuSourceLocation loc) => new(MuSourceErrorType.MalformedStringEscapeSequence, loc);
    public static MuSourceError ExpectedStringAfterFormatSpecifier(MuSourceSpan span) => new(MuSourceErrorType.ExpectedStringAfterFormatSpecifier, span);
    public static MuSourceError ExpectedStringAfterFormatSpecifier(MuSourceLocation loc) => new(MuSourceErrorType.ExpectedStringAfterFormatSpecifier, loc);
    public static MuSourceError ExpectedValueAfterEquals(MuSourceSpan span) => new(MuSourceErrorType.ExpectedValueAfterEquals, span);
    public static MuSourceError ExpectedValueAfterEquals(MuSourceLocation loc) => new(MuSourceErrorType.ExpectedValueAfterEquals, loc);
    
    public override string ToString() => (
        $"{MuSourceError.TypeToString(this.Type)} {this.Span}"
    );

    public int CompareTo(object? obj) => (
        obj is MuSourceError error ?
        this.CompareTo(error) :
        throw new ArgumentException(null, nameof(obj))
    );
    public int CompareTo(MuSourceError error) {
        if(this.Location.Index != error.Location.Index) {
            return this.Location.Index - error.Location.Index;
        }
        else {
            return this.Type - error.Type;
        }
    }

    /// <summary>
    /// Get a string representation of a MuSourceErrorType.
    /// </summary>
    public static string? TypeToString(MuSourceErrorType type) => type switch {
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
        MuSourceErrorType.ExpectedValueAfterEquals => "ExpectedValueAfterEquals",
        _ => null,
    };
}

/// <summary>
/// Represents the list of errors encountered while parsing Muml source.
/// </summary>
public class MuSourceErrors : List<MuSourceError> {
    public MuSourceErrors() {}
    public MuSourceErrors(int capacity) : base(capacity) {}
    public MuSourceErrors(IEnumerable<MuSourceError> collection) : base(collection) {}
    
    public static MuSourceErrors From(IEnumerable<MuSourceError>? errors) => (
        errors is MuSourceErrors list ? list :
        errors is not null ? new(errors) :
        new()
    );
    
    public bool IsLastErrorType(MuSourceErrorType type) => (
        this.Count > 0 && this[^1].Type == type
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
    public void AddExpectedValueAfterEquals(MuSourceSpan span) => this.Add(MuSourceErrorType.ExpectedValueAfterEquals, span);
    public void AddExpectedValueAfterEquals(MuSourceLocation loc) => this.Add(MuSourceErrorType.ExpectedValueAfterEquals, loc);
    
    public override string ToString() => string.Join("\n", this);
}
