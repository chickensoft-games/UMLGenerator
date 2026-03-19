namespace Chickensoft.UMLGenerator;

using System.IO;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
					ProjectDir = x.Right.NormalizePath()
				}
			);

		context.RegisterImplementationSourceOutput(finalProvider, GenerateDiagram);
	}

	private void GenerateDiagram(SourceProductionContext context, GenerationData data)
	{
		var nodeRegistry = new NodeRegistry();

		//Look at all TSCN's in the project which are marked as AdditionalFiles
		foreach (var additionalText in data.TscnFiles)
		{
			var tscnContent = additionalText.GetText(context.CancellationToken)?.ToString();
			if (string.IsNullOrWhiteSpace(tscnContent))
				continue;

			var listener = TscnHelpers.RunTscnBaseListener(tscnContent!, context.ReportDiagnostic, additionalText.Path);
			if (listener.RootNode == null)
				continue;

			var fullScenePath = additionalText.Path.NormalizePath();
			var fullScriptPath = Path.Combine(data.ProjectDir ?? "", listener.Script?.Path.Replace("res://", "") ?? "");

			var node = new DiagramNode(data);
			node.SetSceneComponent(listener.RootNode, fullScenePath, fullScriptPath);

			nodeRegistry.AddBySceneName(node.Name, node);
		}

		//Look at all TypedFiles
		foreach (var syntaxContextGrouping in data.SyntaxContexts
			         .GroupBy(x => x.SemanticModel.SyntaxTree.FilePath))
		{
			var syntaxContext = syntaxContextGrouping.FirstOrDefault(x => x.Node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax);
			if (syntaxContext.Node is not TypeDeclarationSyntax)
				continue;

			var fullScriptPath = syntaxContextGrouping.Key.NormalizePath();

			if (nodeRegistry.TryGetByFilePath(fullScriptPath, out var existingNode))
			{
				existingNode.SetScriptComponent(syntaxContext, fullScriptPath);

				if (existingNode.FullyQualifiedName != null)
					nodeRegistry.AddByFQN(existingNode.FullyQualifiedName, existingNode);
			}
			else
			{
				var node = new DiagramNode(data);
				node.SetScriptComponent(syntaxContext, fullScriptPath);

				if (node.FullyQualifiedName != null)
					nodeRegistry.AddByFQN(node.FullyQualifiedName, node);
			}
		}

		var pumlWriter = new PumlWriter(nodeRegistry);

		foreach (var node in nodeRegistry.AllNodes)
		{
			pumlWriter.GenerateHierarchy(node);
		}

		var nodesWithAttribute = nodeRegistry.AllNodes.Where(x => x.HasClassDiagramAttribute()).ToList();

		foreach (var node in nodesWithAttribute)
		{
			var fileName = node.Name + ".g.puml";
			var filePath = Path.Combine(Path.GetDirectoryName(node.FilePath) ?? string.Empty, fileName);

			var depth = filePath.TrimStart('\\', '/').Split('\\', '/').Length - 1;
			var classDiagramAttribute = node.GetClassDiagramAttribute();

			string diagramString;

			if (classDiagramAttribute.GetAllTopLevelNodes)
			{
				var referencedNodes = new HashSet<DiagramNode>();
				foreach (var baseNode in nodeRegistry.AllNodes)
				{
					foreach (var moduleItem in baseNode.ModuleItems)
					{
						if (pumlWriter.Modules[moduleItem.Key].ShouldDrawChildren)
						{
							foreach (var item in moduleItem.Value)
							{
								if (item.Node != null)
									referencedNodes.Add(item.Node);
							}
						}
					}
				}

				var topLevelNodes = nodeRegistry.AllNodes
					.Where(topLevelNode => topLevelNode.ModuleItems.Count != 0 && !referencedNodes.Contains(topLevelNode))
					.OrderBy(x => x.Name)
					.ToList();

				var sb = new StringBuilder(topLevelNodes.Count * 1024);
				for (var i = 0; i < topLevelNodes.Count; i++)
				{
					if (i > 0) sb.Append('\n');
					sb.Append(pumlWriter.GetDiagram(topLevelNodes[i], depth, 0, classDiagramAttribute));
				}

				diagramString = sb.ToString();
			}
			else
			{
				diagramString = pumlWriter.GetDiagram(node, depth, 0, classDiagramAttribute);
			}

			var destFile = Path.Combine(data.ProjectDir!, filePath.TrimStart('/', '\\'));

			var source = string.Concat("@startuml\n\n", diagramString, "\n\n@enduml");
			File.WriteAllText(destFile, source);
		}

	}
}