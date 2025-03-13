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
        Assert.Equal("root", doc.GetFirstMember().Name);
        Assert.Equal("footer", doc.GetLastMember().Name);
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
        Assert.Null(elNone.GetFirstMember());
        Assert.Null(elNone.GetLastMember());
        Assert.Equal("{}", elNone.Members.ToString());
        Assert.True(elSolo.HasMembers());
        Assert.Equal("1", elSolo.GetFirstMember().Name);
        Assert.Equal("1", elSolo.GetLastMember().Name);
        Assert.Equal("{ 1 }", elSolo.Members.ToString());
        Assert.True(elFruits.HasMembers());
        Assert.Equal("apple", elFruits.GetFirstMember().Name);
        Assert.Equal("citrus", elFruits.GetLastMember().Name);
        Assert.Equal("{ apple berry citrus }", elFruits.Members.ToString());
        // addMember
        Assert.Equal(3, elFruits.Members.Count);
        elFruits.AddMember(new MuElement("durian"));
        Assert.Equal(4, elFruits.Members.Count);
        Assert.Equal("apple", elFruits.GetFirstMember().Name);
        Assert.Equal("durian", elFruits.GetLastMember().Name);
        Assert.Equal("{ apple berry citrus durian }", elFruits.Members.ToString());
    }
    
    // TODO: values, attributes
    
    // TODO: writing quoted strings
    
    // TODO: invalid unicode? arbitrary byte sequence?
    
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
