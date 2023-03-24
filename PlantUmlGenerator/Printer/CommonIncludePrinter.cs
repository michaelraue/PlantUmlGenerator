using System.Text;

namespace PlantUmlGenerator.Printer;

public class CommonIncludePrinter
{
    public const string CommonConfigFileNameWithExtension = "_common.puml";
    private readonly string _fullOutputPath;

    public CommonIncludePrinter(DirectoryInfo outputDirectory)
    {
        _fullOutputPath = outputDirectory.FullName;
    }

    public async Task Print()
    {
        var filename = Path.Combine(_fullOutputPath, CommonConfigFileNameWithExtension);
        if (File.Exists(filename)) return;
        
        var content = new StringBuilder();
        content.AppendLine("This file is included in every other .puml file and will not be deleted/overwritten,");
        content.AppendLine("so you can use it for configuration of common things, like for example skinparams.");
        content.AppendLine();
        content.AppendLine("@startuml");
        content.AppendLine("@enduml");
        await File.WriteAllTextAsync(filename, content.ToString());
    }
}