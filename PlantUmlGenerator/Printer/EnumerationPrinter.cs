using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public class EnumerationPrinter : PrinterForNamedObjects<Enumeration>
{
    public EnumerationPrinter(Enumeration enumeration, TextWriter writer, PumlProject project, IEnumerable<string> namespacesToHideInOtherNamespaces)
        : base(enumeration, writer, project, namespacesToHideInOtherNamespaces)
    {
    }

    public override async Task Print()
    {
        await WriteLine($"@startuml");
        await WriteLine();
        await PrintCommonConfigInclude();
        await PrintNamespaceIncludes();
        await WriteLine();
        await WriteLine("!startsub TYPE");
        await WriteLine($"enum {Object.FullName} {{");
        await PrintMembers();
        await WriteLine("}");
        await WriteLine("!endsub");
        await WriteLine();
        await PrintIncludes();
        await WriteLine();
        await WriteLine("@enduml");
    }

    private async Task PrintNamespaceIncludes()
    {
        var up = GetDirectoryLevelUpsToRoot();
        foreach (var @namespace in GetIncomingReferences().Select(x => x.Namespace)
                     .Union(new[] { Object.Namespace })
                     .SelectMany(PumlPrinter.GetAllSubNamespacePermutations)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Order()
                     .Distinct())
        {
            await WriteLine($"!include {up}{CommonIncludePrinter.CommonConfigFileNameWithExtension}!{@namespace}");
        }
    }

    private async Task PrintMembers()
    {
        IndentationLevel++;
        foreach (var member in Object.Values)
        {
            await WriteLine(member);
        }

        IndentationLevel--;
    }

    private async Task PrintIncludes()
    {
        await PrintIncomingReferenceIncludes();
    }
}