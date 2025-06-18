## Pulling Data
1. We pull all the TSCN files that are imported as AdditionalFiles
2. We also grab all C# files that are within the project directory

## Linking
We first create a list of BaseHierarchy
These are then filled with
1. All the TSCN files as NodeHierarchy
2. All C# files as TypeHierarchy
    1. If any of these are found to match a type with a Node, they are instead added to the existing NodeHierarchy

Then we link all these hierarchies together using the GenerateHierarchy method

1. All TSCN files are linked together first based on the children found within the TSCN, then by properties in the script (if it exists)
2. For just C# file, we only link by property

## Diagram Generation
We only generate a diagram for any hierarchy objects that have the ClassDiagram Attribute

This then recursively goes through all the children of the objects to create a .puml file by doing the following

1. Create the representing type definition
    1. This also outputs the properties of the script if it exists
2. Recursively create the diagram for its children
3. Create relationships between its children
4. Compose it into a diagram and return the puml string.