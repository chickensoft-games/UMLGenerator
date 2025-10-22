namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using Models;

public class ModuleItem
{
	public BaseHierarchy? Hierarchy { get; set; }
	public string TypeName { get; set; }
	public string? Name { get; set; }
	public int LineNumber { get; set; }
}

public interface IModule
{
	int Order { get; }
	string Title { get; }
	void SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList);
	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, bool useVSCodePaths, int depth);
}

public enum ModuleOrder
{
	First = 0,
	Middle = 100,
	Last = 200,
}