using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Pinemach.Muml.Tests;

public readonly struct MuTestCase {
    public readonly string Muml;
    public readonly string Json;
    public readonly List<string> Errors;

    public MuTestCase(string muml, string json, List<string> errors) {
        this.Muml = muml;
        this.Json = json;
        this.Errors = errors;
    }

    public override string ToString() => (
        (string.IsNullOrEmpty(this.Muml) ? "#MUML_BLANK\n" : $"#MUML\n{this.Muml}\n") +
        $"#JSON\n{this.Json}\n" +
        (this.Errors.Count > 0 ? $"#Errors\n{string.Join("\n", this.Errors)}\n" : "") +
        "#END"
    );
}

public class MuTests {
    private readonly ITestOutputHelper output;

    public MuTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
    }
    
    internal IEnumerable<MuTestCase> ParseTestsInFile(string path) {
        const string testCasePattern = (
            @"(?m)" +
            @"^#MUML(_BLANK.*\n|.*\n((?s).*?)\n)" +
            @"(?:^#JSON.*\n((?s).*?))?" +
            @"(?:^#ERRORS.*\n((?s).*?))?" +
            @"^#END"
        );
        string testCasesText = File.ReadAllText(path);
        foreach(Match match in Regex.Matches(testCasesText, testCasePattern)) {
            yield return new(
                muml: match.Groups[1].Value.StartsWith("_BLANK") ? "" : match.Groups[2].Value,
                json: match.Groups[3].Value,
                errors: match.Groups[4].Value.Split("\n").Select(l => l.Trim()).Where(l => l.Length > 0).ToList()
            );
        }
    }
    
    internal int RunTestsInFile(string path) {
        int testCount = 0;
        foreach(var test in this.ParseTestsInFile(path)) {
            MuDocument docActual = null;
            try {
                docActual = MuDocument.Parse(test.Muml);
                Assert.Equal(test.Errors, docActual.Errors.Select(e => e.ToString()).ToList());
                if(!string.IsNullOrEmpty(test.Json)) {
                    var docExpected = JsonConvert.DeserializeObject<MuDocument>(test.Json);
                    try {
                        Assert.Equal(docExpected, docActual, MuDocumentContentComparer.Instance);
                    }
                    catch {
                        this.output.WriteLine($"Expected: {MuWriter.Condensed.WriteDocument(docExpected)}");
                        this.output.WriteLine($"Actual:   {MuWriter.Condensed.WriteDocument(docActual)}");
                        throw;
                    }
                }
                string docActualMuml = MuWriter.Condensed.WriteDocument(docActual);
                this.output.WriteLine($"Passed: {docActualMuml}");
            }
            catch(Exception e) {
                this.output.WriteLine($"Failed test #{1 + testCount} in file: {path}");
                this.output.WriteLine($"Muml: {MuUtil.ToQuotedString(test.Muml)}");
                this.output.WriteLine($"Parsed object: {JsonConvert.SerializeObject(docActual)}");
                throw new Exception(test.Muml, e);
            }
            testCount++;
        }
        this.output.WriteLine($"All {testCount} tests passed in file: {path}");
        return testCount;
    }
    
    [Fact]
    public void RunTestsInFiles() {
        int testCount = 0;
        testCount += this.RunTestsInFile("TestCases/errors.mumltest");
        testCount += this.RunTestsInFile("TestCases/doc.mumltest");
        testCount += this.RunTestsInFile("TestCases/attributes.mumltest");
        testCount += this.RunTestsInFile("TestCases/elements.mumltest");
        testCount += this.RunTestsInFile("TestCases/strings.mumltest");
        testCount += this.RunTestsInFile("TestCases/escapes.mumltest");
        testCount += this.RunTestsInFile("TestCases/format.mumltest");
        testCount += this.RunTestsInFile("TestCases/comments.mumltest");
        this.output.WriteLine($"All {testCount} tests passed across all files");
    }
    
    [Fact]
    public void DocumentAccessApi() {
        MuDocument doc = MuDocument.Parse("""
            root [hello=world] {
                h1 | Example text
                bool { false true }
                vector [x=1 y=2 z=3]
                {"eq="} =1 =1 =2 =2
            }
            footer=bottom [a=1 a=2 a=3]
        """);
        Assert.False(doc.HasText());
        Assert.False(doc.HasValues());
        Assert.Equal(3, doc.Members.GetDepth());
        Assert.Equal(
            new List<string> {"h1", "false", "true", "bool", "vector", "eq=", "root", "footer"},
            doc.Members.EnumerateTreeDepthFirst().Select(el => el.Name)
        );
        Assert.Equal(
            new List<string> {"root", "h1", "bool", "false", "true", "vector", "eq=", "footer"},
            doc.Members.EnumerateTreeBreadthFirst().Select(el => el.Name)
        );
    }
    
    [Fact]
    public void HasMembers() {
        MuDocument doc = MuDocument.Parse("""
            none {}
            solo { 1 }
            fruits { apple berry citrus }
        """);
        Assert.Equal(3, doc.Members.Count);
        MuElement elNone = doc.Members[0];
        MuElement elSolo = doc.Members[1];
        MuElement elFruits = doc.Members[2];
        Assert.False(elNone.HasMembers());
        Assert.Equal("{}", elNone.Members.ToString());
        Assert.True(elSolo.HasMembers());
        Assert.Equal("{ 1 }", elSolo.Members.ToString());
        Assert.True(elFruits.HasMembers());
        Assert.Equal("{ apple berry citrus }", elFruits.Members.ToString());
        // Members.Add
        Assert.Equal(3, elFruits.Members.Count);
        elFruits.Members.Add(new MuElement("durian"));
        Assert.Equal(4, elFruits.Members.Count);
        Assert.Equal("{ apple berry citrus durian }", elFruits.Members.ToString());
    }
    
    [Fact]
    public void HasValues() {
        MuDocument doc = MuDocument.Parse("""
            none
            one=value
            multi =1 =2 =3 =1 =2 =3
        """);
        Assert.Equal(3, doc.Members.Count);
        MuElement elNone = doc.Members[0];
        MuElement elOne = doc.Members[1];
        MuElement elMulti = doc.Members[2];
        Assert.False(elNone.HasValues());
        Assert.Null(elNone.GetFirstValue());
        Assert.Null(elNone.GetLastValue());
        Assert.Equal("", elNone.Values.ToString());
        Assert.True(elOne.HasValues());
        Assert.Equal("value", elOne.GetFirstValue());
        Assert.Equal("value", elOne.GetLastValue());
        Assert.Equal("=value", elOne.Values.ToString());
        Assert.True(elMulti.HasValues());
        Assert.Equal("1", elMulti.GetFirstValue());
        Assert.Equal("3", elMulti.GetLastValue());
        Assert.Equal("=1 =2 =3 =1 =2 =3", elMulti.Values.ToString());
        Assert.Equal(new HashSet<string> {"1", "2", "3"}, elMulti.Values.GetHashSet());
        Assert.True(elMulti.Values.ContainsValue("1"));
        Assert.False(elMulti.Values.ContainsValue("4"));
    }
    
    [Fact]
    public void HasAttributes() {
        MuDocument doc = MuDocument.Parse("""
            none
            one [name=value]
            vector [x=1 y=2 z=3]
            repeat [a=1 b=2 a=3 b=4]
        """);
        Assert.Equal(4, doc.Members.Count);
        MuElement elNone = doc.Members[0];
        MuElement elOne = doc.Members[1];
        MuElement elVector = doc.Members[2];
        MuElement elRepeat = doc.Members[3];
        Assert.False(elNone.HasAttributes());
        Assert.Equal("[]", elNone.Attributes.ToString());
        Assert.True(elOne.HasAttributes());
        Assert.Equal("[name=value]", elOne.Attributes.ToString());
        Assert.True(elVector.HasAttributes());
        Assert.Equal("[x=1 y=2 z=3]", elVector.Attributes.ToString());
        Assert.True(elRepeat.HasAttributes());
        Assert.Equal("[a=1 b=2 a=3 b=4]", elRepeat.Attributes.ToString());
        // Attributes.ContainsName
        Assert.True(elRepeat.Attributes.ContainsName("a"));
        Assert.True(elRepeat.Attributes.ContainsName("b"));
        Assert.False(elRepeat.Attributes.ContainsName("c"));
        // Attributes.GetValue
        Assert.Equal("1", elRepeat.Attributes.GetValue("a"));
        Assert.Equal("2", elRepeat.Attributes.GetValue("b"));
        Assert.Null(elRepeat.Attributes.GetValue("c"));
        Assert.Equal("1", elRepeat.Attributes.GetValue("a", "fallback"));
        Assert.Equal("2", elRepeat.Attributes.GetValue("b", "fallback"));
        Assert.Equal("fallback", elRepeat.Attributes.GetValue("c", "fallback"));
        // Attributes.IndexOf
        Assert.Equal(0, elRepeat.Attributes.IndexOf("a"));
        Assert.Equal(1, elRepeat.Attributes.IndexOf("b"));
        Assert.Equal(-1, elRepeat.Attributes.IndexOf("c"));
        // Attributes.LastIndexOf
        Assert.Equal(2, elRepeat.Attributes.LastIndexOf("a"));
        Assert.Equal(3, elRepeat.Attributes.LastIndexOf("b"));
        Assert.Equal(-1, elRepeat.Attributes.LastIndexOf("c"));
        // Attributes.GetAllValues
        Assert.Equal(new List<string> { "1", "3" }, elRepeat.Attributes.GetAllValues("a"));
        Assert.Equal(new List<string> { "2", "4" }, elRepeat.Attributes.GetAllValues("b"));
        Assert.Empty(elRepeat.Attributes.GetAllValues("c"));
        // Attributes.TryGetValue
        Assert.True(elRepeat.Attributes.TryGetValue("a", out string attrA));
        Assert.True(elRepeat.Attributes.TryGetValue("b", out string attrB));
        Assert.False(elRepeat.Attributes.TryGetValue("c", out string attrC));
        Assert.Equal("1", attrA);
        Assert.Equal("2", attrB);
        Assert.Null(attrC);
        // Attributes.GetDictionary
        Assert.Empty(elNone.Attributes.GetDictionary());
        var vecAttrs = elVector.Attributes.GetDictionary();
        Assert.Equal(3, vecAttrs.Count);
        Assert.Equal(new MuAttribute("x", "1"), vecAttrs["x"]);
        Assert.Equal(new MuAttribute("y", "2"), vecAttrs["y"]);
        Assert.Equal(new MuAttribute("z", "3"), vecAttrs["z"]);
        // Attributes.GetValueDictionary
        Assert.Empty(elNone.Attributes.GetValueDictionary());
        var vecValues = elVector.Attributes.GetValueDictionary();
        Assert.Equal(3, vecValues.Count);
        Assert.Equal("1", vecValues["x"]);
        Assert.Equal("2", vecValues["y"]);
        Assert.Equal("3", vecValues["z"]);
        // Attributes.Add
        var addAttrs = new MuAttributes();
        addAttrs.Add("a");
        addAttrs.Add("b", "1");
        addAttrs.Add(new KeyValuePair<string, string>("c", "2"));
        Assert.Equal("[a b=1 c=2]", addAttrs.ToString());
        // Attributes.Set
        elRepeat.Attributes.Set("c", "5");
        Assert.Equal("[a=1 b=2 a=3 b=4 c=5]", elRepeat.Attributes.ToString());
        elRepeat.Attributes.Set("a", "0");
        Assert.Equal("[a=0 b=2 a=3 b=4 c=5]", elRepeat.Attributes.ToString());
        // Attributes.AddRange
        elRepeat.Attributes.AddRange(Enumerable.Empty<MuAttribute>());
        Assert.Equal("[a=0 b=2 a=3 b=4 c=5]", elRepeat.Attributes.ToString());
        elRepeat.Attributes.AddRange(new List<MuAttribute> { new("d", "6"), new("c", "7") });
        Assert.Equal("[a=0 b=2 a=3 b=4 c=5 d=6 c=7]", elRepeat.Attributes.ToString());
        elRepeat.Attributes.AddRange(new List<KeyValuePair<string, string>> { new("d", "1"), new("c", "2") });
        Assert.Equal("[a=0 b=2 a=3 b=4 c=5 d=6 c=7 d=1 c=2]", elRepeat.Attributes.ToString());
    }
    
    [Fact]
    public void AttributeAsBool() {
        Assert.False(new MuAttribute("x", null).AsBool());
        Assert.False(new MuAttribute("x", "").AsBool());
        Assert.False(new MuAttribute("x", "f").AsBool());
        Assert.False(new MuAttribute("x", "F").AsBool());
        Assert.False(new MuAttribute("x", "false").AsBool());
        Assert.False(new MuAttribute("x", "False").AsBool());
        Assert.False(new MuAttribute("x", "FALSE").AsBool());
        Assert.False(new MuAttribute("x", "t").AsBool());
        Assert.False(new MuAttribute("x", "T").AsBool());
        Assert.False(new MuAttribute("x", "True").AsBool());
        Assert.False(new MuAttribute("x", "TRUE").AsBool());
        Assert.False(new MuAttribute("x", "etc").AsBool());
        Assert.True(new MuAttribute("x", "true").AsBool());
    }
    
    [Fact]
    public void AttributeAsInt() {
        // Int32
        Assert.Equal(0, new MuAttribute("x", null).AsInt32());
        Assert.Equal(0, new MuAttribute("x", "").AsInt32());
        Assert.Equal(0, new MuAttribute("x", "etc").AsInt32());
        Assert.Equal(0, new MuAttribute("x", "0").AsInt32());
        Assert.Equal(1234, new MuAttribute("x", "1234").AsInt32());
        // Int64
        Assert.Equal(0, new MuAttribute("x", null).AsInt64());
        Assert.Equal(0, new MuAttribute("x", "").AsInt64());
        Assert.Equal(0, new MuAttribute("x", "etc").AsInt64());
        Assert.Equal(0, new MuAttribute("x", "0").AsInt64());
        Assert.Equal(1234, new MuAttribute("x", "1234").AsInt64());
    }
    
    [Fact]
    public void AttributeAsFloat() {
        // Single
        Assert.True(float.IsNaN(new MuAttribute("x", null).AsSingle()));
        Assert.True(float.IsNaN(new MuAttribute("x", "").AsSingle()));
        Assert.True(float.IsNaN(new MuAttribute("x", "etc").AsSingle()));
        Assert.Equal(0, new MuAttribute("x", "0").AsSingle());
        Assert.Equal(1234, new MuAttribute("x", "1234").AsSingle());
        Assert.Equal(0.125, new MuAttribute("x", "0.125").AsSingle(), 4);
        Assert.Equal(1e-3, new MuAttribute("x", "1e-3").AsSingle(), 4);
        // Double
        Assert.True(double.IsNaN(new MuAttribute("x", null).AsDouble()));
        Assert.True(double.IsNaN(new MuAttribute("x", "").AsDouble()));
        Assert.True(double.IsNaN(new MuAttribute("x", "etc").AsDouble()));
        Assert.Equal(0, new MuAttribute("x", "0").AsDouble());
        Assert.Equal(1234, new MuAttribute("x", "1234").AsDouble());
        Assert.Equal(0.125, new MuAttribute("x", "0.125").AsDouble(), 4);
        Assert.Equal(1e-3, new MuAttribute("x", "1e-3").AsDouble(), 4);
    }
    
    [Fact]
    public void WriteStrings() {
        MuDocument doc = MuDocument.Parse("""
            el `'x'` el `'x` el `x'` el `'x ` el ` x'`
            el `"x"` el `"x` el `x"` el `"x ` el ` x"`
            el '`x`' el '`x' el 'x`' el '`x ' el ' x`'
            el `'''` el ` '''` el `''' ` el ` ''' `
            el `\"\"\"` el ` \"\"\"` el `\"\"\" ` el ` \"\"\" `
            el '```' el ' ```' el '``` ' el ' ``` '
        """);
        Assert.Equal(doc, MuDocument.Parse(doc.Write(new(null, " ", MuTextType.DoubleQuote))), MuDocumentContentComparer.Instance);
        Assert.Equal(doc, MuDocument.Parse(doc.Write(new(null, " ", MuTextType.SingleQuote))), MuDocumentContentComparer.Instance);
        Assert.Equal(doc, MuDocument.Parse(doc.Write(new(null, " ", MuTextType.Backtick))), MuDocumentContentComparer.Instance);
        Assert.Equal(doc, MuDocument.Parse(doc.Write(new(null, " ", MuTextType.DoubleQuoteFence))), MuDocumentContentComparer.Instance);
        Assert.Equal(doc, MuDocument.Parse(doc.Write(new(null, " ", MuTextType.SingleQuoteFence))), MuDocumentContentComparer.Instance);
        Assert.Equal(doc, MuDocument.Parse(doc.Write(new(null, " ", MuTextType.BacktickFence))), MuDocumentContentComparer.Instance);
    }
    
    [Fact]
    public void InvalidUtf16SurrogatePair() {
        MuDocument doc = MuDocument.Parse("test | \uDF48\uD800");
        string text = doc.Members[0].Text;
        Assert.Equal(2, text.Length);
        Assert.Equal("\uDF48\uD800", text);
        Assert.Equal('\uDF48', text[0]);
        Assert.Equal('\uD800', text[1]);
    }
    
    [Fact]
    public void NullCharacter() {
        MuDocument doc = MuDocument.Parse("test | null\u0000null");
        string text = doc.Members[0].Text;
        Assert.Equal(9, text.Length);
        Assert.Equal("null\u0000null", text);
        Assert.Equal(0, text[4]);
    }
    
    // The Muml parser does not use recursion for members (or otherwise)
    // and therefore should not crash with a call stack overflow here.
    // This is a common oversight in parser implementations!
    [Fact]
    public void DeeplyNestedMembersMalformed() {
        const int repeatCount = 10000;
        string muml = string.Concat(Enumerable.Repeat("x{", repeatCount));
        MuDocument doc = MuDocument.Parse(muml);
        Assert.Equal(repeatCount, doc.Errors.Count);
        Assert.Equal("UnterminatedMembers L1:2", doc.Errors[0].ToString());
    }
    [Fact]
    public void DeeplyNestedMembersWellFormed() {
        const int repeatCount = 10000;
        string muml = (
            string.Concat(Enumerable.Repeat("x{", repeatCount)) +
            string.Concat(Enumerable.Repeat("}", repeatCount))
        );
        MuDocument doc = MuDocument.Parse(muml);
        Assert.Empty(doc.Errors);
        Assert.Equal(repeatCount, doc.Members.GetDepth());
    }
    [Fact]
    public void DeeplyNestedComments() {
        const int repeatCount = 10000;
        string muml = string.Concat(Enumerable.Repeat("#[", repeatCount));
        MuDocument doc = MuDocument.Parse(muml);
        Assert.Single(doc.Errors);
        Assert.Equal("UnterminatedNestedBlockComment L1:1", doc.Errors[0].ToString());
    }
}
