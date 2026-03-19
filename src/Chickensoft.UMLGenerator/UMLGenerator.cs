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
		var sceneNodeList = new Dictionary<string, SceneNode>(StringComparer.OrdinalIgnoreCase);
		var classNodeList = new Dictionary<string, ClassNode>(StringComparer.OrdinalIgnoreCase);

		//Look at all TSCN's in the project which are marked as AdditionalFiles
		foreach (var additionalText in data.TscnFiles)
		{
			var tscnContent = additionalText.GetText(context.CancellationToken)?.ToString();
			if (string.IsNullOrWhiteSpace(tscnContent))
				continue;

			var listener = TscnHelpers.RunTscnBaseListener(tscnContent!, context.ReportDiagnostic, additionalText.Path);

			var sceneNode = new SceneNode(listener, additionalText, data);
			if(sceneNode.FullFilePath != null)
				sceneNodeList.Add(sceneNode.FullFilePath, sceneNode);
		}

		//Look at all TypedFiles
		foreach (var syntaxContextGrouping in data.SyntaxContexts
			         .GroupBy(x => x.SemanticModel.SyntaxTree.FilePath))
		{
			var syntaxContext = syntaxContextGrouping.FirstOrDefault(x => x.Node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax);
			if (syntaxContext.Node is not TypeDeclarationSyntax)
				continue;

			if (sceneNodeList.Values.FirstOrDefault(x => x.FullScriptPath == syntaxContextGrouping.Key) is { } sceneNode)
			{
				sceneNode.Context = syntaxContext;
			}
			else
			{
				var classNode = new ClassNode(syntaxContext, data);
				classNodeList.Add(classNode.FullyQualifiedName!, classNode);
			}
		}

		var hierarchyList = new Dictionary<string, BaseNode>(sceneNodeList.Count + classNodeList.Count, StringComparer.OrdinalIgnoreCase);

		foreach (var keyValuePair in sceneNodeList)
			hierarchyList.Add(keyValuePair.Key, keyValuePair.Value);
		foreach (var keyValuePair in classNodeList)
			hierarchyList.Add(keyValuePair.Key, keyValuePair.Value);

		var pumlWriter = new PumlWriter(hierarchyList);

		foreach (var hierarchy in hierarchyList.Values)
		{
			pumlWriter.GenerateHierarchy(hierarchy);
		}

		var nodesWithAttribute = hierarchyList.Values.Where(x => x.HasClassDiagramAttribute()).ToList();

		foreach (var node in nodesWithAttribute)
		{
			var fileName = node.Name + ".g.puml";
			var filePath = Path.Combine(Path.GetDirectoryName(node.FilePath) ?? string.Empty, fileName);

			var depth = filePath.TrimStart('\\', '/').Split('\\', '/').Length - 1;
			var classDiagramAttribute = node.GetClassDiagramAttribute();

			string diagramString;

			if (classDiagramAttribute.GetAllTopLevelNodes)
			{
				var referencedNodes = new HashSet<BaseNode>();
				foreach (var baseNode in hierarchyList.Values)
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

				var topLevelNodes = hierarchyList.Values
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