namespace Chickensoft.UMLGenerator.Tests;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
		Directory.GetDirectories(CurrentDir("./TestCases")).Select(x => new object[] {x});

	[Theory, MemberData(nameof(TestFolderPaths))]
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
		
		var generator = new UMLGenerator();

		var driver = CSharpGeneratorDriver
			.Create(generator)
			.AddAdditionalTexts([..additionalTexts])
			.WithUpdatedAnalyzerConfigOptions(
				new ConfigOptionsProvider(
					new ConfigOptions(
						new Dictionary<string, string>
						{
							{
								"build_property.projectdir", CurrentDir("./")
							}
						}
					)
				)
			);

		var compilation = CSharpCompilation.Create(
			nameof(SnapshotTests),
			syntaxTrees: csharpFiles);

		driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

		var generatedPumls = Directory.GetFiles(testFolderPath, "*.g.puml", SearchOption.AllDirectories);
		
		if(generatedPumls.Length == 0)
			Assert.Fail("No generated files were found.");

		foreach (var pumlPath in generatedPumls)
		{
			var testName = $"{Path.GetFileName(testFolderPath)}_{Path.GetFileName(pumlPath).Replace(".g.puml", "")}";
			var settings = new VerifySettings();
			settings.UseDirectory("Snapshots");
			settings.UseFileName(testName);
			
			var text = await File.ReadAllTextAsync(pumlPath);
			await Verifier.Verify(text, extension: "puml", settings: settings);
		}
	}
	
	public static string CurrentDir(
		string relativePathInProject,
		[CallerFilePath] string? callerFilePath = null
	) => Path.GetFullPath(Path.Join(
		Path.GetDirectoryName(callerFilePath),
		relativePathInProject
	));
}