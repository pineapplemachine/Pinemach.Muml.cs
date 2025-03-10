using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Pinemach.Muml.Tests;

public readonly struct MuTestCase {
    public readonly string Muml;
    public readonly string Json;

    public MuTestCase(string muml, string json) {
        this.Muml = muml;
        this.Json = json;
    }

    public override string ToString() {
        return $"#MUML\n{this.Muml}\n#JSON\n{this.Json}\n";
    }
}

public class MuTests {
    private readonly ITestOutputHelper output;

    public MuTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
    }
    
    internal static IEnumerable<MuTestCase> ParseTestsInFile(string path) {
        const string testCasePattern = @"(?m)^#MUML.*\n((?s).*?)^#JSON.*\n((?s).*?)^#END";
        string testCasesText = File.ReadAllText(path);
        foreach(Match match in Regex.Matches(testCasesText, testCasePattern)) {
            yield return new MuTestCase(
                muml: match.Groups[1].Value,
                json: match.Groups[2].Value
            );
        }
    }
    
    internal void RunTestsInFile(string path) {
        int testCount = 0;
        foreach(var test in MuTests.ParseTestsInFile(path)) {
            try {
                var docActual = MuDocument.Parse(test.Muml);
                var docExpected = JsonConvert.DeserializeObject<MuDocument>(test.Json);
                Assert.Equal(docExpected, docActual);
            }
            catch(Exception e) {
                this.output.WriteLine($"Failed test #{1 + testCount} in file: {path}");
                throw new Exception(test.Muml, e);
            }
            testCount++;
        }
        this.output.WriteLine($"All {testCount} tests passed in file: {path}");
    }
    
    [Fact]
    public void RunTestsInFile_DocHeaders() {
        this.RunTestsInFile("muml/doc-headers.muml");
    }

    // [Fact]
    // public void WhenProvidedFahrenheitValue_ReturnsDegreeCelsiusValue()
    // {
    //     Assert.Equal(25, Converter.ToDegreeCelsius(77));
    // }
}
