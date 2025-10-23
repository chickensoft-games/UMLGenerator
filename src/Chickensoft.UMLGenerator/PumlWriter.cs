namespace Chickensoft.UMLGenerator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;
using Models;
using Modules;

public class PumlWriter
{
	private readonly IDictionary<string, BaseNode> _sceneNodeList;
	private readonly Dictionary<Type, IModule> _modules;

	public PumlWriter(IDictionary<string, BaseNode> sceneNodeList)
	{
		_sceneNodeList = sceneNodeList;
		var interfaceType = typeof(IModule);

		var navTypes = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t =>
				t is { IsClass: true, IsAbstract: false } &&
				interfaceType.IsAssignableFrom(t));

		_modules = navTypes.Select(type => (IModule)Activator.CreateInstance(type)!).ToDictionary(x => x.GetType(), x => x);
	}

	public void GenerateHierarchy(BaseNode node)
	{
		foreach (var module in _modules.Values)
		{
			var output = module.SetupModule(node, _sceneNodeList);
			if (output.Count > 0)
			{
				node.AddModule(module.GetType(), output);
			}
		}
	}

	public string GetDiagram(BaseNode node, int depth, int fileDepth, ClassDiagramAttribute attribute)
	{
		var childrenToDraw = node.ModuleItems
			.Where(x =>
				(x.Key == typeof(PropertyModule) ||
				x.Key == typeof(InterfaceModule) ||
				x.Key == typeof(NodeModule)) &&
				x.Value.All(y => y.Node != node))
			.Select(x => x.Value)
			.SelectMany(x => x)
			.Select(x => x.Node)
			.Where(x => x?.ModuleItems.Any() ?? false).Distinct().ToList();

		var typeDepth = childrenToDraw.Count > 0 ? fileDepth + 1 : fileDepth;

		var typeDefinition = GetTypeDefinition(node, depth, typeDepth, attribute);

		if (childrenToDraw.Count == 0)
			return typeDefinition;

		var newFilePath = attribute.UseVSCodePaths ?
			NodeExtensions.GetVSCodePath(node.FullFilePath) :
			NodeExtensions.GetPathWithDepth(node.FilePath, depth);

		var childrenDefinitions = string.Join("\n",
			childrenToDraw.Select(x =>
				GetDiagram(x, depth, fileDepth + 1, attribute)
			)
		);

		var relationPadding = new string(' ', typeDepth * 4);

		var childrenRelationships = string.Join("\n",
			childrenToDraw.Select(x =>
			{
				var memberName = (node as SceneNode)?
				                 .Node?
				                 .AllChildren
				                 .FirstOrDefault(node => node.Type == x.Name)?.Name
				                 ?? x.Name;

				return $"{relationPadding}{node.Name}::{memberName} {(x.ModuleItems.Count == 0 ? string.Empty : "-")}--> {x.Name}";
			})
		);

		var packageType = node switch
		{
			ClassNode => "Type",
			SceneNode => "Scene",
			_ => throw new NotImplementedException()
		};

		var depthPadding = new string(' ', fileDepth * 4);

		return
			$$"""
			  {{depthPadding}}package {{node.Name}}-{{packageType}} [[{{newFilePath}}]] {
			  {{typeDefinition}}
			  {{childrenDefinitions}}
			  {{childrenRelationships}}
			  {{depthPadding}}}
			  """;
	}

	private string GetTypeDefinition(BaseNode node, int depth, int fileDepth, ClassDiagramAttribute classDiagramAttribute)
	{
		var depthPadding = new string(' ', fileDepth * 4);
		var depthPadding2 = new string(' ', (fileDepth + 1) * 4);

		var useVsCodePaths = classDiagramAttribute.UseVSCodePaths;

		var outputList = new Dictionary<Type, List<string>>();
		var orderedModules = node.ModuleItems.OrderBy(x => _modules[x.Key].Order);

		foreach (var modulePair in orderedModules)
		{
			var module = _modules[modulePair.Key];
			var moduleItems = modulePair.Value;

			var moduleString = module
				.InvokeModule(node, moduleItems, useVsCodePaths, depth)
				.ToList();

			List<string> finalString = [module.Title, ..moduleString];

			finalString = finalString.Select(x => depthPadding2 + x)
				.ToList();

			outputList.Add(modulePair.Key, finalString);
		}

		var mergedList = outputList
			.GroupBy(x => _modules[x.Key].Order)
			.Select(x =>
			{
				var result = x
					.ToList()
					.SelectMany(y => y.Value);
				var join = string.Join("\n", result);
				return join;
			});

		var moduleOutput = string.Join($"\n{depthPadding2}--\n", mergedList);
		var hasScript = node.ContextList.Any();

		var newFilePath = useVsCodePaths ?
			NodeExtensions.GetVSCodePath(hasScript ? node.FullScriptPath : node.FullFilePath) :
			NodeExtensions.GetPathWithDepth(hasScript ? node.ScriptPath : node.FilePath, depth);

		var fileType = hasScript ? "Script" : "Scene";

		var spotCharacter = hasScript ? "" : "<< (S,black) >>";

		return
		$$"""
		{{depthPadding}}class {{node.Name}} {{spotCharacter}} {
		{{depthPadding2}}[[{{newFilePath}} {{fileType}}File]]
		{{moduleOutput}}
		{{depthPadding}}}
		""";
	}
}