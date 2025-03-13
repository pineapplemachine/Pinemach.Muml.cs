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
    
    public string? Indent;
    public string? Newline;
    public MuTextType PreferHeaderType;
    public MuTextType PreferTagType;
    public MuTextType PreferValueType;
    public MuTextType PreferTextType;
    public MuTextType PreferAttributeNameType;
    public MuTextType PreferAttributeValueType;
    public bool ReduceSpaces;
    
    public MuWriter() : this(MuWriter.DefaultIndent, MuWriter.DefaultNewline) {}
    public MuWriter(string? indent) : this(indent, MuWriter.DefaultNewline) {}
    public MuWriter(string? indent, string? newline, bool reduceSpaces = false) :
        this(indent, newline, MuTextType.Default, reduceSpaces: reduceSpaces)
    {}
    
    public MuWriter(
        string? indent,
        string? newline,
        MuTextType preferTextType,
        bool reduceSpaces = false
    ) : this(
        indent,
        newline,
        preferHeaderType: preferTextType,
        preferTagType: preferTextType,
        preferValueType: preferTextType,
        preferTextType: preferTextType,
        preferAttributeNameType: preferTextType,
        preferAttributeValueType: preferTextType,
        reduceSpaces: reduceSpaces
    ) {}
    
    public MuWriter(
        string? indent,
        string? newline,
        MuTextType preferHeaderType,
        MuTextType preferTagType,
        MuTextType preferValueType,
        MuTextType preferTextType,
        MuTextType preferAttributeNameType,
        MuTextType preferAttributeValueType,
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
    public string WriteElement(MuElement el, string? indent = null) {
        StringWriter writer = new();
        this.WriteElement(el, indent, writer);
        return writer.ToString();
    }
    public string WriteMembers(IEnumerable<MuElement> members, string? indent = null) {
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
    
    public void WriteDocument(MuDocument? doc, TextWriter writer) {
        if(doc == null) return;
        if(doc.Text != null) {
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
    
    public void WriteElement(MuElement? el, string? indent, TextWriter writer) {
        if(el == null) return;
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
            if(el.Values.Count > 1 && !this.ReduceSpaces) writer.Write(' ');
            this.WriteValues(el.Values, writer, false);
        }
        if(el.Text != null) {
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
    
    public void WriteMembers(IEnumerable<MuElement>? members, string? indent, TextWriter writer) {
        if(members == null) {
            writer.Write("{}");
            return;
        }
        writer.Write('{');
        string nextIndent = indent + this.Indent;
        bool anyMembers = false;
        foreach(MuElement member in members) {
            writer.Write(this.Newline);
            writer.Write(nextIndent);
            this.WriteElement(member, nextIndent, writer);
            anyMembers = true;
        }
        if(anyMembers) writer.Write(this.Newline);
        writer.Write(indent);
        writer.Write('}');
    }
    
    public void WriteAttributes(IEnumerable<MuAttribute>? attrs, TextWriter writer) {
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
        if(attr.Name != null) {
            writer.Write(MuUtil.ToIdentifierString(attr.Name, this.PreferAttributeNameType));
        }
        if(attr.Name == null && attr.Value == null) {
            writer.Write('=');
        }
        if(attr.Value != null) {
            writer.Write('=');
            writer.Write(MuUtil.ToIdentifierString(attr.Value, this.PreferAttributeValueType));
        }
    }
    
    public void WriteValues(IEnumerable<string>? values, TextWriter writer, bool lineSep) {
        if(values == null) return;
        bool first = true;
        foreach(string value in values) {
            if(!first) {
                if(lineSep) {
                    writer.Write(this.Newline);
                }
                else if(!this.ReduceSpaces) {
                    writer.Write(' ');
                }
            }
            first = false;
            writer.Write('=');
            writer.Write(MuUtil.ToIdentifierString(value, this.PreferValueType));
        }
    }
}
