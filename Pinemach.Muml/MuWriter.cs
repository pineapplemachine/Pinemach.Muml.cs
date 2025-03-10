using System;
using System.Collections.Generic;
using System.IO;

namespace Pinemach.Muml;

/// <summary>
/// Implements functions for serializing Muml objects to strings.
/// </summary>
public class MuWriter {
    public static readonly MuWriter Default = new();
    public static readonly MuWriter Condensed = new(null, " ");
    public static readonly MuWriter Minimized = new(null, null, reduceSpaces: true);
    
    public const string DefaultIndent = "  ";
    public const string DefaultNewline = "\n";
    
    public string Indent;
    public string Newline;
    public MuTextType PreferHeaderType;
    public MuTextType PreferTagType;
    public MuTextType PreferValueType;
    public MuTextType PreferTextType;
    public MuTextType PreferAttributeNameType;
    public MuTextType PreferAttributeValueType;
    public bool ReduceSpaces;
    
    public MuWriter(
        string indent = MuWriter.DefaultIndent,
        string newline = MuWriter.DefaultIndent,
        MuTextType preferHeaderType = MuTextType.Auto,
        MuTextType preferTagType = MuTextType.Auto,
        MuTextType preferValueType = MuTextType.Auto,
        MuTextType preferTextType = MuTextType.Auto,
        MuTextType preferAttributeNameType = MuTextType.Auto,
        MuTextType preferAttributeValueType = MuTextType.Auto,
        bool reduceSpaces = false
    ) {
        this.Indent = indent;
        this.Newline = newline;
        this.PreferHeaderType = preferHeaderType;
        this.PreferTagType = preferTagType;
        this.PreferValueType = preferValueType;
        this.PreferTextType = preferTextType;
        this.PreferAttributeNameType = preferAttributeNameType;
        this.PreferAttributeValueType = preferAttributeValueType;
        this.ReduceSpaces = reduceSpaces;
    }
    
    public string WriteDocument(MuDocument doc) {
        StringWriter writer = new();
        this.WriteDocument(doc, writer);
        return writer.ToString();
    }
    public string WriteElement(MuElement el, string indent = null) {
        StringWriter writer = new();
        this.WriteElement(el, indent, writer);
        return writer.ToString();
    }
    public string WriteMembers(IEnumerable<MuElement> members, string indent = null) {
        StringWriter writer = new();
        this.WriteMembers(members, indent, writer);
        return writer.ToString();
    }
    public string WriteAttributes(IEnumerable<MuAttribute> members) {
        StringWriter writer = new();
        this.WriteAttributes(members, writer);
        return writer.ToString();
    }
    public string WriteAttribute(MuAttribute attr) {
        StringWriter writer = new();
        this.WriteAttribute(attr, writer);
        return writer.ToString();
    }
    public string WriteValues(IEnumerable<string> values) {
        StringWriter writer = new();
        this.WriteValues(values, writer, false);
        return writer.ToString();
    }
    
    public void WriteDocument(MuDocument doc, TextWriter writer) {
        if(doc.HasText()) {
            writer.Write(MuUtil.ToQuotedString(doc.Text, this.PreferTextType));
            writer.Write(this.Newline);
        }
        if(doc.HasValues()) {
            this.WriteValues(doc.Values, writer, true);
        }
        if(doc.HasMembers()) {
            this.WriteMembers(doc.Members, null, writer);
        }
    }
    
    public void WriteElement(MuElement el, string indent, TextWriter writer) {
        if(el.CommentType == MuCommentType.Line) {
            writer.Write('#');
            writer.Write(el.Text);
            writer.Write('\n');
        }
        else if(el.CommentType == MuCommentType.Fenced) {
            int fenceLength = (
                Math.Max(3, 1 + MuUtil.CountMaxConsecutiveChars(el.Text, '#'))
            );
            for(int i = 0; i < fenceLength; i++) writer.Write('#');
            writer.Write(el.Text);
            for(int i = 0; i < fenceLength; i++) writer.Write('#');
        }
        else if(el.CommentType == MuCommentType.NestedBlock) {
            writer.Write("#[");
            writer.Write(el.Text);
            writer.Write("#]");
        }
        string name = MuUtil.ToIdentifierString(el.Name, this.PreferTagType);
        if(el.HasIdentifierName()) {
            writer.Write(name);
        }
        else {
            writer.Write('{');
            writer.Write(name);
            writer.Write('}');
        }
        if(el.HasValues()) {
            this.WriteValues(el.Values, writer, false);
        }
        if(el.HasText()) {
            if(!this.ReduceSpaces) writer.Write(' ');
            writer.Write(MuUtil.ToQuotedString(el.Text, this.PreferTextType));
        }
        if(el.HasAttributes()) {
            if(!this.ReduceSpaces) writer.Write(' ');
            this.WriteAttributes(el.Attributes, writer);
        }
        if(el.HasMembers()) {
            if(!this.ReduceSpaces) writer.Write(' ');
            this.WriteMembers(el.Members, indent, writer);
        }
    }
    
    public void WriteMembers(IEnumerable<MuElement> members, string indent, TextWriter writer) {
        if(members == null) {
            writer.Write("{}");
            return;
        }
        writer.Write('{');
        bool first = true;
        foreach(MuElement member in members) {
            if(first) {
                writer.Write(this.Newline);
                first = false;
            }
            writer.Write(indent);
            this.WriteElement(member, indent + this.Indent, writer);
        }
        writer.Write(this.Newline);
        writer.Write(indent);
        writer.Write('}');
    }
    
    public void WriteAttributes(IEnumerable<MuAttribute> attrs, TextWriter writer) {
        if(attrs == null) {
            writer.Write("[]");
            return;
        }
        writer.Write('[');
        bool first = true;
        foreach(MuAttribute attr in attrs) {
            if(!first) {
                writer.Write(' ');
            }
            first = false;
            this.WriteAttribute(attr, writer);
        }
        writer.Write(']');
    }
    
    public void WriteAttribute(MuAttribute attr, TextWriter writer) {
        writer.Write(MuUtil.ToIdentifierString(attr.Name, this.PreferAttributeNameType));
        if(!string.IsNullOrEmpty(attr.Value)) {
            writer.Write('=');
            writer.Write(MuUtil.ToIdentifierString(attr.Value, this.PreferAttributeValueType));
        }
    }
    
    public void WriteValues(IEnumerable<string> values, TextWriter writer, bool lineSep) {
        foreach(string value in values) {
            writer.Write('=');
            writer.Write(MuUtil.ToIdentifierString(value, this.PreferValueType));
            if(lineSep) {
                writer.Write(this.Newline);
            }
            else if(!this.ReduceSpaces) {
                writer.Write(' ');
            }
        }
    }
}
