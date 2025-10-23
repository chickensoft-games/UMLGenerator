namespace Chickensoft.UMLGenerator.Models;

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

public class GenerationData
{
	public ImmutableArray<AdditionalText> TscnFiles { get; set; }
	public IEnumerable<GeneratorSyntaxContext> SyntaxContexts { get; set; }
	public string? ProjectDir { get; set; }
}