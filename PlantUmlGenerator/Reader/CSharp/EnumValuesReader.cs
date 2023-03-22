using Microsoft.CodeAnalysis;

namespace PlantUmlGenerator.Reader.CSharp;

public class EnumValuesReader : SymbolVisitor
{
    private readonly List<string> _values = new();

    public IEnumerable<string> Values => _values;

    public override void VisitField(IFieldSymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared)
        {
            return;
        }
        
        _values.Add(symbol.Name);
    }
}
