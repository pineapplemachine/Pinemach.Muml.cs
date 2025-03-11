using System;
using System.Collections.Generic;
using System.IO;

namespace Pinemach.Muml;

/// <summary>
/// Implements a parser which takes Muml source in and outputs a
/// corresponding MuDocument object.
/// </summary>
public class MuParser : IDisposable {
    public readonly MuDocument Document;
    public readonly bool KeepComments;
    
    public readonly MuSourceErrors Errors;
    public bool IsOk() => (this.Errors?.Count ?? 0) == 0;
    
    private readonly MuTokenizer tokenizer;
    
    public MuParser(string source, bool keepComments = false) :
        this(null, new StringReader(source), keepComments)
    {}
    public MuParser(TextReader reader, bool keepComments = false) :
        this(null, reader, keepComments)
    {}
    public MuParser(string fileName, string source, bool keepComments = false) :
        this(fileName, new StringReader(source), keepComments)
    {}
    public MuParser(string fileName, TextReader reader, bool keepComments = false) :
        this(new MuTokenizer(fileName, reader), keepComments)
    {}
    public MuParser(MuTokenizer tokenizer, bool keepComments = false) {
        this.tokenizer = tokenizer;
        this.Errors = tokenizer.Errors;
        this.KeepComments = keepComments;
        this.Document = new(this.Errors);
    }
    
    private readonly List<MuElement> elStack = new();
    private bool elHasTop() => (
        this.elStack.Count > 0
    );
    private MuElement elTop() => (
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
    private List<MuToken> beginMembersTokenStack = new();
    private bool isAfterAttributeName;
    private bool isAfterEquals;
    
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
        bool noInterruptErrorCoalsece = false;
        if(!token.IsValid()) {
            this.handleEof();
            return false;
        }
        else if(token.IsComment()) {
            if(!this.KeepComments) return true;
            this.elAddAfterTop(new MuElement(
                sourceSpan: token.Span,
                name: null,
                commentType: token.GetCommentType(),
                text: token.Text
            ));
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
                noInterruptErrorCoalsece = true;
            }
            this.isAfterEquals = true;
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
            if(this.isAfterEquals && (
                this.elTop() == null ||
                this.elTop().Attributes.Count <= 0 ||
                this.elTop().Attributes[^1].Equals(null, null)
            )) {
                this.addErrorCoalesce(MuSourceError.UnexpectedCloseBracket(token.Location));
                noInterruptErrorCoalsece = true;
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
        if(!noInterruptErrorCoalsece) {
            this.interruptErrorCoalsece();
        }
        return true;
    }
    
    private void handleString(bool isIdentifier, MuToken token) {
        MuElement el = this.elTop();
        if(this.inAttributes) {
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
            if(el != null) {
                el.Values.Add(token.Text);
            }
            else {
                this.Document.Values.Add(token.Text);
            }
        }
        else if(!isIdentifier) {
            if(el != null) {
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
                    name: token.Text
                ));
                this.isAfterBeginMembers = false;
            }
            else {
                this.elAddNext(new MuElement(
                    sourceSpan: token.Span,
                    name: token.Text
                ));
            }
        }
    }
    
    private static string appendText(string mainText, string addText) {
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
        if(this.isAfterEquals && this.elTop() is {} el) {
            el.Values.Add(null);
        }
        this.isAfterEquals = false;
    }
    
    private void handleEof() {
        if(this.inAttributes) {
            this.Errors.AddUnterminatedAttributes(this.inAttributesToken.Location);
        }
        else {
            this.handleLeaveNeutral();
        }
        int lastBeginMembersIndex = -1;
        foreach(MuToken beginMembersToken in this.beginMembersTokenStack) {
            if(
                lastBeginMembersIndex < 0 ||
                beginMembersToken.Location.Index > lastBeginMembersIndex + 1
            ) {
                this.Errors.AddUnterminatedMembers(beginMembersToken.Location);
            }
            lastBeginMembersIndex = beginMembersToken.Location.Index;
        }
        if(this.Errors.Count < 4096) {
            this.Errors.Sort();
        }
    }
    
    /// <summary>
    /// Dispose of the tokenizer's underlying TextReader object.
    /// </summary>
    public void Dispose() {
        this.tokenizer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
