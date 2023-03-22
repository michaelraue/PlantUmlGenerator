using System.CommandLine;
using PlantUmlGenerator.Printer;
using PlantUmlGenerator.Reader.CSharp;

var inputProjectArgument = new Argument<FileInfo?>(
    name: "input-project",
    description: "The C# project for which to create a PlantUML class diagram.",
    parse: result =>
    {
        var path = result.Tokens.SingleOrDefault()?.Value;
        if (!File.Exists(path))
        {
            result.ErrorMessage = "Input project file does not exist";
            return null;
        }

        if (Path.GetExtension(path) != ".csproj")
        {
            result.ErrorMessage = "Input project must be a C# project (.csproj)";
            return null;
        }

        return new FileInfo(path);
    });
var outputDirectoryArgument = new Argument<DirectoryInfo?>(
    name: "output-dir",
    description: "The directory which is used to generate PlantUML code files in.",
    parse: result =>
    {
        var path = result.Tokens.SingleOrDefault()?.Value;
        if (path is null)
        {
            result.ErrorMessage = "Output directory must be set";
            return null;
        }

        return new DirectoryInfo(path);
    });
var excludesOption = new Option<string[]>(
    name: "--excludes",
    description: "A list of namespaces or types which shall not be used for diagram generation",
    getDefaultValue: Array.Empty<string>)
    { AllowMultipleArgumentsPerToken = true };
var clearOutputDirectoryOption = new Option<bool>(
    name: "--clear",
    description: "If set the output directory will be cleaned if folders/files are already present",
    getDefaultValue: () => false);

var rootCommand = new RootCommand("Generates PlantUML class diagrams from C# code");
rootCommand.AddArgument(inputProjectArgument);
rootCommand.AddArgument(outputDirectoryArgument);
rootCommand.AddOption(excludesOption);
rootCommand.AddOption(clearOutputDirectoryOption);

rootCommand.SetHandler(async (inputProject, outputDirectory, excludes, clearOutputDirectory) =>
{
    var reader = new CSharpReader(inputProject!, excludes);
    var printer = new PumlPrinter(outputDirectory!, clearOutputDirectory);

    var puml = await reader.Read();
    await printer.PrintPuml(puml);
}, inputProjectArgument, outputDirectoryArgument, excludesOption, clearOutputDirectoryOption);

await rootCommand.InvokeAsync(args);