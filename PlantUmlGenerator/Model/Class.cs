namespace PlantUmlGenerator.Model;

public class Class : NamespacedObject
{
    private readonly List<Association> associations;

    public Class(string @namespace, string name, bool isAbstract, TypeSymbol? baseTypeSymbol)
        : base(@namespace, name)
    {
        IsAbstract = isAbstract;
        BaseTypeSymbol = baseTypeSymbol;
        associations = new(); 
    }
    
    public bool IsAbstract { get; }

    public TypeSymbol? BaseTypeSymbol { get; }

    public bool HasBaseClass => BaseTypeSymbol != null;

    public IReadOnlyList<Association> Associations => associations.AsReadOnly();

    public void Add(Association association) => associations.Add(association);
}
