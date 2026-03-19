namespace Chickensoft.UMLGenerator;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Helpers;
using Models;
using Modules;

public class PumlWriter
{
	public IReadOnlyDictionary<Type, IModule> Modules => _modules;

	private readonly NodeRegistry _nodeRegistry;
	private readonly Dictionary<Type, IModule> _modules;
	private readonly Dictionary<int, string> _paddingCache = new();

	public PumlWriter(NodeRegistry nodeRegistry)
	{
		_nodeRegistry = nodeRegistry;
		var interfaceType = typeof(IModule);

		var navTypes = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t =>
				t is { IsClass: true, IsAbstract: false } &&
				interfaceType.IsAssignableFrom(t));

		_modules = navTypes.Select(type => (IModule)Activator.CreateInstance(type)!).ToDictionary(x => x.GetType(), x => x);

		foreach (var module in _modules.Values)
		{
			module.InitModule(_nodeRegistry);
		}
	}

	public void GenerateHierarchy(DiagramNode node)
	{
		foreach (var module in _modules.Values)
		{
			var output = module.SetupModule(node, _nodeRegistry);
			if (output.Count > 0)
			{
				node.AddModule(module.GetType(), output);
			}
		}
	}

	private string GetPadding(int depth)
	{
		if (!_paddingCache.TryGetValue(depth, out var padding))
		{
			padding = new string(' ', depth * 4);
			_paddingCache[depth] = padding;
		}
		return padding;
	}

	private List<DiagramNode> GetChildrenToDraw(DiagramNode node)
	{
		var children = new List<DiagramNode>();
		var seen = new HashSet<DiagramNode>();

		foreach (var kvp in node.ModuleItems)
		{
			if (!_modules[kvp.Key].ShouldDrawChildren)
				continue;

			foreach (var item in kvp.Value)
			{
				var childNode = item.Node;
				if (childNode != null &&
				    childNode != node &&
				    childNode.ModuleItems.Count > 0 &&
				    seen.Add(childNode))
				{
					children.Add(childNode);
				}
			}
		}

		return children;
	}

	public string GetDiagram(DiagramNode node, int depth, int fileDepth, ClassDiagramAttribute attribute)
	{
		var sb = new StringBuilder(4096);
		BuildDiagramIterative(sb, node, depth, fileDepth, attribute, new HashSet<DiagramNode>());
		return sb.ToString();
	}

	private void BuildDiagramIterative(StringBuilder sb, DiagramNode node, int depth, int fileDepth, ClassDiagramAttribute attribute, HashSet<DiagramNode> visited)
	{
		if (!visited.Add(node))
			return;

		var childrenToDraw = GetChildrenToDraw(node);
		var typeDepth = childrenToDraw.Count > 0 ? fileDepth + 1 : fileDepth;

		var typeDefinition = GetTypeDefinition(node, depth, typeDepth, attribute);

		if (childrenToDraw.Count == 0)
		{
			sb.Append(typeDefinition);
			return;
		}

		var newFilePath = attribute.UseVSCodePaths ?
			NodeExtensions.GetVSCodePath(node.FullFilePath) :
			NodeExtensions.GetPathWithDepth(node.FilePath, depth);

		var packageType = node.HasScene ? "Scene" : "Type";

		var depthPadding = GetPadding(fileDepth);
		var relationPadding = GetPadding(typeDepth);

		// Write package header
		sb.AppendFormat("{0}package {1}-{2} [[{3}]] {{\n{4}\n",
			depthPadding, node.Name, packageType, newFilePath, typeDefinition);

		// Build children definitions
		foreach (var child in childrenToDraw)
		{
			BuildDiagramIterative(sb, child, depth, fileDepth + 1, attribute, visited);
			sb.Append('\n');
		}

		// Build relationships
		var onlyHasDI = node.ModuleItems.All(y => y.Key == typeof(DependencyInjectionModule));

		foreach (var child in childrenToDraw)
		{
			var memberRelationship = string.Empty;

			if (!onlyHasDI)
			{
				memberRelationship = node.SceneNode?.AllChildren
					.FirstOrDefault(c => c.Type == child.Name)?.Name ?? child.Name;
				memberRelationship = "::" + memberRelationship;
			}

			var arrow = child.ModuleItems.Count == 0 ? "-->" : "--->";

			sb.AppendFormat("{0}{1}{2} {3} {4}\n",
				relationPadding, node.Name, memberRelationship, arrow, child.Name);
		}

		sb.Append(depthPadding).Append('}');
	}

	private string GetTypeDefinition(DiagramNode node, int depth, int fileDepth, ClassDiagramAttribute classDiagramAttribute)
	{
		var sb = new StringBuilder(2048);
		var depthPadding = GetPadding(fileDepth);
		var depthPadding2 = GetPadding(fileDepth + 1);

		var useVsCodePaths = classDiagramAttribute.UseVSCodePaths;
		var hasScript = node.HasScript;

		var newFilePath = useVsCodePaths ?
			NodeExtensions.GetVSCodePath(hasScript ? node.FullScriptPath : node.FullFilePath) :
			NodeExtensions.GetPathWithDepth(hasScript ? node.ScriptPath : node.FilePath, depth);

		var fileType = hasScript ? "Script" : "Scene";
		var spotCharacter = hasScript ? "" : "<< (S,black) >>";

		// Write class header with file link
		sb.AppendFormat("{0}class {1} {2} {{\n{3}[[{4} {5}File]]",
			depthPadding, node.Name, spotCharacter, depthPadding2, newFilePath, fileType);

		// Group modules by order and build output
		var modulesByOrder = new SortedDictionary<int, List<(Type Type, List<string> Content)>>();

		foreach (var modulePair in node.ModuleItems)
		{
			var module = _modules[modulePair.Key];
			var order = module.Order;

			if (!modulesByOrder.TryGetValue(order, out var list))
			{
				list = new List<(Type, List<string>)>();
				modulesByOrder[order] = list;
			}

			var moduleStrings = module
				.InvokeModule(node, modulePair.Value, useVsCodePaths, depth)
				.ToList();

			var content = new List<string>(moduleStrings.Count + 1) { module.Title };
			content.AddRange(moduleStrings);

			list.Add((modulePair.Key, content));
		}

		// Write module content or empty class body
		if (modulesByOrder.Count > 0)
		{
			sb.Append('\n');
			bool firstOrderGroup = true;

			foreach (var orderGroup in modulesByOrder.Values)
			{
				if (!firstOrderGroup)
				{
					sb.AppendFormat("{0}--\n", depthPadding2);
				}
				firstOrderGroup = false;

				foreach (var (_, content) in orderGroup)
				{
					foreach (var line in content)
					{
						sb.Append(depthPadding2).Append(line).Append('\n');
					}
				}
			}
		}
		else
		{
			sb.Append("\n\n");
		}

		// Close class definition
		sb.Append(depthPadding).Append('}');

		return sb.ToString();
	}
}