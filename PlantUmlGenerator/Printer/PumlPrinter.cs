using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public class PumlPrinter : IPumlPrinter
{
    public const string IndentationString = "    ";

    private readonly DirectoryInfo _outputDirectory;
    private readonly bool _clearOutputDirectory;

    public PumlPrinter(DirectoryInfo outputDirectory, bool clearOutputDirectory)
    {
        _outputDirectory = outputDirectory;
        _clearOutputDirectory = clearOutputDirectory;
    }
    
    public async Task PrintPuml(PumlProject project)
    {
        CreateOutputDirectoryIfNecessary();
        var includesPrinter = new IncludesPrinter(_outputDirectory);
        foreach (var @class in project.Classes)
        {
            var folder = CreateNamespaceFolder(_outputDirectory, @class);
            await Print(@class, folder, includesPrinter, (c, writer) => new ClassPrinter(c, writer, project));
        }

        foreach (var enumeration in project.Enumerations)
        {
            var folder = CreateNamespaceFolder(_outputDirectory, enumeration);
            await Print(enumeration, folder, includesPrinter, (e, writer) => new EnumerationPrinter(e, writer, project));
        }

        await includesPrinter.Print();
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
        }
        else
        {
            if ((_outputDirectory.GetDirectories().Any() || _outputDirectory.GetFiles().Any()) && !_clearOutputDirectory)
            {
                throw new Exception("Output directory is not empty");
            }
            
            _outputDirectory.Delete(true);
            _outputDirectory.Create();
        }
    }
}