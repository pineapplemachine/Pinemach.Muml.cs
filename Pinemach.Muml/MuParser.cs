using System;
using System.Collections.Generic;
using System.IO;

namespace Pinemach.Muml;

/// <summary>
/// Implements a parser which takes Muml source in and outputs a
/// corresponding MuDocument object.
/// </summary>
public class MuParser : IDisposable {
    /// <summary>
    /// Document object constructed from the parsed Muml input.
    /// </summary>
    public readonly MuDocument Document;
    
    /// <summary>
    /// Error log object, to which parsing errors are added.
    /// </summary>
    public readonly MuSourceErrors Errors;
    
    /// <summary>
    /// Returns true when the parser has no errors associated with it.
    /// </summary>
    public bool IsOk() => (this.Errors.Count == 0);
    
    private readonly MuTokenizer tokenizer;
    
    public MuParser(string source) :
        this(null, new StringReader(source))
    {}
    public MuParser(TextReader reader) :
        this(null, reader)
    {}
    public MuParser(string? fileName, string source) :
        this(fileName, new StringReader(source))
    {}
    public MuParser(string? fileName, TextReader reader) :
        this(new MuTokenizer(fileName, reader))
    {}
    public MuParser(MuTokenizer tokenizer) {
        this.tokenizer = tokenizer;
        this.Errors = tokenizer.Errors;
        this.Document = new(this.Errors);
    }
    
    private readonly List<MuElement> elStack = new();
    private bool elHasTop() => (
        this.elStack.Count > 0
    );
    private MuElement? elTop() => (
        this.elStack.Count > 0 ? this.elStack[^1] : null
    );
    private void elAddAfterTop(MuElement el) {
        if(this.elStack.Count <= 1) {
            this.Document.Members.Add(el);
        }
        else {
            this.elStack[^2].Members.Add(el);
        }
    }
    private void elAddNext(MuElement el) {
        this.elAddAfterTop(el);
        if(this.elStack.Count <= 0) {
            this.elStack.Add(el);
        }
        else {
            this.elStack[^1] = el;
        }
    }
    private void elAddMember(MuElement el) {
        if(this.elStack.Count <= 0) {
            this.Document.Members.Add(el);
        }
        else {
            this.elStack[^1].Members.Add(el);
        }
        this.elStack.Add(el);
    }
    private void elPop() {
        if(this.elStack.Count >= 0) {
            this.elStack.RemoveAt(this.elStack.Count - 1);
        }
    }
    
    private bool inAttributes;
    private MuToken inAttributesToken;
    private bool isAfterBeginMembers;
    private readonly List<MuToken> beginMembersTokenStack = new();
    private bool isAfterAttributeName;
    private bool isAfterEquals;
    private MuToken isAfterEqualsToken;
    
    private MuSourceError lastError;
    private void interruptErrorCoalsece() {
        this.lastError = MuSourceError.None;
    }
    private void addErrorCoalesce(MuSourceError error) {
        if(error.Type != this.lastError.Type) {
            this.Errors.Add(error);
            this.lastError = error;
        }
    }
    
    /// <summary>
    /// Parse tokens in the source text until finished.
    /// </summary>
    public void Parse() {
        while(this.ParseNextToken()) {}
    }
    
    /// <summary>
    /// Parse and handle the immediate next token in the source text.
    /// </summary>
    public bool ParseNextToken() {
        MuToken token = this.tokenizer.NextToken();
        bool noInterruptErrorCoalesce = false;
        if(!token.IsValid()) {
            this.handleEof();
            return false;
        }
        else if(token.IsComment()) {
            return true;
        }
        else if(token.IsIdentifier()) {
            this.handleString(true, token);
        }
        else if(token.IsString()) {
            if(this.isAfterBeginMembers) {
                this.Errors.AddUnexpectedStringLiteral(token.Location);
                return true;
            }
            this.handleString(false, token);
        }
        else if(token.IsEquals()) {
            if(this.isAfterEquals || this.isAfterBeginMembers) {
                this.addErrorCoalesce(MuSourceError.UnexpectedEquals(token.Location));
                noInterruptErrorCoalesce = true;
            }
            this.isAfterEquals = true;
            this.isAfterEqualsToken = token;
            if(this.inAttributes && !this.isAfterAttributeName && this.elTop() is {} el) {
                el.Attributes.Add(null, null);
            }
            this.isAfterAttributeName = false;
        }
        else if(token.IsBeginAttributes()) {
            if(this.inAttributes || this.isAfterBeginMembers || !this.elHasTop()) {
                this.addErrorCoalesce(MuSourceError.UnexpectedOpenBracket(token.Location));
                return true;
            }
            this.handleLeaveNeutral();
            this.inAttributes = true;
            this.isAfterAttributeName = false;
            this.inAttributesToken = token;
        }
        else if(token.IsEndAttributes()) {
            if(!this.inAttributes || this.isAfterBeginMembers) {
                this.addErrorCoalesce(MuSourceError.UnexpectedCloseBracket(token.Location));
                return true;
            }
            MuElement? el = this.elTop();
            if(this.isAfterEquals && (
                el is null ||
                el.Attributes.Count <= 0 ||
                el.Attributes[^1].Equals(null, null)
            )) {
                this.addErrorCoalesce(MuSourceError.UnexpectedCloseBracket(token.Location));
                noInterruptErrorCoalesce = true;
            }
            this.inAttributes = false;
            this.isAfterEquals = false;
        }
        else if(token.IsBeginMembers()) {
            if(this.inAttributes || this.isAfterBeginMembers || !this.elHasTop()) {
                this.addErrorCoalesce(MuSourceError.UnexpectedOpenBrace(token.Location));
                return true;
            }
            this.handleLeaveNeutral();
            this.isAfterBeginMembers = true;
            this.beginMembersTokenStack.Add(token);
        }
        else if(token.IsEndMembers()) {
            if(this.inAttributes || !this.elHasTop()) {
                this.addErrorCoalesce(MuSourceError.UnexpectedCloseBrace(token.Location));
                return true;
            }
            this.handleLeaveNeutral();
            if(!this.isAfterBeginMembers) {
                this.elPop();
            }
            if(this.beginMembersTokenStack.Count > 0) {
                this.beginMembersTokenStack.RemoveAt(this.beginMembersTokenStack.Count - 1);
            }
            this.isAfterBeginMembers = false;
        }
        if(!noInterruptErrorCoalesce) {
            this.interruptErrorCoalsece();
        }
        return true;
    }
    
    private void handleString(bool isIdentifier, MuToken token) {
        MuElement? el = this.elTop();
        if(this.inAttributes && el is not null) {
            if(this.isAfterEquals) {
                this.isAfterEquals = false;
                this.isAfterAttributeName = false;
                if(el.Attributes.Count == 0) {
                    el.Attributes.Add(null, token.Text);
                }
                else {
                    MuAttribute lastAttr = el.Attributes[^1];
                    el.Attributes[^1] = new MuAttribute(lastAttr.Name, token.Text);
                }
            }
            else {
                el.Attributes.Add(token.Text, null);
                this.isAfterAttributeName = true;
            }
        }
        else if(this.isAfterEquals) {
            this.isAfterEquals = false;
            if(token.Text is not null) {
                if(el is not null) {
                    el.Values.Add(token.Text);
                }
                else {
                    this.Document.Values.Add(token.Text);
                }
            }
        }
        else if(!isIdentifier) {
            if(el is not null) {
                el.Text = MuParser.appendText(el.Text, token.Text);
            }
            else {
                this.Document.Text = MuParser.appendText(this.Document.Text, token.Text);
            }
        }
        else {
            if(this.isAfterBeginMembers) {
                this.elAddMember(new MuElement(
                    sourceSpan: token.Span,
                    name: token.Text ?? ""
                ));
                this.isAfterBeginMembers = false;
            }
            else {
                this.elAddNext(new MuElement(
                    sourceSpan: token.Span,
                    name: token.Text ?? ""
                ));
            }
        }
    }
    
    private static string? appendText(string? mainText, string? addText) {
        if(string.IsNullOrEmpty(addText)) {
            return string.IsNullOrEmpty(mainText) ? mainText ?? addText : mainText + '\n';
        }
        else if(string.IsNullOrEmpty(mainText)) {
            return addText;
        }
        else if(MuUtil.IsWhitespaceChar(mainText[^1]) || MuUtil.IsWhitespaceChar(addText[0])) {
            return mainText + addText;
        }
        else {
            return mainText + ' ' + addText;
        }
    }
    
    private void handleLeaveNeutral() {
        if(this.isAfterEquals) {
            this.Errors.AddExpectedValueAfterEquals(this.isAfterEqualsToken.Location);
            this.isAfterEquals = false;
        }
    }
    
    private void handleEof() {
        // Handle being mid-attributes
        if(this.inAttributes) {
            this.Errors.AddUnterminatedAttributes(this.inAttributesToken.Location);
        }
        else {
            this.handleLeaveNeutral();
        }
        // Handle being mid-members
        foreach(MuToken beginMembersToken in this.beginMembersTokenStack) {
            this.Errors.AddUnterminatedMembers(beginMembersToken.Location);
        }
        // Sort errors by source pos, provided there isn't an extreme number
        if(this.Errors.Count < 4096) {
            this.Errors.Sort();
        }
    }
    
    /// <summary>
    /// Dispose of the tokenizer's underlying TextReader object.
    /// </summary>
    public void Dispose() {
        this.tokenizer.Dispose();
        GC.SuppressFinalize(this);
    }
}
