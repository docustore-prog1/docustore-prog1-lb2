using Archivsoftware.Services;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using System.Linq;


namespace Archivsoftware
{
    public partial class MainWindow : Window
    {
        private readonly FolderService _folderService;
        private readonly DocumentService _documentService;
        private readonly WatcherService _watcherService;


        private FolderTreeItem? _selectedItem;

        public MainWindow()
        {
            InitializeComponent();

            _folderService = new FolderService();
            _documentService = new DocumentService();
            _watcherService = new WatcherService(_documentService);


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
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Watcher-Ordner auswählen"
            };

            var result = dialog.ShowDialog();
            if (result == true)
            {
                var path = dialog.FolderName;
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext == ".pdf" || ext == ".docx";
                    })
                    .ToArray();

                _documentService.ImportFiles(files, null);


                _watcherService.Start(path);

                MessageBox.Show($"Watcher gestartet für:\n{path}", "Watcher", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}
