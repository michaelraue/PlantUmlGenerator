using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Reader.CSharp;

public class CSharpReader : IReader
{
    private readonly FileInfo _csproj;
    private readonly IEnumerable<string> _excludes;

    public CSharpReader(FileInfo csproj, IEnumerable<string> excludes)
    {
        _csproj = csproj;
        _excludes = excludes;
    }

    public async Task<PumlProject> Read()
    {
        RegisterMsBuildLocator();
        using var wp = MSBuildWorkspace.Create();
        var csProject = await GetCsProject(wp);
        var pumlProject = new PumlProject(csProject.Name);
        foreach (var doc in csProject.Documents)
        {
            if (await doc.GetSyntaxRootAsync() is not CSharpSyntaxNode syntaxRoot ||
                await doc.GetSemanticModelAsync() is not { } semanticModel)
            {
                continue;
            }
        
            syntaxRoot.Accept(new TypeReader(pumlProject, semanticModel, _excludes));
        }

        pumlProject.LinkSymbols();
        return pumlProject;
    }

    private async Task<Project> GetCsProject(MSBuildWorkspace wp)
    {
        var csProject = await wp.OpenProjectAsync(_csproj.FullName);
        await CheckForBuildErrors(csProject);
        return csProject;
    }

    private static async Task CheckForBuildErrors(Project csProject)
    {
        var compilation = await csProject.GetCompilationAsync();
        if (compilation is null)
        {
            throw new InvalidOperationException($"Project {csProject.FilePath} does not support compilation");
        }

        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
        {
            var formatter = new DiagnosticFormatter();
            foreach (var d in diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error))
            {
                Console.WriteLine(formatter.Format(d));
            }

            throw new InvalidOperationException("There were build errors");
        }
    }

    private static void RegisterMsBuildLocator()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
