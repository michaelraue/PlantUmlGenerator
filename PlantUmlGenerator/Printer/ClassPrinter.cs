using System.Text.RegularExpressions;
using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public class ClassPrinter : PrinterForNamedObjects<Class>
{
    private readonly List<string> _namespacesToDrawNoArrowsTo;

    public ClassPrinter(Class @class, TextWriter writer, PumlProject project,
        IEnumerable<string> namespacesToDrawNoAssociationsTo, IEnumerable<string> namespacesToHideInOtherNamespaces)
        : base(@class, writer, project, namespacesToHideInOtherNamespaces)
    {
        _namespacesToDrawNoArrowsTo = namespacesToDrawNoAssociationsTo.ToList();
    }

    public override async Task Print()
    {
        var abstractModifier = Object.IsAbstract ? "abstract " : string.Empty;
        var stereotype = GetClassStereotype();
        await WriteLine("@startuml");
        await WriteLine();
        await PrintCommonConfigInclude();
        await PrintNamespaceIncludes();
        await WriteLine();
        await WriteLine("!startsub TYPE");
        await WriteLine($"{abstractModifier}class {Object.FullName}{stereotype}{{");
        await PrintAttributes();
        await WriteLine("}");
        await PrintAssociations();
        await PrintInheritance();
        await WriteLine("!endsub");
        await WriteLine();
        await PrintIncludes();
        await WriteLine();
        await WriteLine("@enduml");
    }

    private async Task PrintNamespaceIncludes()
    {
        var up = GetDirectoryLevelUpsToRoot();
        foreach (var @namespace in
                 GetOutgoingAssociationsToPrint().Select(x => x.TargetSymbol.ResolvedTarget!.Namespace)
                     .Union(GetIncomingReferences().Select(x => x.Namespace))
                     .Union(new[] { HasBaseClassToPrint() ? Object.BaseTypeSymbol!.ResolvedTarget!.Namespace : string.Empty})
                     .Union(new[] { Object.Namespace })
                     .SelectMany(PumlPrinter.GetAllSubNamespacePermutations)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Order()
                     .Distinct())
        {
            await WriteLine($"!include {up}{CommonIncludePrinter.CommonConfigFileNameWithExtension}!{@namespace}");
        }
    }

    private async Task PrintAttributes()
    {
        IndentationLevel++;
        foreach (var attribute in Object.Associations.Where(x => !ShouldPrintAttributeAsAssociation(Object, x)))
        {
            var cardinality = attribute.IsList ? "[*]" : attribute.IsNullable ? "?" : string.Empty;
            var targetTypeName = attribute.TargetSymbol.IsResolved
                ? attribute.TargetSymbol.ResolvedTarget!.FullName
                : attribute.TargetSymbol.SymbolName;
            await WriteLine($"{attribute.Name}: {targetTypeName}{cardinality}");
        }

        IndentationLevel--;
    }

    private async Task PrintAssociations()
    {
        foreach (var attribute in Object.Associations.Where(x => ShouldPrintAttributeAsAssociation(Object, x)))
        {
            var name = attribute.Name.StartsWith(attribute.TargetSymbol.ResolvedTarget!.Name) ? string.Empty : $" : \"{attribute.Name}\"";
            var cardinality = attribute.IsList ? "\"0..*\" " : attribute.IsNullable ? "\"0..1\"" : "\"1\" ";
            var targetTypeName = attribute.TargetSymbol.ResolvedTarget!.FullName;
            await WriteLine($"{Object.FullName} --> {cardinality}{targetTypeName}{name}");
        }
    }

    private bool ShouldPrintAttributeAsAssociation(Class source, Association x) =>
        x.TargetSymbol.IsResolved &&
        AreThereNotTooManyReferences(Project.GetReferencesTo(x.TargetSymbol.ResolvedTarget!)) &&
        TargetNamespaceAllowedToPrintAssociationsTo(source, x.TargetSymbol.ResolvedTarget!.Namespace) &&
        NamespaceIsVisible(source, x.TargetSymbol.ResolvedTarget!.Namespace);

    private static bool AreThereNotTooManyReferences(IEnumerable<NamespacedObject> references) =>
        references.Count() is > 0 and < 4;

    private bool TargetNamespaceAllowedToPrintAssociationsTo(NamespacedObject source, string @namespace) =>
        !_namespacesToDrawNoArrowsTo.Contains(@namespace) ||
        source.Namespace == @namespace;

    private async Task PrintInheritance()
    {
        if (!Object.HasBaseClass ||
            !Object.BaseTypeSymbol!.IsResolved ||
            GetDddType(Object.BaseTypeSymbol!.ResolvedTarget!.FullName) != DddType.None)
        {
            return;
        }

        await WriteLine($"{Object.BaseTypeSymbol!.ResolvedTarget!.FullName} <|-- {Object.FullName}");
    }

    private async Task PrintIncludes()
    {
        await PrintOutgoingAssociationIncludes();
        await PrintIncomingReferenceIncludes();
        await PrintOutgoingBaseClassInclude();
    }

    private IEnumerable<Association> GetOutgoingAssociationsToPrint() =>
        Object.Associations.Where(x =>
            x.TargetSymbol.IsResolved &&
            NamespaceIsVisible(Object, x.TargetSymbol.ResolvedTarget!.Namespace));

    private bool HasBaseClassToPrint() => Object.HasBaseClass && Object.BaseTypeSymbol!.IsResolved &&
                                          NamespaceIsVisible(Object, Object.BaseTypeSymbol.ResolvedTarget!.Namespace);

    private async Task PrintOutgoingAssociationIncludes()
    {
        var associations = GetOutgoingAssociationsToPrint().ToList();
        if (!associations.Any())
        {
            return;
        }

        var up = GetDirectoryLevelUpsToRoot();
        foreach (var down in associations
                     .Select(x => IncludesPrinter.GetIncludesPathByNamespace(x.TargetSymbol.ResolvedTarget!)).Distinct())
        {
            await WriteLine($"!includesub {up}{down}.puml!TYPE");
        }
    }

    private async Task PrintOutgoingBaseClassInclude()
    {
        if (!HasBaseClassToPrint())
        {
            return;
        }

        var up = GetDirectoryLevelUpsToRoot();
        var down = IncludesPrinter.GetIncludesPathByNamespace(Object.BaseTypeSymbol!.ResolvedTarget!);
        await WriteLine($"!includesub {up}{down}.puml!TYPE");
    }

    private string GetClassStereotype()
    {
        if (Object.IsRecord)
        {
            return "<<Value Object>> ";
        }

        if (Object.BaseTypeSymbol is null)
        {
            return " ";
        }

        return GetDddType(Object.BaseTypeSymbol!.SymbolName) switch
        {
            DddType.AggregateRoot => "<<Aggregate Root>> ",
            DddType.Entity => "<<Entity>> ",
            DddType.ValueObject => "<<Value Object>> ",
            DddType.Enumeration => "<<Enumeration>> ",
            _ => " "
        };
    }

    private DddType GetDddType(string baseType)
    {
        if (string.IsNullOrWhiteSpace(baseType))
        {
            return DddType.None;
        }

        if (Regex.IsMatch(baseType, @"Aggregate(Root)?"))
        {
            return DddType.AggregateRoot;
        }
        
        if (Regex.IsMatch(baseType, @"Entity"))
        {
            return DddType.Entity;
        }
        
        if (Regex.IsMatch(baseType, @"(Single)?ValueObject"))
        {
            return DddType.ValueObject;
        }
        
        if (Regex.IsMatch(baseType, @"Enumeration"))
        {
            return DddType.Enumeration;
        }

        return DddType.None;
    }

    private enum DddType
    {
        None,
        AggregateRoot,
        Entity,
        ValueObject,
        Enumeration,
    }
}