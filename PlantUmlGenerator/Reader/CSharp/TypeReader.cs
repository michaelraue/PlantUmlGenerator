using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Reader.CSharp;

public class TypeReader : CSharpSyntaxWalker
{
    private readonly PumlProject _project;
    
    private readonly SemanticModel _semanticModel;

    private readonly IEnumerable<string> _excludes;

    public TypeReader(PumlProject project, SemanticModel semanticModel, IEnumerable<string> excludes)
    {
        _project = project;
        _semanticModel = semanticModel;
        _excludes = excludes;
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        if (_semanticModel.GetDeclaredSymbol(node) is not {} symbol)
        {
            base.VisitEnumDeclaration(node);
            return;
        }

        var @namespace = _project.ConvertToRelativeNamespace(symbol.ContainingNamespace?.ToDisplayString());

        var enumValuesReader = new EnumValuesReader();
        foreach (var member in symbol.GetMembers())
        {
            member.Accept(enumValuesReader);
        }

        var enumeration = new Enumeration(@namespace, symbol.Name, enumValuesReader.Values);
        if (ShallBeExcluded(enumeration))
        {
            base.VisitEnumDeclaration(node);
            return;
        }

        _project.Add(enumeration);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (!HandleTypeDeclaration(node))
        {
            return;
        }

        base.VisitClassDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (!HandleTypeDeclaration(node))
        {
            return;
        }

        base.VisitRecordDeclaration(node);
    }
    
    private bool HandleTypeDeclaration(TypeDeclarationSyntax node)
    {
        if (_semanticModel.GetDeclaredSymbol(node) is not {} symbol)
        {
            return false;
        }

        var baseClassSymbol = GetBaseClassSymbol(symbol);
        var @namespace = _project.ConvertToRelativeNamespace(symbol.ContainingNamespace?.ToDisplayString());
        var @class = new Class(@namespace, symbol.Name, symbol.IsAbstract, symbol.IsRecord, baseClassSymbol);
        if (ShallBeExcluded(@class))
        {
            return false;
        }

        _project.Add(@class);
        foreach (var member in symbol.GetMembers())
        {
            member.Accept(new AssociationReader(_project));
        }

        return true;
    }

    private TypeSymbol? GetBaseClassSymbol(ITypeSymbol symbol)
    {
        if (symbol.BaseType == null)
        {
            return null;
        }

        var baseTypeNamespace = symbol.BaseType.Accept(new GetTypeSymbolNamespace());
        var baseTypeName = symbol.BaseType.Accept(new GetTypeSymbolName(baseTypeNamespace));
        if (string.IsNullOrWhiteSpace(baseTypeName))
        {
            return null;
        }

        var relativeNamespace = _project.ConvertToRelativeNamespace(baseTypeNamespace);
        return new TypeSymbol(baseTypeName, relativeNamespace);
    }

    private bool ShallBeExcluded(NamespacedObject obj)
    {
        if (obj.FullName == "<global namespace>.ThisAssembly")
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(obj.Namespace))
        {
            return _excludes.Any(x => x == obj.Name); 
        }

        return _excludes.Any(x => obj.Namespace.StartsWith(x) || x == obj.FullName);
    }
    
    private class GetTypeSymbolName : SymbolVisitor<string>
    {
        private readonly string? _symbolNamespace;

        public GetTypeSymbolName(string? symbolNamespace)
        {
            _symbolNamespace = symbolNamespace;
        }

        public override string? VisitNamedType(INamedTypeSymbol symbol)
        {
            if (_symbolNamespace is null)
            {
                return symbol.Name;
            }
            
            return symbol.Name
                .Replace(_symbolNamespace, string.Empty)
                .TrimStart('.');
        }
    }

    private class GetTypeSymbolNamespace : SymbolVisitor<string>
    {
        public override string VisitNamedType(INamedTypeSymbol symbol) =>
            symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    }
}