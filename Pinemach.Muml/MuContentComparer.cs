using System.Collections.Generic;

namespace Pinemach.Muml;

/// <summary>
/// Comparer which checks equality of document content.
/// </summary>
public class MuDocumentContentComparer : IEqualityComparer<MuDocument> {
    /// <summary>Singleton instance.</summary>
    public static readonly MuDocumentContentComparer Instance = new();
    
    /// <inheritdoc />
    public bool Equals(MuDocument? doc1, MuDocument? doc2) => doc1?.ContentEquals(doc2) ?? false;
    
    /// <inheritdoc />
    public int GetHashCode(MuDocument doc) => doc.GetHashCode();
}

/// <summary>
/// Comparer which checks equality of element content.
/// </summary>
public class MuElementContentComparer : IEqualityComparer<MuElement> {
    /// <summary>Singleton instance.</summary>
    public static readonly MuElementContentComparer Instance = new();
    
    /// <inheritdoc />
    public bool Equals(MuElement? el1, MuElement? el2) => el1?.ContentEquals(el2) ?? false;
    
    /// <inheritdoc />
    public int GetHashCode(MuElement el) => el.GetHashCode();
}
