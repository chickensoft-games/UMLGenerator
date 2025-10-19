## Pulling Data
1. We pull all the TSCN files that are imported as AdditionalFiles
2. We also grab all C# files that are within the project directory

## Linking
We first create a list of BaseHierarchy
These are then filled with
1. All the TSCN files as NodeHierarchy
2. All C# files as TypeHierarchy
   - If any of these are found to match a type with a Node, they are instead added to the existing NodeHierarchy

Then we link all these hierarchies together using the GenerateHierarchy method

1. All TSCN files are linked together first based on the children found within the TSCN, then it continues to draw relationships from the C# script
2. For the C# script, we link by properties and primary constructor parameters
   - We also check for any dependents and provisions
     - Provisions by checking for any IProvide<T>.Value methods, and grab the generic parameter
     - Dependents by checking for any properties that have the Dependent attribute
   - The way C# members are linked is by getting the type from the class itself and then seeing if the type exists within the node hierarchy list.

## Diagram Generation
We only generate a diagram for any classes that have the ClassDiagram Attribute

This then recursively goes through all the children of the objects to create a .puml file by doing the following

1. Create the representing type definition (This also outputs the properties of the script if it exists)
   - See getting type definition
2. If there are no children to draw, return
3. Otherwise, wrap it inside its own package
4. Recursively create the diagram for its children and join their strings together (Goes back to step 1 for the children)
5. Create relationships between its children
   - This grabs the previously outputted property names that are keyed by type, and uses them to point to the correct type.
6. Check whether we're a Node or a standalone script, and type the package accordingly
7. Then Compose it into a diagram and return the puml string.

## Getting Type Definition
This is used to formulate all the External Children (Nodes that don't have scripts), Properties, Methods, Provisions, and Dependents of a type
1. First, we create empty strings for all the types we want to show
2. Then get the script path based on the depth and type of diagram we want to generate
    - This is either going to be the path to the script or the path to the tscn if the script doesn't exist
3. Then we create a string for each category
   a. External Children
      - Get all children that aren't found in the interface property declarations
        - Gets the script for that child — Can be a c# file, a tscn file or a gdscript file
      - If it's a property, set string value to be the property name with a link to where it was declared
      - If it's a node, get the name of the first node that has the same type as the tscn file
      - Otherwise, use the name found in the hierarchy list
      - Then if it has a script/scene, append a link to the file along with the type of file
   b. Properties
      - Get all properties that were found in the interface property declarations
      - Put the name of that property along with a link to the declaration
      - If there exists a script for that property, append a link to the file along with the type of file
   c. Methods
      - Get all methods that were found in the interface method declarations
      - If the explicit interface identifier is found, make sure to include it
      - Put a link to the method along with a link to the declaration
   d. Provisions
      - Add all Provisions with a link to the declaration and a link to the script of the Provision
   e. Dependents
      - Add all Dependents with a link to the declaration and a link to the script of the Dependent
4. This then gets all put together within a class declaration
   - Script to the type itself gets put at the top, followed by dependency properties, provision methods, interface properties, interface proeprties, and then external children.