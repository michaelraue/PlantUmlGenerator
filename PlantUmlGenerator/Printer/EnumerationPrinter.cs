using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public class EnumerationPrinter : PrinterForNamedObjects<Enumeration>
{
    public EnumerationPrinter(Enumeration enumeration, TextWriter writer, PumlProject project)
        : base(enumeration, writer, project)
    {
    }

    public override async Task Print()
    {
        await WriteLine($"@startuml");
        await WriteLine();
        await PrintCommonConfigInclude();
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