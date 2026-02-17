namespace Chickensoft.UMLGenerator.Models;

using System.Collections.Generic;
using System.Linq;
using Helpers;
using Microsoft.CodeAnalysis;

public class ClassNode(GeneratorSyntaxContext context, GenerationData data) : BaseNode(data)
{
	public override GeneratorSyntaxContext? Context { get; set; } = context;
	public override string FullFilePath => context.SemanticModel.SyntaxTree.FilePath.NormalizePath();
	public override string FullScriptPath => FullFilePath;
}