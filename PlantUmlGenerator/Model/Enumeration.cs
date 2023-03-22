namespace PlantUmlGenerator.Model;

public class Enumeration : NamespacedObject
{
    public Enumeration(string @namespace, string name, IEnumerable<string> values)
        : base(@namespace, name)
    {
        Values = values;
    }
    
    public IEnumerable<string> Values { get; }
}