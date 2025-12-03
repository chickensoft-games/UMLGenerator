namespace Chickensoft.UMLGenerator.Tests;

using Chickensoft.UMLGenerator;

[ClassDiagram]
public partial class Test
{
	public Test(Test2 test2, Test3 test3, Test4 test4) { }
	public Test(Test2 test2, Test3 test3) { }
	public Test(Test2 test2) { }
	public Test() { }
}