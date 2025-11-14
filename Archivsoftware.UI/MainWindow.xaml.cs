using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Archivsoftware
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFilePicker(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Files and Folders",
                IsFolderPicker = true,
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments
                )
            };
            dialog.ShowDialog();
        }
    }
}