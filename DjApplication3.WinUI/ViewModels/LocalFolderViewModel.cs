namespace DjApplication3.WinUI.ViewModels
{
    public sealed class LocalFolderViewModel
    {
        public LocalFolderViewModel(string name, string path, bool isParent = false)
        {
            Name = name;
            Path = path;
            IsParent = isParent;
        }

        public string Name { get; }
        public string Path { get; }
        public bool IsParent { get; }
    }
}
