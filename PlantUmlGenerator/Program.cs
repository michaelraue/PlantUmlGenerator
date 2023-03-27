using System.CommandLine;
using PlantUmlGenerator.Printer;
using PlantUmlGenerator.Printer.Options;
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
    description: "A list of namespaces or types which shall completely excluded in all diagrams",
    getDefaultValue: Array.Empty<string>)
{
    AllowMultipleArgumentsPerToken = true,
    Arity = ArgumentArity.OneOrMore,
    ArgumentHelpName = "names",
};
var clearOutputDirectoryOption = new Option<bool>(
    name: "--clear",
    description: "If set the output directory will be cleaned if folders/files are already present",
    getDefaultValue: () => false);
var noAssociationsOption = new Option<string[]>(
    name: "--noAssociations",
    description: "A list of namespaces to which no associations are drawn to, to prevent clutter",
    getDefaultValue: Array.Empty<string>)
{
    AllowMultipleArgumentsPerToken = true,
    Arity = ArgumentArity.OneOrMore,
    ArgumentHelpName = "namespaces",
};
var hideOption = new Option<string[]>(
    name: "--hide",
    description: "A list of namespaces which shall not appear on diagrams of other namespaces",
    getDefaultValue: Array.Empty<string>)
{
    AllowMultipleArgumentsPerToken = true,
    Arity = ArgumentArity.OneOrMore,
    ArgumentHelpName = "namespaces",
};
var topLevelNamespaceOption = new Option<string?>(
    name: "--topLevelNamespace",
    description: "The top level namespace to consider for PlantUML, if it differs from the project name",
    getDefaultValue: () => null)
{
    ArgumentHelpName = "namespace",
};
    
var rootCommand = new RootCommand("Generates PlantUML class diagrams from C# code");
rootCommand.AddArgument(inputProjectArgument);
rootCommand.AddArgument(outputDirectoryArgument);
rootCommand.AddOption(excludesOption);
rootCommand.AddOption(clearOutputDirectoryOption);
rootCommand.AddOption(noAssociationsOption);
rootCommand.AddOption(hideOption);
rootCommand.AddOption(topLevelNamespaceOption);

rootCommand.SetHandler(async (inputProject, outputDirectory, excludes, clearOutputDirectory, noAssociations, hide, topLevelNamespace) =>
    {
        var reader = new CSharpReader(inputProject!, excludes, topLevelNamespace);
        var printerOptions = new PumlPrinterOptions
        {
            NamespacesToDrawNoAssociationsTo = noAssociations,
            NamespacesToHideInOtherNamespaces = hide,
        };
        var printer = new PumlPrinter(outputDirectory!, clearOutputDirectory, printerOptions);

        var puml = await reader.Read();
        await printer.PrintPuml(puml);
    }, inputProjectArgument, outputDirectoryArgument, excludesOption, clearOutputDirectoryOption, noAssociationsOption,
    hideOption, topLevelNamespaceOption);

await rootCommand.InvokeAsync(args);