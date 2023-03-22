using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Reader;

public interface IReader
{
    Task<PumlProject> Read();
}