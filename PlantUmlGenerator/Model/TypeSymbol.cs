namespace PlantUmlGenerator.Model;

public class TypeSymbol
{
    public TypeSymbol(string name, string? @namespace = null)
    {
        SymbolName = name;
        SymbolNamespace = @namespace;
    }
    
    private string? SymbolNamespace { get; }
    
    public string SymbolName { get; }

    public string SymbolFullName => string.IsNullOrWhiteSpace(SymbolNamespace) ? SymbolName : $"{SymbolNamespace}.{SymbolName}";
    
    public NamespacedObject? ResolvedTarget { get; set; }

    public bool IsResolved => ResolvedTarget != null;
}
