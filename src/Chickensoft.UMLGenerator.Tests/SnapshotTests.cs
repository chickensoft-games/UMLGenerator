namespace Chickensoft.UMLGenerator.Tests;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmptyFiles;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Utils;
using VerifyTests;
using VerifyXunit;
using Xunit;

public class SnapshotTests
{
	static SnapshotTests()
	{
		FileExtensions.AddTextExtension("puml");
	}

	public static IEnumerable<object[]> TestFolderPaths = 
		Directory.GetDirectories("./TestCases").Select(x => new object[] {x});
	
	[Theory]
	[MemberData(nameof(TestFolderPaths))]
	public async Task VerifyTestCases(string testFolderPath)
	{
		var additionalTexts = new List<AdditionalText>();
		var csharpFiles = new List<SyntaxTree>();
		
		foreach (var filePath in Directory.GetFiles(testFolderPath, "*.cs", SearchOption.AllDirectories))
		{
			var syntaxTree = CSharpSyntaxTree.ParseText(
				await File.ReadAllTextAsync(filePath), 
				path: Path.GetFullPath(filePath));
			csharpFiles.Add(syntaxTree);
		}
		
		foreach (var filePath in Directory.GetFiles(testFolderPath, "*.tscn", SearchOption.AllDirectories))
		{
			var text = await File.ReadAllTextAsync(filePath);
			additionalTexts.Add(new TestAdditionalFile(filePath, text));
		}
		
		// Create an instance of the source generator.
		var generator = new UMLGenerator();

		// Source generators should be tested using 'GeneratorDriver'.
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Add the additional file separately from the compilation.
		driver = driver.AddAdditionalTexts([..additionalTexts]);

		// To run generators, we can use an empty compilation.
		var compilation = CSharpCompilation.Create(
			nameof(SnapshotTests),
			syntaxTrees: csharpFiles);

		driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

		foreach (var pumlPath in Directory.GetFiles(testFolderPath, "*.g.puml", SearchOption.AllDirectories))
		{
			var testName = $"{Path.GetFileName(testFolderPath)}_{Path.GetFileName(pumlPath).Replace(".g.puml", "")}";
			var settings = new VerifySettings();
			settings.UseDirectory("Snapshots");
			settings.UseFileName(testName);
			
			var text = await File.ReadAllTextAsync(pumlPath);
			await Verifier.Verify(text, extension: "puml", settings: settings);
		}
	}
}