using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PP02.Classes.Person;

namespace PP02.Label
{
    /// <summary>
    /// Окно для отображения ошибок импорта
    /// </summary>
    public partial class ImportErrorsWindow : Window
    {
        public ObservableCollection<ImportErrorInfo> Errors { get; }

        public ImportErrorsWindow(ObservableCollection<ImportErrorInfo> errors)
        {
            InitializeComponent();
            Errors = errors;
            DataContext = this;

            ErrorsDataGrid.ItemsSource = errors;
            ErrorCountText.Text = $"Всего ошибок: {errors.Count}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyErrorButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var error = button?.DataContext as ImportErrorInfo;

            if (error != null)
            {
                var errorText = $"Ошибка в строке {error.RowNumber} ({error.FullName}):\n{error.ErrorMessage}\n{error.ErrorDetails}";
                Clipboard.SetText(errorText);
                MessageBox.Show("Информация об ошибке скопирована в буфер обмена", "Копирование",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}