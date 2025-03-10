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
    
    public readonly MuSourceErrors Errors = new();
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
    private MuToken isAfterBeginMembersToken;
    private bool isAfterEquals;
    
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
                this.Errors.AddUnexpectedStringLiteral(token.Span);
                return true;
            }
            this.handleString(false, token);
        }
        else if(token.IsEquals()) {
            if(this.isAfterEquals || this.isAfterBeginMembers) {
                this.Errors.AddUnexpectedEquals(token.Span);
            }
            this.isAfterEquals = true;
            if(this.inAttributes && this.elTop() is {} el) {
                el.Attributes.Add(null, null);
            }
        }
        else if(token.IsBeginAttributes()) {
            if(this.inAttributes || this.isAfterBeginMembers || !this.elHasTop()) {
                this.Errors.AddUnexpectedOpenBracket(token.Span);
                return true;
            }
            this.handleLeaveNeutral();
            this.inAttributes = true;
            this.inAttributesToken = token;
        }
        else if(token.IsEndAttributes()) {
            if(!this.inAttributes || this.isAfterBeginMembers) {
                this.Errors.AddUnexpectedCloseBracket(token.Span);
                return true;
            }
            this.inAttributes = false;
            this.isAfterEquals = false;
        }
        else if(token.IsBeginMembers()) {
            if(this.inAttributes || this.isAfterBeginMembers || !this.elHasTop()) {
                this.Errors.AddUnexpectedOpenBrace(token.Span);
                return true;
            }
            this.handleLeaveNeutral();
            this.isAfterBeginMembers = true;
            this.isAfterBeginMembersToken = token;
        }
        else if(token.IsEndMembers()) {
            if(this.inAttributes || !this.elHasTop()) {
                this.Errors.AddUnexpectedCloseBrace(token.Span);
                return true;
            }
            this.handleLeaveNeutral();
            if(!this.isAfterBeginMembers) {
                this.elPop();
            }
            this.isAfterBeginMembers = false;
        }
        return true;
    }
    
    private void handleString(bool isIdentifier, MuToken token) {
        MuElement el = this.elTop();
        if(this.inAttributes) {
            if(this.isAfterEquals) {
                this.isAfterEquals = false;
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
            return string.IsNullOrEmpty(mainText) ? mainText : mainText + '\n';
        }
        else if(string.IsNullOrEmpty(mainText)) {
            return addText;
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
            this.Errors.AddUnterminatedAttributes(this.inAttributesToken.Span);
        }
        else {
            this.handleLeaveNeutral();
        }
        if(this.elStack.Count > 1) {
            this.Errors.AddUnterminatedMembers(this.isAfterBeginMembersToken.Span);
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
