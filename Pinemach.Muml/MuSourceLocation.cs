namespace Pinemach.Muml;

/// <summary>
/// Represents a single specific byte location within a source text.
/// </summary>
public struct MuSourceLocation {
    public static readonly MuSourceLocation None = new(null, -1, 0, 0);
    
    public string FileName;
    public int Index;
    public int LineStartIndex;
    public int LineNumber;
    
    public MuSourceLocation(int index, int lineStartIndex, int lineNumber) :
        this(null, index, lineStartIndex, lineNumber)
    {}
    public MuSourceLocation(string fileName, int index, int lineStartIndex, int lineNumber) {
        this.FileName = fileName;
        this.Index = index;
        this.LineStartIndex = lineStartIndex;
        this.LineNumber = lineNumber;
    }
    
    public bool IsValid() => this.Index >= 0;
    
    public int ColumnNumber {
        get => 1 + this.Index - this.LineStartIndex;
    }
    
    public MuSourceSpan SpanFrom(MuSourceLocation start) => new MuSourceSpan(
        fileName: this.FileName,
        startIndex: start.Index,
        startLineStartIndex: start.LineStartIndex,
        startLineNumber: start.LineNumber,
        endIndex: this.Index,
        endLineStartIndex: this.LineStartIndex,
        endLineNumber: this.LineNumber
    );
    public MuSourceSpan SpanTo(MuSourceLocation end) => new MuSourceSpan(
        fileName: this.FileName,
        startIndex: this.Index,
        startLineStartIndex: this.LineStartIndex,
        startLineNumber: this.LineNumber,
        endIndex: end.Index,
        endLineStartIndex: end.LineStartIndex,
        endLineNumber: end.LineNumber
    );
    
    public string ToLString() => (
        this.IsValid() ? $"L{this.LineNumber}:{this.ColumnNumber}" : "?"
    );
    
    public override string ToString() => (
        !string.IsNullOrEmpty(this.FileName) ?
        $"{this.ToLString()} in {this.FileName}" :
        this.ToLString()
    );
}

/// <summary>
/// Represents a byte span within a source text, with start and end location.
/// </summary>
public struct MuSourceSpan {
    public static readonly MuSourceSpan None = new(null, -1, 0, 0, -1, 0, 0);
    
    public string FileName;
    public int StartIndex;
    public int StartLineStartIndex;
    public int StartLineNumber;
    public int EndIndex;
    public int EndLineStartIndex;
    public int EndLineNumber;
    
    public MuSourceSpan(MuSourceLocation loc) : this(loc, loc) {}
    public MuSourceSpan(MuSourceLocation start, MuSourceLocation end) : this(
        start.FileName,
        start.Index,
        start.LineStartIndex,
        start.LineNumber,
        end.Index,
        end.LineStartIndex,
        end.LineNumber
    ) {}
    
    public MuSourceSpan(
        string fileName,
        int startIndex,
        int startLineStartIndex,
        int startLineNumber,
        int endIndex,
        int endLineStartIndex,
        int endLineNumber
    ) {
        this.FileName = fileName;
        this.StartIndex = startIndex;
        this.StartLineStartIndex = startLineStartIndex;
        this.StartLineNumber = startLineNumber;
        this.EndIndex = endIndex;
        this.EndLineStartIndex = endLineStartIndex;
        this.EndLineNumber = endLineNumber;
    }
    
    public readonly bool IsStartValid() => this.StartIndex >= 0;
    public readonly bool IsEndValid() => this.EndIndex >= 0;
    public readonly bool IsStartAndEndValid() => this.StartIndex >= 0 && this.EndIndex >= 0;
    
    public readonly int Length {
        get => this.EndIndex - this.StartIndex;
    }
    public readonly int StartColumnNumber {
        get => 1 + this.StartIndex - this.StartLineStartIndex;
    }
    public readonly int EndColumnNumber {
        get => 1 + this.EndIndex - this.EndLineStartIndex;
    }
    
    public readonly MuSourceLocation Start {
        get => new MuSourceLocation(this.FileName, this.StartIndex, this.StartLineStartIndex, this.StartLineNumber);
    }
    public readonly MuSourceLocation End {
        get => new MuSourceLocation(this.FileName, this.EndIndex, this.EndLineStartIndex, this.EndLineNumber);
    }
    
    public readonly string StartToLString() => (
        this.IsStartValid() ? $"L{this.StartLineNumber}:{this.StartColumnNumber}" : "?"
    );
    public readonly string EndToLString() => (
        this.IsEndValid() ? $"L{this.EndLineNumber}:{this.EndColumnNumber}" : "?"
    );
    public readonly string ToLString() {
        if(!this.IsStartValid()) {
            return "?";
        }
        else if(!this.IsEndValid()) {
            return this.StartToLString();
        }
        else if(this.EndLineNumber != this.StartLineNumber) {
            return $"{this.StartToLString()}..{this.EndToLString()}";
        }
        else if(this.StartColumnNumber != this.EndColumnNumber) {
            return $"L{this.StartLineNumber}:{this.StartColumnNumber}..{this.EndColumnNumber}";
        }
        else {
            return this.StartToLString();
        }
    }
    
    public readonly override string ToString() => (
        !string.IsNullOrEmpty(this.FileName) ?
        $"{this.ToLString()} in {this.FileName}" :
        this.ToLString()
    );
}
