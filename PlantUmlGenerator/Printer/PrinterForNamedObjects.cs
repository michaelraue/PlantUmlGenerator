using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public abstract class PrinterForNamedObjects<T> where T : NamespacedObject
{
    private readonly TextWriter _writer;

    protected PrinterForNamedObjects(T obj, TextWriter writer, PumlProject project)
    {
        Object = obj;
        _writer = writer;
        Project = project;
    }

    protected T Object { get; }
    
    protected PumlProject Project { get; }

    protected int IndentationLevel { get; set; }

    public abstract Task Print();

    protected string GetDirectoryLevelUpsToRoot() =>
        string.Concat(Enumerable.Repeat($"..{IncludesPrinter.PumlFileDirectorySeparator}", Object.Namespace.Split(".").Length));

    protected async Task<bool> PrintIncomingReferenceIncludes()
    {
        var incomingReferences = Project.GetReferencesTo(Object).ToList();
        if (!incomingReferences.Any())
        {
            return false;
        }

        var up = GetDirectoryLevelUpsToRoot();
        foreach (var down in incomingReferences.Select(IncludesPrinter.GetIncludesPathByNamespace))
        {
            await WriteLine($"!includesub {up}{down}.puml!TYPE");
        }

        return true;
    }

    protected async Task WriteLine()
    {
        await _writer.WriteLineAsync();
    }

    protected async Task WriteLine(string line)
    {
        await _writer.WriteLineAsync(GetIndentation() + line);
    }

    private string GetIndentation() =>
        string.Concat(Enumerable.Repeat(PumlPrinter.IndentationString, IndentationLevel));
}