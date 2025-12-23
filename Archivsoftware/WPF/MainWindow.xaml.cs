using Archivsoftware.Services;
using DocumentManager.Data;
using DocumentManager.Data.Models;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows;

namespace Archivsoftware
{
    public partial class MainWindow : Window
    {
        private readonly FolderService _folderService;
        private readonly DocumentService _documentService;
        private readonly WatcherService _watcherService;
        private readonly SearchService _searchService;

        private FolderTreeItem? _selectedItem;
        private List<SearchResultItem> _currentResults = new();

        public MainWindow()
        {
            InitializeComponent();

            _folderService = new FolderService();
            _documentService = new DocumentService();
            _watcherService = new WatcherService(_documentService);
            _searchService = new SearchService();

            _watcherService.FileCreatedOrChanged += path =>
            {
                Dispatcher.Invoke(LoadFolderTree);
            };

            LoadFolderTree();
        }

        private void LoadFolderTree()
        {
            var rootItems = _folderService.GetFolderTree();
            FolderTree.ItemsSource = rootItems;
        }

        private async void OpenFilePicker(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select files to import",
                Filter = "PDF and Word Files (*.pdf;*.docx)|*.pdf;*.docx",
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                FilePathTextBox.Text = string.Join(Environment.NewLine, dialog.FileNames);
                FilePathTextBox.IsEnabled = false;
                UploadButton.IsEnabled = false;
                UploadButton.Content = "Uploading...";

                var fileNames = dialog.FileNames.ToArray();
                var selectedItem = _selectedItem;

                await Task.Run(() =>
                {
                    _documentService.ImportFiles(fileNames, selectedItem);
                });

                UploadButton.Content = "Upload...";
                UploadButton.IsEnabled = true;
                FilePathTextBox.IsEnabled = true;
                FilePathTextBox.Text = "";

                LoadFolderTree();
            }
        }

        private void OpenWatcherFolderPicker(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null || !_selectedItem.IsFolder)
            {
                MessageBox.Show("Bitte zuerst einen Zielordner im Baum auswählen.",
                    "Watcher", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Watcher-Ordner auswählen"
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                var path = dialog.FolderName;

                // Initialen Bestand in den ausgewählten Ordner importieren
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext == ".pdf" || ext == ".docx";
                    })
                    .ToArray();

                _documentService.ImportFiles(files, _selectedItem);

                // Watcher mit Zielordner starten
                _watcherService.Start(path, _selectedItem);

                MessageBox.Show($"Watcher gestartet für:\n{path}", "Watcher", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadFolderTree();
            }
        }

        private void FolderTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedItem = e.NewValue as FolderTreeItem;
        }

        private void NewFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox(
                "Name des neuen Ordners:",
                "Neuer Ordner",
                "Neuer Ordner"
            );

            if (string.IsNullOrWhiteSpace(name))
                return;

            var folder = _folderService.CreateFolder(name, _selectedItem);
            if (folder == null)
            {
                MessageBox.Show("In diesem Ordner-Level existiert bereits ein Ordner mit diesem Namen.",
                    "Ordner anlegen nicht möglich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            LoadFolderTree();
        }

        private void RenameMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null || !_selectedItem.IsFolder)
                return;

            var currentName = _selectedItem.Name;

            var newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Neuer Name des Ordners:",
                "Ordner umbenennen",
                currentName
            );

            if (string.IsNullOrWhiteSpace(newName) || newName == currentName)
                return;

            var success = _folderService.RenameFolder(_selectedItem, newName);

            if (!success)
            {
                MessageBox.Show("In diesem Ordner-Level existiert bereits ein Ordner mit diesem Namen oder der Ordner konnte nicht gefunden werden.",
                    "Umbenennen nicht möglich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            LoadFolderTree();
        }

        private void DeleteMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null || !_selectedItem.IsFolder)
                return;

            var confirm = MessageBox.Show(
                "Möchten Sie diesen Ordner wirklich löschen?",
                "Ordner löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            var success = _folderService.DeleteFolder(_selectedItem);

            if (!success)
            {
                MessageBox.Show("Der Ordner enthält möglicherweise Unterordner oder konnte nicht gelöscht werden.",
                    "Löschen nicht möglich",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            LoadFolderTree();
        }

        private void MoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // Quellordner: Item, zu dem das Kontextmenü gehört
            if (sender is FrameworkElement fe && fe.DataContext is FolderTreeItem sourceItem && sourceItem.IsFolder)
            {
                // Zielordner: aktuell im TreeView ausgewählter Ordner
                var targetItem = _selectedItem;

                // Verschieben auf Root-Ebene erlauben: wenn kein Ziel ausgewählt ist
                if (targetItem != null && !targetItem.IsFolder)
                {
                    MessageBox.Show("Bitte als Ziel einen Ordner auswählen.",
                        "Verschieben", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var success = _folderService.MoveFolder(sourceItem, targetItem);

                if (!success)
                {
                    MessageBox.Show("Verschieben nicht möglich (Name existiert im Ziel bereits, Ziel ungültig oder Ordner-Beziehung unzulässig).",
                        "Verschieben", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoadFolderTree();
            }
            else
            {
                MessageBox.Show("Quellordner konnte nicht bestimmt werden.",
                    "Verschieben", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            var query = SearchTextBox.Text;
            _currentResults = _searchService.Search(query);
            ResultsList.ItemsSource = _currentResults;
            DetailTextBox.Text = string.Empty;
        }

        private void ResultsList_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var item = ResultsList.SelectedItem as SearchResultItem;
            if (item == null)
            {
                DetailTextBox.Text = string.Empty;
                return;
            }

            // Volltext des Dokuments laden
            using var db = new DocumentDbContext();
            var doc = db.Documents.FirstOrDefault(d => d.Id == item.DocumentId);
            if (doc == null)
            {
                DetailTextBox.Text = string.Empty;
                return;
            }

            DetailTextBox.Text = doc.PlainText ?? string.Empty;
        }
    }
}
