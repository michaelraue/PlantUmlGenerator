using System.Text;
using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public class CommonIncludePrinter
{
    public const string CommonConfigFileNameWithExtension = "_common.puml";
    private readonly string _fullOutputPath;
    private readonly PumlProject _project;

    public CommonIncludePrinter(DirectoryInfo outputDirectory, PumlProject project)
    {
        _fullOutputPath = outputDirectory.FullName;
        _project = project;
    }

    public async Task Print()
    {
        var filename = Path.Combine(_fullOutputPath, CommonConfigFileNameWithExtension);
        if (File.Exists(filename)) return;
        
        var content = new StringBuilder();
        content.AppendLine("This file is included in other .puml files and will not be deleted/overwritten,");
        content.AppendLine("so you can use it for configuration of common things, like for example skinparams.");
        content.AppendLine("Also each namespace can be configured separately, for example with a color.");
        content.AppendLine();
        content.AppendLine("@startuml");
        content.AppendLine("@enduml");

        foreach (var @namespace in _project.GetAllNamespaces()
                     .SelectMany(PumlPrinter.GetAllSubNamespacePermutations)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Order()
                     .Distinct())
        {
            content.AppendLine();
            content.AppendLine($"@startuml(id={@namespace})");
            content.AppendLine($"namespace {@namespace} {{}}");
            content.AppendLine("@enduml");
        }
        
        await File.WriteAllTextAsync(filename, content.ToString());
    }
}