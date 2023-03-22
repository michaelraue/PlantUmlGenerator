using System.Text;
using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public class IncludesPrinter
{
    public const char PumlFileDirectorySeparator = '/';
    private const string IncludesFileNameWithoutExtension = "includes";
    private readonly string _fullOutputPath;
    private readonly Folder _outputFolder;

    public IncludesPrinter(DirectoryInfo outputDirectory)
    {
        _fullOutputPath = outputDirectory.FullName;
        _outputFolder = new Folder(outputDirectory.FullName);
    }

    public static string GetIncludesPathByNamespace(NamespacedObject obj)
    {
        return obj.FullName.Replace('.', PumlFileDirectorySeparator);
    }

    public Task Print() => _outputFolder.Print();

    public void Add(FileInfo file) =>
        _outputFolder.Add(
            file.FullName
                .Replace(_fullOutputPath, string.Empty)
                .TrimStart(Path.DirectorySeparatorChar));

    private class Folder
    {
        private readonly string _path;
        private readonly Dictionary<string, Folder> _subFolders;
        private readonly List<string> _files;

        public Folder(string path)
        {
            _path = path;
            _files = new();
            _subFolders = new();
        }

        public void Add(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                _files.Add(parts[0]);
                return;
            }

            if (!_subFolders.ContainsKey(parts[0]))
            {
                _subFolders.Add(parts[0], new Folder(Path.Combine(_path, parts[0])));
            }

            _subFolders[parts[0]].Add(string.Join(Path.DirectorySeparatorChar, parts[1..]));
        }

        public async Task Print()
        {
            var content = new StringBuilder();
            content.AppendLine("@startuml includes").AppendLine();
            foreach (var subFolder in _subFolders)
            {
                content.AppendLine($"!include {subFolder.Key}{PumlFileDirectorySeparator}{IncludesFileNameWithoutExtension}.puml");
            }

            foreach (var file in _files)
            {
                content.AppendLine($"!includesub {file}!TYPE");
            }

            content.AppendLine().AppendLine("@enduml");
            await File.WriteAllTextAsync(Path.Combine(_path, $"{IncludesFileNameWithoutExtension}.puml"), content.ToString());
            foreach (var subFolder in _subFolders)
            {
                await subFolder.Value.Print();
            }
        }
    }
}