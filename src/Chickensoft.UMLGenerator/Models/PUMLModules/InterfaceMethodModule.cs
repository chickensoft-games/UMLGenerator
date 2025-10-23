namespace Chickensoft.UMLGenerator.PumlModules;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public class InterfaceMethodModule : IModule
{
	public int Order => (int)ModuleOrder.Last;
	public string Title => "[Interface Methods]";
	public List<ModuleItem> SetupModule(BaseHierarchy hierarchy, IDictionary<string, BaseHierarchy> nodeHierarchyList)
	{
		var baseTypeSyntax = hierarchy.TypeSyntax;
		if (baseTypeSyntax == null)
			return [];

		var methodDeclarations =
			from interfaceMember in hierarchy.InterfaceSyntax?.Members ?? []
			from typeMember in hierarchy.TypeSyntax?.Members ?? []
			where typeMember is MethodDeclarationSyntax typeMethod &&
			      interfaceMember is MethodDeclarationSyntax interfaceMethod &&
			      typeMethod.Identifier.Value == interfaceMethod.Identifier.Value
			orderby (typeMember as MethodDeclarationSyntax)?.Identifier.ValueText
			select typeMember as MethodDeclarationSyntax;

		var items = new List<ModuleItem>();

		foreach (var ctx in methodDeclarations)
		{
			items.Add(new ModuleItem
			{
				Name = ctx.Identifier.ToString(),
				LineNumber = ctx.GetLineNumber()
			});
		}

		return items;
	}

	public IEnumerable<string> InvokeModule(BaseHierarchy hierarchy, List<ModuleItem> moduleItems, bool useVSCodePaths, int depth)
	{
		var parentScriptPath = hierarchy.GetScriptPath(useVSCodePaths, depth);
		foreach (var module in moduleItems)
		{
			yield return $"[[{parentScriptPath}:{module.LineNumber} {module.Name}()]]";
		}
	}
}