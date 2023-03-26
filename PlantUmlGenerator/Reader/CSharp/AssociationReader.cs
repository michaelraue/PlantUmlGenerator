using Microsoft.CodeAnalysis;
using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Reader.CSharp;

public class AssociationReader : SymbolVisitor
{
    private const string EnumerableType = "System.Collections.IEnumerable";
    private const string DictionaryType = "System.Collections.IDictionary";
    
    private readonly PumlProject _project;

    public AssociationReader(PumlProject project)
    {
        _project = project;
    }

    public override void VisitProperty(IPropertySymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared)
        {
            return;
        }
        
        var targetTypeNamespace = symbol.Type.Accept(new GetTypeSymbolNamespace());
        var targetTypeName = symbol.Type.Accept(new GetTypeSymbolName(targetTypeNamespace));
        if (string.IsNullOrWhiteSpace(targetTypeName))
        {
            return;
        }

        var relativeTargetTypeNamespace = _project.ConvertToRelativeNamespace(targetTypeNamespace);
        var isList = symbol.Type.Accept(new IsList());
        var isNullable = symbol.Type.Accept(new IsBoxedNullableType());
        if (targetTypeName.EndsWith("?"))
        {
            isNullable = true;
            targetTypeName = targetTypeName[..^1];
        }

        _project.Add(new Association(symbol.Name, new TypeSymbol(targetTypeName, relativeTargetTypeNamespace), isList, isNullable));
    }

    private static bool IsDictionaryType(INamedTypeSymbol symbol)
    {
        bool IsIDictionary(ITypeSymbol s) =>
            s.TypeKind == TypeKind.Interface &&
            (s.ContainingNamespace.ToString()?.StartsWith("System") ?? false) &&
            s.Name == DictionaryType.Split(".").Last();

        bool IsDictionary(ITypeSymbol s) =>
            s.TypeKind == TypeKind.Class &&
            s.Interfaces.Any(x => x.ToString() == DictionaryType);

        return symbol.IsGenericType &&
               (IsIDictionary(symbol) || IsDictionary(symbol));
    }

    private static bool IsListType(INamedTypeSymbol symbol) =>
        symbol.IsGenericType && symbol.Interfaces.Any(x => x.ToString() == EnumerableType);

    private static bool IsNullableType(INamedTypeSymbol symbol) =>
        symbol.IsGenericType && symbol.ContainingNamespace.Name == "System" && symbol.Name == "Nullable";

    private abstract class ChooseTypeSymbolBasedOnGenerics : SymbolVisitor<string>
    {
        public override string? VisitArrayType(IArrayTypeSymbol symbol) =>
            symbol.ElementType.Accept(this);

        public override string? VisitNamedType(INamedTypeSymbol symbol)
        {
            var symbolToConsider = IsDictionaryType(symbol) ? symbol.TypeArguments.ElementAt(1)
                : IsListType(symbol) || IsNullableType(symbol) ? symbol.TypeArguments.First() : symbol;
            return HandleSymbol(symbolToConsider);
        }

        protected abstract string? HandleSymbol(ITypeSymbol symbol);
    }

    private class GetTypeSymbolName : ChooseTypeSymbolBasedOnGenerics
    {
        private readonly string? _symbolNamespace;

        public GetTypeSymbolName(string? symbolNamespace)
        {
            _symbolNamespace = symbolNamespace;
        }

        protected override string? HandleSymbol(ITypeSymbol symbol)
        {
            if (_symbolNamespace is null)
            {
                return symbol.ToString();
            }
            
            return symbol.ToString()?
                .Replace(_symbolNamespace, string.Empty)
                .TrimStart('.');
        }
    }

    private class GetTypeSymbolNamespace : ChooseTypeSymbolBasedOnGenerics
    {
        protected override string HandleSymbol(ITypeSymbol symbol) =>
            symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    }

    private class IsList : SymbolVisitor<bool>
    {
        public override bool VisitArrayType(IArrayTypeSymbol symbol) => true;

        public override bool VisitNamedType(INamedTypeSymbol symbol) =>
            IsDictionaryType(symbol) || IsListType(symbol);
    }

    private class IsBoxedNullableType : SymbolVisitor<bool>
    {
        public override bool VisitNamedType(INamedTypeSymbol symbol) =>
            IsNullableType(symbol);
    }
}
