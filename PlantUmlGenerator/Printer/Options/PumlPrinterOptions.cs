namespace PlantUmlGenerator.Printer.Options;

public class PumlPrinterOptions
{
    public IEnumerable<string> NamespacesToDrawNoAssociationsTo { get; set; } = Enumerable.Empty<string>();
    
    public IEnumerable<string> NamespacesToHideInOtherNamespaces { get; set; } = Enumerable.Empty<string>();
}