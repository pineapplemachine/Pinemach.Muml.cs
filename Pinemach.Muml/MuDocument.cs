using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Pinemach.Muml;

/// <summary>
/// Interface implemented by MuDocument and MuElement.
/// </summary>
public interface IMuHasMembers {
    /// <summary>
    /// Members belonging to this object.
    /// In a document, members are all top-level elements.
    /// An element's members are enclosed within curly braces.
    /// </summary>
    public MuMembers Members { get; set; }
}

/// <summary>
/// Extension methods for MuDocument and MuElement objects.
/// </summary>
public static class MuHasMembersExtensions {
    /// <summary>
    /// Returns true when the object has at least one member.
    /// </summary>
    public static bool HasMembers(this IMuHasMembers obj) => (
        obj.Members.Count > 0
    );
}

/// <summary>
/// Interface implemented by MuDocument and MuElement.
/// </summary>
public interface IMuHasValues {
    /// <summary>
    /// Values belonging to this object.
    /// Values are text preceded with an equals sign.
    /// </summary>
    public MuValues Values { get; set; }
}

/// <summary>
/// Extension methods for MuDocument and MuElement objects.
/// </summary>
public static class MuHasValuesExtensions {
    /// <summary>
    /// Returns true when the object has at least one value.
    /// </summary>
    public static bool HasValues(this IMuHasValues obj) => (
        obj.Values.Count > 0
    );
    
    public static string? GetFirstValue(this IMuHasValues obj) => (
        obj.Values is { Count: > 0 } ? obj.Values[0] : null
    );
    public static string? GetLastValue(this IMuHasValues obj) => (
        obj.Values is { Count: > 0 } ? obj.Values[^1] : null
    );
    public static bool TryGetFirstValue(this IMuHasValues obj, out string? value) {
        bool ok = obj.Values is { Count: > 0 };
        value = ok ? obj.Values[0] : null;
        return ok;
    }
    public static bool TryGetLastValue(this IMuHasValues obj, out string? value) {
        bool ok = obj.Values is { Count: > 0 };
        value = ok ? obj.Values[^1] : null;
        return ok;
    }
}

/// <summary>
/// Interface implemented by MuElement.
/// </summary>
public interface IMuHasAttributes {
    /// <summary>
    /// Attributes belonging to this object.
    /// Attributes are key, value pairs, with non-unique keys.
    /// </summary>
    public MuAttributes Attributes { get; set; }
}

/// <summary>
/// Extension methods for MuElement objects.
/// </summary>
public static class MuHasAttributesExtensions {
    /// <summary>
    /// Returns true when the object has at least one attribute.
    /// </summary>
    public static bool HasAttributes(this IMuHasAttributes obj) => (
        obj.Attributes.Count > 0
    );
}

/// <summary>
/// Represents a Muml document, typically corresponding to a single file.
/// A document is a collection of one or more top-level Muml elements,
/// plus optional header content.
/// </summary>
public class MuDocument : IMuHasValues, IMuHasMembers {
    /// <summary>
    /// Error log object, representing errors which occurred while parsing
    /// this document.
    /// </summary>
    public readonly MuSourceErrors Errors = new();
    
    /// <summary>
    /// Returns true when the document has no parse errors associated with it.
    /// </summary>
    public bool IsOk() => (this.Errors.Count == 0);
    
    public string? Text;
    public MuValues Values { get; set; } = new();
    public MuMembers Members { get; set; } = new();
    
    public MuDocument() {}
    public MuDocument(MuSourceErrors errors) {
        this.Errors = errors;
    }
    public MuDocument(
        string? text = null,
        IEnumerable<string>? values = null,
        IEnumerable<MuElement>? members = null,
        IEnumerable<MuSourceError>? errors = null
    ) {
        this.Text = text;
        this.Values = MuValues.From(values);
        this.Members = MuMembers.From(members);
        this.Errors = MuSourceErrors.From(errors);
    }
    /// <summary>
    /// Parse and produce a MuDocument given a Muml document file.
    /// </summary>
    public static MuDocument Load(string filePath) => MuDocument.Parse(filePath, new StreamReader(filePath));
    
    /// <inheritdoc cref="Load(string)"/>
    public static MuDocument Load(string filePath, Encoding encoding) => MuDocument.Parse(filePath, new StreamReader(filePath, encoding));
    
    /// <summary>
    /// Parse and produce a MuDocument given a Muml source.
    /// </summary>
    public static MuDocument Parse(string source) => MuDocument.Parse(null, new StringReader(source));
    
    /// <inheritdoc cref="Parse(string)"/>
    public static MuDocument Parse(TextReader reader) => MuDocument.Parse(null, reader);
    
    /// <inheritdoc cref="Parse(string)"/>
    public static MuDocument Parse(string? fileName, string source) => MuDocument.Parse(fileName, new StringReader(source));
    
    /// <inheritdoc cref="Parse(string)"/>
    public static MuDocument Parse(string? fileName, TextReader reader) {
        MuParser parser = new(fileName, reader);
        parser.Parse();
        return parser.Document;
    }
    
    /// <summary>
    /// Encode the document as Muml and write it to a destination.
    /// </summary>
    public void Save(TextWriter textWriter) => MuWriter.Default.WriteDocument(this, textWriter);
    
    /// <inheritdoc cref="Save(TextWriter)"/>
    public void Save(TextWriter textWriter, MuWriter mumlWriter) => mumlWriter.WriteDocument(this, textWriter);
    
    /// <inheritdoc cref="Save(TextWriter)"/>
    public void Save(string path) => this.Save(path, MuWriter.Default);
    
    /// <inheritdoc cref="Save(TextWriter)"/>
    public void Save(string path, MuWriter mumlWriter) {
        using StreamWriter writer = new(path);
        mumlWriter.WriteDocument(this, writer);
    }
    
    /// <summary>
    /// Returns true if the document has non-null text.
    /// </summary>
    public bool HasText() => this.Text is not null;
    
    /// <summary>
    /// Get a Muml representation of this document.
    /// </summary>
    public override string ToString() => MuWriter.Condensed.WriteDocument(this);
    
    /// <summary>
    /// Get a Muml representation of this document, using the settings of a given MuWriter.
    /// </summary>
    public string ToString(MuWriter writer) => writer.WriteDocument(this);
    
    /// <summary>
    /// Compare equality of the contents of this document with another.
    /// </summary>
    public bool ContentEquals(object obj) => this.ContentEquals(obj as MuDocument);

    /// <inheritdoc cref="ContentEquals(object)" />
    public bool ContentEquals(MuDocument? doc) => (
        doc is not null &&
        this.Text == doc.Text &&
        MuUtil.SequencesEqual(this.Values, doc.Values) &&
        MuUtil.SequencesEqual(this.Members, doc.Members, MuElementContentComparer.Instance)
    );
}

/// <summary>
/// Represents an element within a Muml document.
/// Elements are the fundamental unit of structure and data within
/// a document.
/// <pre>name=value "text" [attrName=attrValue] {memberName}</pre>
/// </summary>
public class MuElement : IMuHasValues, IMuHasAttributes, IMuHasMembers {
    public string Name;
    public MuSourceSpan SourceSpan;
    public string? Text;
    public MuValues Values { get; set; } = new();
    public MuAttributes Attributes { get; set; } = new();
    public MuMembers Members { get; set; } = new();
    
    public MuElement() : this("") {}
    
    public MuElement(
        string name,
        string? text = null,
        IEnumerable<string>? values = null,
        IEnumerable<MuAttribute>? attributes = null,
        IEnumerable<MuElement>? members = null
    ) : this(
        MuSourceSpan.None,
        name,
        text,
        values,
        attributes,
        members
    ) {}
    
    public MuElement(
        MuSourceSpan sourceSpan,
        string name,
        string? text = null,
        IEnumerable<string>? values = null,
        IEnumerable<MuAttribute>? attributes = null,
        IEnumerable<MuElement>? members = null
    ) {
        this.Name = name;
        this.SourceSpan = sourceSpan;
        this.Text = text;
        this.Values = MuValues.From(values);
        this.Attributes = MuAttributes.From(attributes);
        this.Members = MuMembers.From(members);
    }
    
    /// <summary>
    /// Returns true when this element's name is possible to represent
    /// as a plain identifier string.
    /// Otherwise, the element's name can only be represented using a
    /// braced identifier.
    /// </summary>
    public bool HasIdentifierName() => MuUtil.IsIdentifierString(this.Name);
    
    /// <summary>
    /// Returns true if the element has non-null text.
    /// </summary>
    public bool HasText() => this.Text is not null;
    
    /// <summary>
    /// Get a Muml representation of this element.
    /// </summary>
    public override string ToString() => MuWriter.Condensed.WriteElement(this);
    
    /// <summary>
    /// Compare equality of the contents of this element with another.
    /// </summary>
    public bool ContentEquals(object obj) => this.ContentEquals(obj as MuElement);
    /// <inheritdoc cref="ContentEquals(object)" />
    public bool ContentEquals(MuElement? el) => (
        el is not null &&
        this.Name == el.Name &&
        this.Text == el.Text &&
        MuUtil.SequencesEqual(this.Values, el.Values) &&
        MuUtil.SequencesEqual(this.Attributes, el.Attributes) &&
        MuUtil.SequencesEqual(this.Members, el.Members, MuElementContentComparer.Instance)
    );
}

/// <summary>
/// Represents a list of value strings associated with a MuElement or
/// MuDocument.
/// </summary>
public class MuValues : List<string> {
    public MuValues() {}
    public MuValues(int capacity) : base(capacity) {}
    public MuValues(IEnumerable<string> collection) : base(collection) {}
    
    /// <summary>Initialize MuValues from values.</summary>
    public static MuValues From(IEnumerable<string>? values) => (
        values is MuValues list ? list :
        values is not null ? new(values) :
        new()
    );
    
    /// <summary>
    /// Get a HashSet representation of this attribute list.
    /// </summary>
    public HashSet<string> GetHashSet() => new(this);
    
    public bool ContainsValue(string? value) {
        if(value is null) return false;
        foreach(string val in this) {
            if(val == value) return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get a Muml representation of this value list.
    /// </summary>
    public override string ToString() => MuWriter.Condensed.WriteValues(this);
}

/// <summary>
/// Represents a list of member elements associated with a MuElement or
/// MuDocument.
/// </summary>
public class MuMembers : List<MuElement> {
    public MuMembers() {}
    public MuMembers(int capacity) : base(capacity) {}
    public MuMembers(IEnumerable<MuElement> collection) : base(collection) {}
    
    /// <summary>Initialize MuMembers from elements.</summary>
    public static MuMembers From(IEnumerable<MuElement>? members) => (
        members is MuMembers list ? list :
        members is not null ? new(members) :
        new()
    );
    
    /// <inheritdoc />
    public override string ToString() => MuWriter.Condensed.WriteMembers(this);
    
    /// <summary>
    /// Enumerate all elements in a tree, depth-first.
    /// </summary>
    public IEnumerable<MuElement> EnumerateTreeDepthFirst() => (
        this.EnumerateTreeDepthFirst(null)
    );
    
    /// <inheritdoc cref="EnumerateTreeDepthFirst()" />
    public IEnumerable<MuElement> EnumerateTreeDepthFirst(MuElement? elRoot) {
        if(this.Count <= 0) yield break;
        List<(MuElement?, MuMembers, int)> stack = new();
        stack.Add((elRoot, this, 0));
        while(stack.Count > 0) {
            (MuElement? el, MuMembers members, int index) = stack[^1];
            if(index < members.Count) {
                MuElement elChild = members[index];
                stack[^1] = (el, members, index + 1);
                stack.Add((elChild, elChild.Members, 0));
            }
            else {
                stack.RemoveAt(stack.Count - 1);
                if(el is not null) yield return el;
            }
        }
    }
    
    /// <summary>
    /// Enumerate all elements in a tree, breadth-first.
    /// </summary>
    public IEnumerable<MuElement> EnumerateTreeBreadthFirst() => (
        this.EnumerateTreeBreadthFirst(null)
    );

    /// <inheritdoc cref="EnumerateTreeBreadthFirst()" />
    public IEnumerable<MuElement> EnumerateTreeBreadthFirst(MuElement? elRoot) {
        if(this.Count <= 0) yield break;
        if(elRoot is not null) yield return elRoot;
        List<(MuMembers, int)> stack = new();
        stack.Add((this, 0));
        while(stack.Count > 0) {
            (MuMembers members, int index) = stack[^1];
            if(index < members.Count) {
                MuElement elChild = members[index];
                stack[^1] = (members, index + 1);
                stack.Add((elChild.Members, 0));
                yield return elChild;
            }
            else {
                stack.RemoveAt(stack.Count - 1);
            }
        }
    }
    
    /// <summary>
    /// Get the greatest depth of the element tree.
    /// This is a potentially expensive operation!
    /// It involves traversing the entire element tree.
    /// </summary>
    public int GetDepth() {
        List<(MuMembers, int)> stack = new();
        stack.Add((this, 0));
        int maxStackCount = 1;
        while(stack.Count > 0) {
            (MuMembers members, int index) = stack[^1];
            if(index < members.Count) {
                MuElement elChild = members[index];
                stack[^1] = (members, index + 1);
                stack.Add((elChild.Members, 0));
                maxStackCount = Math.Max(maxStackCount, stack.Count);
            }
            else {
                stack.RemoveAt(stack.Count - 1);
            }
        }
        return maxStackCount - 1;
    }
}

/// <summary>
/// Represents a list of attributes belonging to a MuElement.
/// </summary>
public class MuAttributes : List<MuAttribute> {
    public MuAttributes() {}
    public MuAttributes(int capacity) : base(capacity) {}
    public MuAttributes(IEnumerable<MuAttribute> collection) : base(collection) {}
    public MuAttributes(IEnumerable<KeyValuePair<string?, string?>> collection) :
        base(collection.Select(attr => new MuAttribute(attr.Key, attr.Value)))
    {}
    
    /// <summary>Initialize MuAttributes from attributes.</summary>
    public static MuAttributes From(IEnumerable<MuAttribute>? attrs) => (
        attrs is MuAttributes list ? list :
        attrs is not null ? new(attrs) :
        new()
    );
    
    /// <summary>Initialize MuAttributes from key, value pairs.</summary>
    public static MuAttributes From(IEnumerable<KeyValuePair<string?, string?>>? attrs) => (
        attrs is not null ? new(attrs) : new()
    );
    
    /// <summary>
    /// Get a Dictionary representation of this attribute list.
    /// The first instance of an attribute with any given name will be
    /// included in the returned Dictionary.
    /// </summary>
    public Dictionary<string, string?> GetDictionary() {
        Dictionary<string, string?> dict = new();
        for(int i = this.Count - 1; i >= 0; i--) {
            if(this[i].Name is not null) dict[this[i].Name!] = this[i].Value;
        }
        return dict;
    }
    
    /// <summary>
    /// Assign an attribute value.
    /// If there is any existing attribute with a matching name, then
    /// overwrite the value of the first matching attribute.
    /// If not, then add a new attribute with the given name and value.
    /// </summary>
    public void Set(string? name, string? value) {
        int i = this.IndexOf(name);
        if(i >= 0) {
            this[i] = new MuAttribute(name, value);
        }
        else {
            this.Add(new MuAttribute(name, value));
        }
    }
    
    /// <summary>
    /// Add a new attribute to the list.
    /// </summary>
    public void Add(string? name) => base.Add(new(name, null));
    
    /// <summary>
    /// Add a new attribute to the list.
    /// </summary>
    public void Add(string? name, string? value) => base.Add(new(name, value));
    
    /// <summary>
    /// Add a new attribute to the list.
    /// </summary>
    public void Add(KeyValuePair<string?, string?> attr) => base.Add(new(attr.Key, attr.Value));
    
    /// <summary>
    /// Add new attributes to the list.
    /// </summary>
    public void AddRange(IEnumerable<KeyValuePair<string?, string?>> attrs) => this.AddRange(
        attrs.Select(attr => new MuAttribute(attr.Key, attr.Value))
    );
    
    /// <summary>
    /// Check whether the list contains any attribute with a given name.
    /// </summary>
    public bool ContainsName(string? name) {
        foreach(MuAttribute attr in this) {
            if(attr.Name == name) return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get the index of the first attribute with a matching name.
    /// Returns -1 when there was no matching attribute.
    /// </summary>
    public int IndexOf(string? name) {
        for(int i = 0; i < this.Count; i++) {
            if(this[i].Name == name) return i;
        }
        return -1;
    }
    
    /// <summary>
    /// Get the index of the last attribute with a matching name.
    /// Returns -1 when there was no matching attribute.
    /// </summary>
    public int LastIndexOf(string? name) {
        for(int i = this.Count - 1; i >= 0; i--) {
            if(this[i].Name == name) return i;
        }
        return -1;
    }
    
    /// <summary>
    /// Get the value of the first attribute with a matching name.
    /// Returns null when there was no matching attribute.
    /// </summary>
    public string? GetValue(string? name) => this.GetValue(name, null);
    
    /// <summary>
    /// Get the value of the first attribute with a matching name.
    /// Returns the fallback value when there was no matching attribute.
    /// </summary>
    public string? GetValue(string? name, string? fallback) {
        foreach(MuAttribute attr in this) {
            if(attr.Name == name) return attr.Value;
        }
        return fallback;
    }
    
    /// <summary>
    /// Get the value of the first attribute with a matching name.
    /// </summary>
    public bool TryGetValue(string? name, out string? value) {
        foreach(MuAttribute attr in this) {
            if(attr.Name == name) {
                value = attr.Value;
                return true;
            }
        }
        value = null;
        return false;
    }
    
    /// <summary>
    /// Get an enumerable of all values of attributes with matching names.
    /// </summary>
    public IEnumerable<string?> GetAllValues(string? name) {
        foreach(MuAttribute attr in this) {
            if(attr.Name == name) yield return attr.Value;
        }
    }
    
    /// <summary>
    /// Get a Muml representation of this attribute list.
    /// </summary>
    public override string ToString() => MuWriter.Condensed.WriteAttributes(this);
}

/// <summary>
/// Represents an attribute belonging to a MuElement.
/// An attribute is fundamentally a key, value pair.
/// </summary>
public struct MuAttribute {
    public string? Name;
    public string? Value;
    
    public MuAttribute(string? name) : this(name, null) {}
    public MuAttribute(string? name, string? value) {
        this.Name = name;
        this.Value = value;
    }
    
    public bool HasValue() => !string.IsNullOrEmpty(this.Value);
    
    public override string ToString() => MuWriter.Condensed.WriteAttribute(this);

    public override int GetHashCode() {
        return HashCode.Combine(this.Name, this.Value);
    }
    
    public override bool Equals(object? obj) => (
        (obj is MuAttribute attr && this.Equals(attr)) ||
        (obj is KeyValuePair<string?, string?> pair && this.Equals(pair))
    );
    public bool Equals(MuAttribute attr) => (
        this.Name == attr.Name &&
        this.Value == attr.Value
    );
    public bool Equals(KeyValuePair<string?, string?> attr) => (
        this.Name == attr.Key &&
        this.Value == attr.Value
    );
    public bool Equals(string? name, string? value) => (
        this.Name == name &&
        this.Value == value
    );

    public static bool operator ==(MuAttribute left, MuAttribute right) => left.Equals(right);
    public static bool operator !=(MuAttribute left, MuAttribute right) => !(left == right);
    
    public static implicit operator (string?, string?)(MuAttribute attr) {
        return (attr.Name, attr.Value);
    }
    public static implicit operator KeyValuePair<string?, string?>(MuAttribute attr) {
        return new(attr.Name, attr.Value);
    }
    
    private const NumberStyles IntNumberStyles = (
        NumberStyles.Integer |
        NumberStyles.AllowThousands |
        NumberStyles.AllowTrailingSign |
        NumberStyles.AllowParentheses
    );
    private const NumberStyles FloatNumberStyles = (
        NumberStyles.Float |
        NumberStyles.Number |
        NumberStyles.AllowThousands |
        NumberStyles.AllowTrailingSign |
        NumberStyles.AllowParentheses
    );
    
    /// <summary>
    /// Get the attribute's value as a string. No coercion necessary.
    /// </summary>
    public string? AsString() => this.Value;
    
    /// <summary>
    /// Get the attribute's value, coerced to a boolean.
    /// </summary>
    public bool AsBool() => (this.Value == "true");
    
    /// <summary>
    /// Get the attribute's value, coerced to an int.
    /// </summary>
    public int AsInt32() => int.TryParse(
        this.Value,
        MuAttribute.IntNumberStyles,
        CultureInfo.InvariantCulture,
        out int result
    ) ? result : 0;
    
    /// <summary>
    /// Get the attribute's value, coerced to a long.
    /// </summary>
    public long AsInt64() => long.TryParse(
        this.Value,
        MuAttribute.IntNumberStyles,
        CultureInfo.InvariantCulture,
        out long result
    ) ? result : 0;
    
    /// <summary>
    /// Get the attribute's value, coerced to a float.
    /// </summary>
    public float AsSingle() => float.TryParse(
        this.Value,
        MuAttribute.FloatNumberStyles,
        CultureInfo.InvariantCulture,
        out float result
    ) ? result : float.NaN;
    
    /// <summary>
    /// Get the attribute's value, coerced to a double.
    /// </summary>
    public double AsDouble() => double.TryParse(
        this.Value,
        MuAttribute.FloatNumberStyles,
        CultureInfo.InvariantCulture,
        out double result
    ) ? result : double.NaN;
}
