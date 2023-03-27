using System.Text;
using PlantUmlGenerator.Model;

namespace PlantUmlGenerator.Printer;

public class IncludesPrinter
{
    public const char PumlFileDirectorySeparator = '/';
    private const string IncludesFileNameWithoutExtension = "_includes";
    private readonly string _fullOutputPath;
    private readonly Folder _outputFolder;

    public IncludesPrinter(DirectoryInfo outputDirectory)
    {
        _fullOutputPath = outputDirectory.FullName;
        _outputFolder = new Folder(string.Empty, outputDirectory.FullName, 0);
    }

    public static string GetIncludesPathByNamespace(NamespacedObject obj) =>
        obj.FullName.Replace('.', PumlFileDirectorySeparator);

    public static string GetDirectoryLevelUpsToRoot(int nestLevels) =>
        string.Concat(Enumerable.Repeat($"..{PumlFileDirectorySeparator}", nestLevels));

    public Task Print() => _outputFolder.Print();

    public void Add(FileInfo file) =>
        _outputFolder.Add(
            file.FullName
                .Replace(_fullOutputPath, string.Empty)
                .TrimStart(Path.DirectorySeparatorChar));

    private class Folder
    {
        private readonly string _name;
        private readonly string _path;
        private readonly Folder? _parent;
        private readonly Dictionary<string, Folder> _subFolders;
        private readonly List<string> _files;
        private readonly int _nestLevel;

        public Folder(string name, string path, int nestLevel, Folder? parent = null)
        {
            _name = name;
            _path = path;
            _nestLevel = nestLevel;
            _files = new();
            _subFolders = new();
            _parent = parent;
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
                _subFolders.Add(parts[0], new Folder(parts[0], Path.Combine(_path, parts[0]), _nestLevel+1, this));
            }

            _subFolders[parts[0]].Add(string.Join(Path.DirectorySeparatorChar, parts[1..]));
        }

        public async Task Print()
        {
            await WriteTheFileInThisFolder();
            await WriteFilesInSubFolders();
        }

        private async Task WriteTheFileInThisFolder()
        {
            var content = new StringBuilder();
            content.AppendLine("@startuml").AppendLine();
            content.AppendLine("!startsub FOLDER_INCLUDES");
            PrintCommonFileIncludes(content);
            PrintSubFolderIncludes(content);
            content.AppendLine("!endsub").AppendLine();
            content.AppendLine("!startsub FILE_INCLUDES");
            PrintFileIncludes(content);
            content.AppendLine("!endsub");
            content.AppendLine().AppendLine("@enduml");
            await WriteFile(content);
        }

        private IEnumerable<string> GetFullname() => _parent == null ? new[] { _name } : _parent.GetFullname().Concat(new[] { _name });

        private void PrintMyBaseNamespaceIncludes(StringBuilder content, string directoryLevelUpsToRoot)
        {
            if (_parent == null)
            {
                return;
            }

            _parent.PrintMyBaseNamespaceIncludes(content, directoryLevelUpsToRoot);
            var name = string.Join(".", GetFullname().Where(x => !string.IsNullOrWhiteSpace(x)));
            content.AppendLine($"!include {directoryLevelUpsToRoot}{CommonIncludePrinter.CommonConfigFileNameWithExtension}!{name}");
        }

        private void PrintCommonFileIncludes(StringBuilder content)
        {
            var up = GetDirectoryLevelUpsToRoot(_nestLevel);
            content.AppendLine($"!include {up}{CommonIncludePrinter.CommonConfigFileNameWithExtension}");
            PrintMyBaseNamespaceIncludes(content, up);
        }

        private void PrintSubFolderIncludes(StringBuilder content)
        {
            foreach (var subFolder in _subFolders)
            {
                content.AppendLine($"!includesub {subFolder.Key}{PumlFileDirectorySeparator}{IncludesFileNameWithoutExtension}.puml!FOLDER_INCLUDES");
            }
        }

        private void PrintFileIncludes(StringBuilder content)
        {
            foreach (var subFolder in _subFolders)
            {
                content.AppendLine($"!includesub {subFolder.Key}{PumlFileDirectorySeparator}{IncludesFileNameWithoutExtension}.puml!FILE_INCLUDES");
            }

            foreach (var file in _files)
            {
                content.AppendLine($"!includesub {file}!TYPE");
            }
        }

        private Task WriteFile(StringBuilder content) =>
            File.WriteAllTextAsync(Path.Combine(_path, $"{IncludesFileNameWithoutExtension}.puml"), content.ToString());

        private async Task WriteFilesInSubFolders()
        {
            foreach (var subFolder in _subFolders)
            {
                await subFolder.Value.Print();
            }
        }
    }
}