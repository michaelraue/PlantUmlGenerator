namespace PlantUmlGenerator.Model;

public class Association
{
    public Association(string name, TypeSymbol targetSymbol, bool isList, bool isNullable)
    {
        Name = name;
        TargetSymbol = targetSymbol;
        IsList = isList;
        IsNullable = isNullable;
    }
    
    public string Name { get; }

    public TypeSymbol TargetSymbol { get; }

    public bool IsList { get; }

    public bool IsNullable { get; }
}