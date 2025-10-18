namespace Chickensoft.UMLGenerator;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models;

public static class AttributeExtensions
{
	public static ClassDiagramAttribute GetClassDiagramAttribute(this BaseHierarchy hierarchy)
	{
		var attribute = hierarchy.GetClassDiagramAttributeSyntax();
		var arguments = attribute?.ArgumentList?.Arguments;
		return new ClassDiagramAttribute()
		{
			UseVSCodePaths = GetAttributeBooleanValue(arguments, nameof(ClassDiagramAttribute.UseVSCodePaths)),
			ShowAllProperties = GetAttributeBooleanValue(arguments, nameof(ClassDiagramAttribute.ShowAllProperties)),
			ShowAllMethods = GetAttributeBooleanValue(arguments, nameof(ClassDiagramAttribute.ShowAllMethods))
		};
	}

	private static AttributeSyntax? GetClassDiagramAttributeSyntax(this BaseHierarchy hierarchy)
	{
		var attributeName = nameof(ClassDiagramAttribute).Replace("Attribute", "");
		var classDiagramAttribute = hierarchy.ContextList
			.Select(x => (x.Node as TypeDeclarationSyntax)?.AttributeLists.SelectMany(x => x.Attributes))
			.SelectMany(x => x)
			.FirstOrDefault(x => x.Name.ToString() == attributeName);

		return classDiagramAttribute;
	}

	public static bool HasClassDiagramAttribute(this BaseHierarchy hierarchy) => hierarchy.GetClassDiagramAttributeSyntax() != null;

	private static bool GetAttributeBooleanValue(SeparatedSyntaxList<AttributeArgumentSyntax>? arguments, string attributeName)
	{
		return arguments?.Any(arg =>
			arg.NameEquals is { } nameEquals &&
			nameEquals.Name.ToString() == attributeName &&
			arg.Expression is LiteralExpressionSyntax { Token.ValueText: "true" }) ?? false;
	}
}