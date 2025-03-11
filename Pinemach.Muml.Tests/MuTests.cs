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
            yield return new MuTestCase(
                muml: match.Groups[1].Value.StartsWith("_BLANK") ? "" : match.Groups[2].Value,
                json: match.Groups[3].Value,
                errors: (match.Groups[4].Value ?? "").Split("\n").Select(l => l.Trim()).Where(l => l.Length > 0).ToList()
            );
        }
    }
    
    internal void RunTestsInFile(string path) {
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
    }
    
    [Fact]
    public void RunTestsInFiles() {
        // this.RunTestsInFile("muml/scratch.muml");
        this.RunTestsInFile("muml/strings.muml");
        
        this.RunTestsInFile("muml/errors.muml");
        this.RunTestsInFile("muml/doc.muml");
        this.RunTestsInFile("muml/attributes.muml");
        this.RunTestsInFile("muml/elements.muml");
    }
    
    [Fact]
    public void NoMemberRecursionCrash() {
        // The Muml parser does not use recursion for members (or otherwise)
        // and therefore should not crash with a call stack overflow here.
        // This is a common oversight in parser implementations!
        string muml = "x" + new string('{', 10000);
        MuDocument doc = MuDocument.Parse(muml);
        Assert.Equal(doc.Errors.Select(e => e.ToString()), new List<string>() {
            "UnterminatedMembers L1:2",
            "UnexpectedOpenBrace L1:3",
        });
    }
}
