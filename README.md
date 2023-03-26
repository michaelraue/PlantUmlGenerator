# Plant UML Generator
## Intro
With this console app you can parse source code and generate [PlantUML](https://plantuml.com/) diagrams from it.

Currently supported are:
* C# parser
* Plant UML Class Diagram generator

The main focus is to support domain logic models driven by Domain Driven Design.

This project is inspired by https://github.com/pierre3/PlantUmlClassDiagramGenerator which is a great tool. Unfortunately a simple fork was not possible because under the hood this works vastly different, with the biggest difference being the use of a semantic model rather than a syntactic one.

## Usage
```
Description:
  Generates PlantUML class diagrams from C# code
Usage:
  PlantUmlGenerator <input-project> <output-dir> [options]
Arguments:
  <input-project>  The C# project for which to create a PlantUML class diagram.
  <output-dir>     The directory which is used to generate PlantUML code files
                   in.

Options:
  --excludes <names>             A list of namespaces or types which shall
                                 completely excluded in all diagrams []
  --clear                        If set the output directory will be cleaned if
                                 folders/files are already present [default:
                                 False]
  --noAssociations <namespaces>  A list of namespaces to which no associations
                                 are drawn to, to prevent clutter []
  --hide <namespaces>            A list of namespaces which shall not appear on
                                 diagrams of other namespaces []
  --version                      Show version information
  -?, -h, --help                 Show help and usage information
```

Example:

`PlantUmlGenerator C:\Project\Project.csproj D:\Project\doc\uml --clear --excludes Program Startup Helpers Common.Logging Common.Validation`

This command generates class diagrams for C# code of project `C:\Project\Project.csproj` in the output folder `D:\Project\doc\uml`, which will be cleared beforehand. Also 5 classes/namespaces are excluded.

Note: The given project must compile for the generator to work. If there are compile errors they will be printed on the console.

### Excluding classes

With the `--excludes` parameter a list of classes, namespaces or a mix of them can be given. In case of a namespace, all sub namespaces and classes are excluded.

## Requirements

### Version 1.1.0
* Let the user configure different background colors for namespaces

### Version 1.0.0
* Organize the classes in their namespaces
* Generate a PUML file for each class individually, but also aggregated views for each namespace and overall
* If the diagram of a specific class is shown it should also show all surrounding classes to easily see the context
* If a class inherits from `ValueObject`, `SingleValueObject`, `Entity`, `Aggregate` or `AggregateRoot`, consider these terms as the DDD counterparts and display stereotypes instead of real inheritance
  * Also records are always considered as a `Value Object`
* Display list types not as an association to the real list type, but as a `*` association to the generic type behind the list
  * If a dictionary type is used, use the `.Value` part as the underlying class an association should point to
* Let the user configure namespaces in which no associations go in (all properties of types of such a namespace get always printed as attributes)
* Let the user configure namespaces which shall never appear in diagrams of other namespaces
* Let the user configure the color of namespace box borders
  * This is done with a `_common.puml` file which gets generated in the output folder and will never be overwritten

### Backlog
* Support interfaces and structs
