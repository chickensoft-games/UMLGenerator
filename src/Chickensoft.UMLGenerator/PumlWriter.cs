namespace Chickensoft.UMLGenerator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;
using Models;
using PumlModules;

public class PumlWriter
{
	private readonly IDictionary<string, BaseHierarchy> _nodeHierarchyList;
	private readonly Dictionary<Type, IModule> _modules;

	public PumlWriter(IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		_nodeHierarchyList = nodeHierarchyList;
		var interfaceType = typeof(IModule);

		var navTypes = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t =>
				t is { IsClass: true, IsAbstract: false } &&
				interfaceType.IsAssignableFrom(t));

		_modules = navTypes.Select(type => (IModule)Activator.CreateInstance(type)!).ToDictionary(x => x.GetType(), x => x);
	}

	public void GenerateHierarchy(BaseHierarchy hierarchy)
	{
		foreach (var module in _modules.Values)
		{
			var output = module.SetupModule(hierarchy, _nodeHierarchyList);
			if (output.Count > 0)
			{
				hierarchy.AddModule(module.GetType(), output);
			}
		}
	}

	public string GetDiagram(BaseHierarchy hierarchy, int depth, int fileDepth, ClassDiagramAttribute attribute)
	{
		var childrenToDraw = hierarchy.ModuleItems
			.Where(x =>
				x.Key == typeof(PropertyModule) ||
				x.Key == typeof(NodeModule))
			.Select(x => x.Value)
			.SelectMany(x => x)
			.Select(x => x.Hierarchy)
			.Where(x => x?.ModuleItems.Any() ?? false).Distinct().ToList();

		var typeDepth = childrenToDraw.Count > 0 ? fileDepth + 1 : fileDepth;

		var typeDefinition = GetTypeDefinition(hierarchy, depth, typeDepth, attribute);

		if (childrenToDraw.Count == 0)
			return typeDefinition;

		var newFilePath = attribute.UseVSCodePaths ?
			HierarchyExtensions.GetVSCodePath(hierarchy.FullFilePath) :
			HierarchyExtensions.GetPathWithDepth(hierarchy.FilePath, depth);

		var childrenDefinitions = string.Join("\n",
			childrenToDraw.Select(x =>
				GetDiagram(x, depth, fileDepth + 1, attribute)
			)
		);

		var relationPadding = new string(' ', typeDepth * 4);

		var childrenRelationships = string.Join("\n",
			childrenToDraw.Select(x =>
			{
				var memberName = (hierarchy as NodeHierarchy)?
				                 .Node?
				                 .AllChildren
				                 .FirstOrDefault(node => node.Type == x.Name)?.Name
				                 ?? x.Name;

				return $"{relationPadding}{hierarchy.Name}::{memberName} {(x.ModuleItems.Count == 0 ? string.Empty : "-")}--> {x.Name}";
			})
		);

		var packageType = hierarchy switch
		{
			TypeHierarchy => "Type",
			NodeHierarchy => "Scene",
			_ => throw new NotImplementedException()
		};

		var depthPadding = new string(' ', fileDepth * 4);

		return
			$$"""
			  {{depthPadding}}package {{hierarchy.Name}}-{{packageType}} [[{{newFilePath}}]] {
			  {{depthPadding}}{{typeDefinition}}
			  {{childrenDefinitions}}
			  {{childrenRelationships}}
			  {{depthPadding}}
			  {{depthPadding}}}
			  """;
	}

	private string GetTypeDefinition(BaseHierarchy hierarchy, int depth, int fileDepth, ClassDiagramAttribute classDiagramAttribute)
	{
		var depthPadding = new string(' ', fileDepth * 4);
		var depthPadding2 = new string(' ', (fileDepth + 1) * 4);

		var useVsCodePaths = classDiagramAttribute.UseVSCodePaths;

		var outputList = new Dictionary<Type, List<string>>();
		var orderedModules = hierarchy.ModuleItems.OrderBy(x => _modules[x.Key].Order);

		foreach (var moduleItem in orderedModules)
		{
			var module = _modules[moduleItem.Key];

			var moduleString = module
				.InvokeModule(hierarchy, useVsCodePaths, depth)
				.ToList();

			List<string> finalString = ["", module.Title, ..moduleString];

			finalString = finalString.Select(x => depthPadding2 + x)
				.ToList();

			outputList.Add(moduleItem.Key, finalString);
		}

		var mergedList = outputList.Values.SelectMany(x => x);
		var moduleOutput = string.Join("\t\n", mergedList);
		var hasScript = hierarchy.ContextList.Any();

		var newFilePath = useVsCodePaths ?
			HierarchyExtensions.GetVSCodePath(hierarchy.FullFilePath) :
			HierarchyExtensions.GetPathWithDepth(hierarchy.FilePath, depth);

		var fileType = hasScript ? "Script" : "Scene";

		var spotCharacter = hasScript ? "" : "<< (S,black) >>";

		return
		$$"""

		{{depthPadding}}class {{hierarchy.Name}} {{spotCharacter}} {
		{{depthPadding2}}[[{{newFilePath}} {{fileType}}File]]
		{{depthPadding}}{{moduleOutput}}
		{{depthPadding}}}

		""";
	}
}