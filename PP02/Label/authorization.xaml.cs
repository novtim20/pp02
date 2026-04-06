using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PP02.Label
{
    /// <summary>
    /// Логика взаимодействия для authorization.xaml
    /// </summary>
    public partial class authorization : Page
    {
        MainWindow mainWindow;
        public authorization(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ErrorTextBlock.Text = "Заполните все поля";
                return;
            }

            // Пример проверки (замените на реальную логику!)
            if (login == "admin" && password == "admin")
            {
                MessageBox.Show("Успешный вход!", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                MainWindow.init.OpenPages(new Label.search());

                // Здесь можно открыть главное окно:
                // new MainWindow().Show();

            }
            else
            {
                ErrorTextBlock.Text = "Неверный логин или пароль";
                PasswordBox.Clear();
            }
        }
    }
}
