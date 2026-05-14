using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PP02.Label;
using PP02.Classes.Person;
using PP02.Connect;

namespace PP02.Label
{
    public partial class StudentsListPage : Page
    {
        public StudentsListPage()
        {
            InitializeComponent();
            LoadStudents();
        }

        /// <summary>
        /// Загрузка списка студентов из базы данных
        /// </summary>
        private async void LoadStudents()
        {
            try
            {
                // Если список людей пуст, загружаем данные из базы данных
                if (DataProvider.PeopleVMList.Count == 0)
                {
                    var db = new DataProvider();
                    string connectionString = Connect.Connect.GetConnectionString();

                    // Загружаем справочники и данные о людях
                    await Task.Run(() =>
                    {
                        db.LoadAllDictionaries(connectionString);
                        db.DataPeople(connectionString);
                    });
                }

                // Получаем список всех студентов из DataProvider (фильтруем по роли "Студент")
                var allPeople = DataProvider.PeopleVMList;
                var students = allPeople.Where(p => p.Role == "Студент" || string.IsNullOrEmpty(p.Role)).ToList();
                StudentsList.ItemsSource = students;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка студентов:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Кнопка "Добавить студента"
        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.OpenPages(new AddStudentPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода на страницу добавления студента:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Кнопка "Документы об образовании"
        private void EducationDocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.OpenPages(new EducationDocumentsPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода на страницу документов об образовании:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Кнопка "Импорт из Excel"
        private void ImportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.OpenPages(new ImportExcelPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода на страницу импорта Excel:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Кнопка "Отчёты"
        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.OpenPages(new ReportPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода на страницу отчётов:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Кнопка "Специальности и группы"
        private void SpecialtyGroupsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.OpenPages(new SpecialtyGroupsPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода на страницу специальностей и групп:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Кнопка "Поиск студентов"
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.OpenPages(new search());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка перехода на страницу поиска:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}