using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public abstract class PrinterForNamedObjects<T> where T : NamespacedObject
{
    private readonly List<string> _namespacesToHideInOtherNamespaces;
    private readonly TextWriter _writer;

    protected PrinterForNamedObjects(T obj, TextWriter writer, PumlProject project, IEnumerable<string> namespacesToHideInOtherNamespaces)
    {
        Object = obj;
        _writer = writer;
        Project = project;
        _namespacesToHideInOtherNamespaces = namespacesToHideInOtherNamespaces.ToList();
    }

    protected T Object { get; }
    
    protected PumlProject Project { get; }

    protected int IndentationLevel { get; set; }

    public abstract Task Print();

    protected IEnumerable<Class> GetIncomingReferences() =>
        Project.GetReferencesTo(Object).Where(x => NamespaceIsVisible(Object, x.Namespace));

    protected async Task PrintIncomingReferenceIncludes()
    {
        var incomingReferences = GetIncomingReferences().ToList();
        if (!incomingReferences.Any())
        {
            return;
        }

        var up = GetDirectoryLevelUpsToRoot();
        foreach (var down in incomingReferences.Select(IncludesPrinter.GetIncludesPathByNamespace))
        {
            await WriteLine($"!includesub {up}{down}.puml!TYPE");
        }
    }

    protected bool NamespaceIsVisible(NamespacedObject source, string @namespace) =>
        !_namespacesToHideInOtherNamespaces.Contains(@namespace) ||
        source.Namespace == @namespace;

    protected async Task PrintCommonConfigInclude()
    {
        var up = GetDirectoryLevelUpsToRoot();
        await WriteLine($"!include {up}{CommonIncludePrinter.CommonConfigFileNameWithExtension}");
    }
    
    protected string GetDirectoryLevelUpsToRoot() =>
        IncludesPrinter.GetDirectoryLevelUpsToRoot(Object.Namespace.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length);

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