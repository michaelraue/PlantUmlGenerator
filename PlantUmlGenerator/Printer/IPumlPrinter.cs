using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public interface IPumlPrinter
{
    Task PrintPuml(PumlProject project);
}