using System.Text.RegularExpressions;
using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public class ClassPrinter : PrinterForNamedObjects<Class>
{
    public ClassPrinter(Class @class, TextWriter writer, PumlProject project)
        : base(@class, writer, project)
    {
    }

    public override async Task Print()
    {
        var abstractModifier = Object.IsAbstract ? "abstract " : string.Empty;
        var stereotype = GetClassStereotype();
        await WriteLine($"@startuml");
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
        await WriteLine("@enduml");
    }

    private async Task PrintAttributes()
    {
        IndentationLevel++;
        foreach (var attribute in Object.Associations.Where(x => !ShouldPrintAttributeAsAssociation(x)))
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
        foreach (var attribute in Object.Associations.Where(ShouldPrintAttributeAsAssociation))
        {
            var name = attribute.Name.StartsWith(attribute.TargetSymbol.ResolvedTarget!.Name) ? string.Empty : $" : \"{attribute.Name}\"";
            var cardinality = attribute.IsList ? "\"0..*\" " : attribute.IsNullable ? "\"0..1\"" : "\"1\" ";
            var targetTypeName = attribute.TargetSymbol.ResolvedTarget!.FullName;
            await WriteLine($"{Object.FullName} --> {cardinality}{targetTypeName}{name}");
        }
    }

    private bool ShouldPrintAttributeAsAssociation(Association x) =>
        x.TargetSymbol.IsResolved &&
        AreThereNotTooManyReferences(Project.GetReferencesTo(x.TargetSymbol.ResolvedTarget!));

    private static bool AreThereNotTooManyReferences(IEnumerable<NamespacedObject> references) =>
        references.Count() is > 0 and < 4;

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
        if (await PrintOutgoingAssociationIncludes() |
            await PrintIncomingReferenceIncludes() |
            await PrintOutgoingBaseClassInclude())
        {
            await WriteLine();
        }
    }

    private async Task<bool> PrintOutgoingAssociationIncludes()
    {
        var associations = Object.Associations.Where(x => x.TargetSymbol.IsResolved).ToList();
        if (!associations.Any())
        {
            return false;
        }

        var up = GetDirectoryLevelUpsToRoot();
        foreach (var down in associations.Select(x => IncludesPrinter.GetIncludesPathByNamespace(x.TargetSymbol.ResolvedTarget!)))
        {
            await WriteLine($"!includesub {up}{down}.puml!TYPE");
        }

        return true;
    }

    private async Task<bool> PrintOutgoingBaseClassInclude()
    {
        if (!Object.HasBaseClass || !Object.BaseTypeSymbol!.IsResolved)
        {
            return false;
        }

        var up = GetDirectoryLevelUpsToRoot();
        var down = IncludesPrinter.GetIncludesPathByNamespace(Object.BaseTypeSymbol!.ResolvedTarget!);
        await WriteLine($"!includesub {up}{down}.puml!TYPE");

        return true;
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