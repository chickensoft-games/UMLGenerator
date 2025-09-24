namespace Chickensoft.UMLGenerator;

using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ClassDiagramAttribute : Attribute
{
	/// <summary>
	/// Changes the paths so that they're generated as full paths and uses the
	/// vscode:// url protocol. This allows them to be used with the VSCode plugin.
	/// </summary>
	public bool UseVSCodePaths { get; set; }

	/// <summary>
	/// Makes it so all properties are shown in the diagram instead of
	/// just the ones that are in the interface or are considered children.
	/// </summary>
	public bool ShowAllProperties { get; set; }

	/// <summary>
	/// Makes it so all methods are shown in the diagram instead of
	/// just the ones in the interface.
	/// </summary>
	public bool ShowAllMethods { get; set; }
}