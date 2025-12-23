using System;
using System.IO;

namespace Archivsoftware.Services
{
    public class WatcherService
    {
        private FileSystemWatcher? _watcher;
        private readonly DocumentService _documentService;

        // Zielordner in der DB
        private FolderTreeItem? _targetFolder;

        // Event für GUI-Refresh
        public event Action<string>? FileCreatedOrChanged;

        public WatcherService(DocumentService documentService)
        {
            _documentService = documentService;
        }

        public void Start(string path, FolderTreeItem? targetFolder)
        {
            Stop(); // immer nur 1 Ordner gleichzeitig

            _targetFolder = targetFolder;

            _watcher = new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Created += OnCreatedOrChanged;
            _watcher.Changed += OnCreatedOrChanged;

            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;

                _watcher.Created -= OnCreatedOrChanged;
                _watcher.Changed -= OnCreatedOrChanged;

                _watcher.Dispose();
                _watcher = null;
            }

            _targetFolder = null;
        }

        private void OnCreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            // Nur Dateien, keine Ordner
            if (!File.Exists(e.FullPath)) return;

            var ext = Path.GetExtension(e.FullPath).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".docx")
                return;

            if (!WaitUntilFileIsReady(e.FullPath)) return;

            // In den Zielordner importieren
            _documentService.ImportFiles(new[] { e.FullPath }, _targetFolder);

            // GUI informieren
            FileCreatedOrChanged?.Invoke(e.FullPath);
        }

        private static bool WaitUntilFileIsReady(string path)
        {
            // max ~3 Sekunden warten
            for (int i = 0; i < 15; i++)
            {
                try
                {
                    using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return true;
                }
                catch
                {
                    System.Threading.Thread.Sleep(200);
                }
            }

            return false;
        }
    }
}
