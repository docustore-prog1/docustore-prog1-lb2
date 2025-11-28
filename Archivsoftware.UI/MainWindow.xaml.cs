using DocumentManager.Data;
using DocumentManager.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;

namespace Archivsoftware
{
    public partial class MainWindow : Window
    {
        private readonly DocumentDbContext _db;
        private FolderTreeItem? _selectedItem;

        public MainWindow()
        {
            InitializeComponent();
            _db = new DocumentDbContext();
            LoadFolderTree();
        }

        private async void OpenFilePicker(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select folders to import",
                IsFolderPicker = true,
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                FilePathTextBox.Text = string.Join(Environment.NewLine, dialog.FileNames);
                FilePathTextBox.IsEnabled = false;
                UploadButton.IsEnabled = false;
                UploadButton.Content = "Uploading...";

                int? parentFolderId = null;
                if (_selectedItem != null && _selectedItem.IsFolder)
                {
                    parentFolderId = _selectedItem.FolderId;
                }

                await Task.Run(() =>
                {
                    using (var backgroundDb = new DocumentDbContext())
                    {
                        Folder? dbParent = null;
                        if (parentFolderId.HasValue)
                        {
                            dbParent = backgroundDb.Folders.Find(parentFolderId.Value);
                        }

                        foreach (var directoryPath in dialog.FileNames)
                        {
                            ImportDirectory(backgroundDb, directoryPath, dbParent);
                        }

                        backgroundDb.SaveChanges();
                    }
                });

                UploadButton.Content = "Upload...";
                UploadButton.IsEnabled = true;
                FilePathTextBox.IsEnabled = true;
                FilePathTextBox.Text = "";

                LoadFolderTree();
            }
        }

        private void ImportDirectory(DocumentDbContext context, string dirPath, Folder? parentFolder)
        {
            var directoryInfo = new DirectoryInfo(dirPath);
            var newFolder = new Folder
            {
                Name = directoryInfo.Name,
                ParentFolder = parentFolder
            };

            context.Folders.Add(newFolder);
            context.SaveChanges();

            foreach (var filePath in Directory.GetFiles(dirPath))
            {
                var fileInfo = new FileInfo(filePath);

                var newDoc = new Document
                {
                    Title = fileInfo.Name,
                    Folder = newFolder
                };

                context.Documents.Add(newDoc);
            }

            foreach (var subDirPath in Directory.GetDirectories(dirPath))
            {
                ImportDirectory(context, subDirPath, newFolder);
            }
        }

        private void LoadFolderTree()
        {
            var folders = _db.Folders
                .Include(f => f.ParentFolder)
                .Include(f => f.SubFolders)
                .Include(f => f.Documents)
                .AsNoTracking()
                .ToList();

            var rootFolders = folders
                .Where(f => f.ParentFolder == null)
                .ToList();

            var rootItems = new List<FolderTreeItem>();

            foreach (var root in rootFolders)
            {
                var rootNode = BuildFolderNode(root);
                rootItems.Add(rootNode);
            }

            FolderTree.ItemsSource = rootItems;
        }

        private FolderTreeItem BuildFolderNode(Folder folder)
        {
            var node = new FolderTreeItem(folder.Id, null, folder.Name);

            foreach (var child in folder.SubFolders)
            {
                node.Children.Add(BuildFolderNode(child));
            }

            foreach (var doc in folder.Documents)
            {
                node.Children.Add(new FolderTreeItem(null, doc.Id, doc.Title));
            }

            return node;
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

            Folder? parentFolder = null;

            if (_selectedItem != null && _selectedItem.IsFolder)
            {
                parentFolder = _db.Folders
                    .FirstOrDefault(f => f.Id == _selectedItem.FolderId);
            }

            var newFolder = new Folder
            {
                Name = name,
                ParentFolder = parentFolder
            };

            _db.Folders.Add(newFolder);
            _db.SaveChanges();

            LoadFolderTree();
        }
    }
}
