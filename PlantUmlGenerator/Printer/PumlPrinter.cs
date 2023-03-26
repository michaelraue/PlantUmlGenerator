using PlantUmlGenerator.Model;
using PlantUmlGenerator.Printer.Options;

namespace PlantUmlGenerator.Printer;

public class PumlPrinter : IPumlPrinter
{
    public const string IndentationString = "    ";

    private readonly DirectoryInfo _outputDirectory;
    private readonly bool _clearOutputDirectory;
    private readonly PumlPrinterOptions _options;

    public PumlPrinter(
        DirectoryInfo outputDirectory,
        bool clearOutputDirectory,
        PumlPrinterOptions options)
    {
        _outputDirectory = outputDirectory;
        _clearOutputDirectory = clearOutputDirectory;
        _options = options;
    }
    
    public async Task PrintPuml(PumlProject project)
    {
        CreateOutputDirectoryIfNecessary();
        var includesPrinter = new IncludesPrinter(_outputDirectory);
        foreach (var @class in project.Classes)
        {
            var folder = CreateNamespaceFolder(_outputDirectory, @class);
            await Print(@class, folder, includesPrinter,
                (c, writer) => new ClassPrinter(c, writer, project, _options.NamespacesToDrawNoAssociationsTo,
                    _options.NamespacesToHideInOtherNamespaces));
        }

        foreach (var enumeration in project.Enumerations)
        {
            var folder = CreateNamespaceFolder(_outputDirectory, enumeration);
            await Print(enumeration, folder, includesPrinter,
                (e, writer) => new EnumerationPrinter(e, writer, project, _options.NamespacesToDrawNoAssociationsTo));
        }

        await includesPrinter.Print();
        await new CommonIncludePrinter(_outputDirectory, project).Print();
    }

    public static IEnumerable<string> GetAllSubNamespacePermutations(string @namespace)
    {
        var parts = @namespace.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i <= parts.Length; i++)
        {
            var result = string.Join(".", parts[..i]);
            if (!string.IsNullOrWhiteSpace(result))
            {
                yield return result;
            }
        }
    }

    private static async Task Print<T>(
        T obj,
        DirectoryInfo folder,
        IncludesPrinter includesPrinter,
        Func<T, TextWriter, PrinterForNamedObjects<T>> createPrinter) where T : NamespacedObject
    {
        var outputFile = Path.ChangeExtension(Path.Combine(folder.FullName, obj.Name), "puml");
        includesPrinter.Add(new FileInfo(outputFile));
        await using var fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
        await using var writer = new StreamWriter(fileStream);
        var printer = createPrinter(obj, writer);
        await printer.Print();
    }

    private static DirectoryInfo CreateNamespaceFolder(DirectoryInfo root, NamespacedObject @class)
    {
        var folder = new DirectoryInfo(Path.Combine(root.FullName, @class.Namespace.Replace('.', Path.DirectorySeparatorChar)));
        if (!folder.Exists)
        {
            folder.Create();
        }

        return folder;
    }

    private void CreateOutputDirectoryIfNecessary()
    {
        if (!_outputDirectory.Exists)
        {
            _outputDirectory.Create();
            return;
        }

        var folders = _outputDirectory.GetDirectories();
        var files = _outputDirectory.GetFiles()
            .Where(x => x.Name != CommonIncludePrinter.CommonConfigFileNameWithExtension);
        if (!_clearOutputDirectory)
        {
            if (folders.Any() || files.Any())
            {
                throw new Exception("Output directory is not empty");
            }

            return;
        }
        
        foreach (var x in folders) x.Delete(true);
        foreach (var x in files) x.Delete();
    }
}