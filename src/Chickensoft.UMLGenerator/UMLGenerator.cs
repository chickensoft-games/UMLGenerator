namespace Chickensoft.UMLGenerator;

using System.IO;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

[Generator]
public class UMLGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var tscnProvider = context.AdditionalTextsProvider
			.Where(f => Path.GetExtension(f.Path).Equals(".tscn", StringComparison.OrdinalIgnoreCase))
			.Collect();

		var projectDirProvider = context.AnalyzerConfigOptionsProvider
			.Select((optionsProvider, _) =>
			{
				optionsProvider.GlobalOptions
					.TryGetValue("build_property.projectdir", out var projectDir);

				return projectDir ?? Directory.GetCurrentDirectory();
			});
		
		var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
			(node, _) => node is TypeDeclarationSyntax,
			(syntaxContext, _) => syntaxContext)
			.Combine(projectDirProvider)
			.Where((x) => x.Left.Node.SyntaxTree.FilePath.Contains(x.Right))
			.Select((x, _) => x.Left)
			.Collect();

		var finalProvider = tscnProvider
			.Combine(syntaxProvider)
			.Combine(projectDirProvider)
			.Select((x, _) => 
				new GenerationData
				{
					TscnFiles = x.Left.Left,
					SyntaxContexts =  x.Left.Right,
					ProjectDir = x.Right
				}
			);

		context.RegisterImplementationSourceOutput(finalProvider, GenerateDiagram);
	}

	private void GenerateDiagram(SourceProductionContext context, GenerationData data)
	{
		Dictionary<string, BaseHierarchy> hierarchyList = [];
		//Look at all TSCN's in the project which are marked as AdditionalFiles
		foreach (var additionalText in data.TscnFiles)
		{
			var tscnContent = additionalText.GetText(context.CancellationToken)?.ToString();
			if (string.IsNullOrWhiteSpace(tscnContent))
				continue;

			var listener = TscnHelpers.RunTscnBaseListener(tscnContent!, context.ReportDiagnostic, additionalText.Path);

			var nodeHierarchy = new NodeHierarchy(listener, additionalText, data);
			hierarchyList.Add(nodeHierarchy.Name, nodeHierarchy);
		}

		//Look at all TypedFiles
		foreach (var syntaxContextGrouping in data.SyntaxContexts
			         .GroupBy(x => x.SemanticModel.SyntaxTree.FilePath))
		{
			var name = Path.GetFileNameWithoutExtension(syntaxContextGrouping.Key);
			if (!hierarchyList.TryGetValue(name, out var nodeHierarchy))
			{
				var typeHierarchy = new TypeHierarchy(syntaxContextGrouping, data);
				hierarchyList.Add(name, typeHierarchy);
			}
			else
			{
				var nodeContextList = nodeHierarchy.ContextList;
				nodeContextList.AddRange(syntaxContextGrouping.ToList());
			}
		}
		
		foreach (var hierarchy in hierarchyList.Values)
		{
			hierarchy.GenerateHierarchy(hierarchyList);
		}

		var nodesWithAttribute = hierarchyList.Values.Where(x => x.HasClassDiagramAttribute());

		foreach (var node in nodesWithAttribute)
		{
			var fileName = node.Name + ".g.puml";
			var filePath = Path.Combine(Path.GetDirectoryName(node.FilePath) ?? string.Empty, fileName);

			var depth = filePath.Split('\\', '/').Length - 1;
			var classDiagramAttribute = node.GetClassDiagramAttribute();

			var source =
			$"""
			@startuml
			{node.GetDiagram(depth, classDiagramAttribute)}
			@enduml
			""";
			var destFile = Path.Combine(data.ProjectDir!, filePath);
			
			File.WriteAllText(destFile, source);
		}
		
	}
}