namespace PlantUmlGenerator.Model;

public abstract class NamespacedObject
{
    protected NamespacedObject(string @namespace, string name)
    {
        Namespace = @namespace;
        Name = name;
    }

    public string Namespace { get; }

    public string Name { get; }
    
    public string FullName => string.IsNullOrWhiteSpace(Namespace) ? Name : $"{Namespace}.{Name}";
}