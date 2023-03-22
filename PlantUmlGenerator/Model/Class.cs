namespace PlantUmlGenerator.Model;

public class Class : NamespacedObject
{
    private readonly List<Association> _associations;

    public Class(string @namespace, string name, bool isAbstract, bool isRecord, TypeSymbol? baseTypeSymbol)
        : base(@namespace, name)
    {
        IsAbstract = isAbstract;
        IsRecord = isRecord;
        BaseTypeSymbol = baseTypeSymbol;
        _associations = new(); 
    }
    
    public bool IsAbstract { get; }

    public bool IsRecord { get; }

    public TypeSymbol? BaseTypeSymbol { get; }

    public bool HasBaseClass => BaseTypeSymbol != null;

    public IReadOnlyList<Association> Associations => _associations.AsReadOnly();

    public void Add(Association association) => _associations.Add(association);
}
