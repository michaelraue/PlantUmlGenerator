namespace PlantUmlGenerator.Model;

public class PumlProject
{
    private readonly Dictionary<string, Class> _classes;
    private readonly Dictionary<string, Enumeration> _enumerations;
    private Class? _currentClass;

    public PumlProject(string topLevelNamespace)
    {
        TopLevelNamespace = topLevelNamespace;
        _classes = new Dictionary<string, Class>();
        _enumerations = new Dictionary<string, Enumeration>();
    }

    public string TopLevelNamespace { get; }

    public IReadOnlyList<Class> Classes => _classes.Values.ToList().AsReadOnly();
    
    public IReadOnlyList<Enumeration> Enumerations => _enumerations.Values.ToList().AsReadOnly();

    public IEnumerable<string> GetAllNamespaces() => Classes.Select(x => x.Namespace).Distinct();

    public string ConvertToRelativeNamespace(string? @namespace) =>
        @namespace?.Replace(TopLevelNamespace, string.Empty).TrimStart('.') ?? string.Empty;

    public IEnumerable<Class> GetReferencesTo(NamespacedObject target) =>
        Classes.Where(c =>
            c.Associations.Any(a => a.TargetSymbol.IsResolved && a.TargetSymbol.ResolvedTarget == target) ||
            (c.BaseTypeSymbol?.IsResolved ?? false) && c.BaseTypeSymbol.ResolvedTarget == target);
    
    public void Add(Class @class)
    {
        _classes.Add(@class.FullName, @class);
        _currentClass = @class;
    }
    
    public void Add(Association association)
    {
        if (_currentClass is null)
        {
            throw new InvalidOperationException("Cannot access current class before adding one");
        }

        _currentClass.Add(association);
    }

    public void Add(Enumeration enumeration)
    {
        _enumerations.Add(enumeration.FullName, enumeration);
    }
    
    public void LinkSymbols()
    {
        LinkAssociations();
        LinkBaseTypes();
    }

    private Dictionary<string, NamespacedObject> AllNamedObjects()
    {
        var result = new Dictionary<string, NamespacedObject>();
        foreach (var x in _classes)
        {
            result.Add(x.Key, x.Value);
        }

        foreach (var x in _enumerations)
        {
            result.Add(x.Key, x.Value);
        }

        return result;
    }

    private void LinkAssociations()
    {
        var namedObjects = AllNamedObjects();
        foreach (var association in Classes.SelectMany(x => x.Associations).Where(x => namedObjects.ContainsKey(x.TargetSymbol.SymbolFullName)))
        {
            association.TargetSymbol.ResolvedTarget = namedObjects[association.TargetSymbol.SymbolFullName];
        }
    }

    private void LinkBaseTypes()
    {
        foreach (var @class in Classes.Where(x => x.HasBaseClass && _classes.ContainsKey(x.BaseTypeSymbol!.SymbolFullName)))
        {
            @class.BaseTypeSymbol!.ResolvedTarget = _classes[@class.BaseTypeSymbol.SymbolFullName];
        }
    }
}