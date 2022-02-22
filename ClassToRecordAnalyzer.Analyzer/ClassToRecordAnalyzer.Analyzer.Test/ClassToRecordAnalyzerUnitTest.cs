using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = ClassToRecordAnalyzer.Analyzer.Test.CSharpCodeFixVerifier<
    ClassToRecordAnalyzer.Analyzer.ClassToRecordAnalyzer,
    ClassToRecordAnalyzer.Analyzer.ClassToRecordAnalyzerCodeFixProvider>;

namespace ClassToRecordAnalyzer.Analyzer.Test;

[TestClass]
public class ClassToRecordAnalyzerUnitTest
{
    [TestMethod]
    public async Task DoesNothingWhenCodeIsBlanck()
    {
        var test = @"";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [TestMethod]
    public async Task DoesNothingIfAtLeastOnePropertyIsNotPublic()
    {
        var test = @"using System;

namespace ConsoleApplication1
{
    public class Person
    {
        public string Name { get; set; }
        private int Age { get; set; }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [TestMethod]
    public async Task DoesNothingIfAtLeastOneMethod()
    {
        var test = @"using System;

namespace ConsoleApplication1
{
    public class Person
    {
        public string Name { get; set; }
        public void DoNothing() { }
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [TestMethod]
    public async Task CreateRecordWhenDoesHaveOnlyOneProperty()
    {
        var test = @"using System;

namespace ConsoleApplication1
{
    public class Person
    {
        public string Name { get; set; }
    }
}";

        var fixtest = @"using System;

namespace ConsoleApplication1
{
    public record Person(string Name);
}".ReplaceLineEndings();

        var expected = VerifyCS.Diagnostic("CRA0001")
            .WithSpan(5, 18, 5, 24)
            .WithMessage("Type name 'Person' can be a record.");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
    }

    [TestMethod]
    public async Task CreateRecordWhenDoesHaveTwoOrMoreProperties()
    {
        var test = @"using System;

namespace ConsoleApplication1
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}";

        var fixtest = @"using System;

namespace ConsoleApplication1
{
    public record Person(string Name, int Age);
}".ReplaceLineEndings();

        var expected = VerifyCS.Diagnostic("CRA0001")
            .WithSpan(5, 18, 5, 24)
            .WithMessage("Type name 'Person' can be a record.");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
    }

    [TestMethod]
    public async Task CreateRecordWhenDoesHaveTwoOrMorePropertiesWithAttribute()
    {
        var test = @"using System;
using System.Text.Json.Serialization;

namespace ConsoleApplication1
{
    public class Person
    {
        [JsonPropertyName(""theName""), JsonIgnore]
        public string Name { get; set; }
        [JsonPropertyName(""theAge"")]
        [JsonIgnore]
        public int Age { get; set; }
    }
}";

        var fixtest = @"using System;
using System.Text.Json.Serialization;

namespace ConsoleApplication1
{
    public record Person([property: JsonPropertyName(""theName""), JsonIgnore] string Name, [property: JsonPropertyName(""theAge""), JsonIgnore] int Age);
}".ReplaceLineEndings();

        var expected = VerifyCS.Diagnostic("CRA0001")
            .WithSpan(6, 18, 6, 24)
            .WithMessage("Type name 'Person' can be a record.");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
    }
}