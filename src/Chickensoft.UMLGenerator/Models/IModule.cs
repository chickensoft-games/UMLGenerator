namespace Chickensoft.UMLGenerator.Models;

using System.Collections.Generic;

public class ModuleItem
{
	public DiagramNode? Node { get; set; }
	public string? TypeName { get; set; }
	public string? Name { get; set; }
	public int LineNumber { get; set; }
}

public interface IModule
{
	int Order { get; }
	string Title { get; }
	bool ShouldDrawChildren { get; }
	List<ModuleItem> SetupModule(DiagramNode node, NodeRegistry nodeRegistry);
	IEnumerable<string> InvokeModule(DiagramNode node, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth);
	void InitModule(NodeRegistry nodeRegistry);
}

public enum ModuleOrder
{
	Ignore = -1,
	First = 0,
	Middle = 100,
	Last = 200
}