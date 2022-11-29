using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminMenuPage.xaml
    /// </summary>
    public partial class AdminMenuPage : Page
    {
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private App app = (App)App.Current;
        public AdminMenuPage()
        {
            InitializeComponent();
            //  DispatcherTimer setup
            dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 30);
        }
        private void OnLoad(object sender, RoutedEventArgs e) // this event was added in HomePage.xaml header
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.NavPanelBottom.Visibility = Visibility.Visible;
            parentWindow.bBack.Visibility = Visibility.Hidden;
            parentWindow.brdAdmin.Visibility = Visibility.Hidden;
            parentWindow.PageTitle.Content = "Расширенные функции управления";
            dispatcherTimer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            //return to home page
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.MainFrame.NavigationService.Navigate(new HomePage());
        }


        private void BtSettings_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            app.logger.Info("Пользователь произвел переход на страницу Settings");
            parentWindow.MainFrame.NavigationService.Navigate(new SettingsPage());
        }

    
        private void BtSystemTest_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            app.logger.Info("Пользователь произвел переход на страницу Test");
            parentWindow.MainFrame.NavigationService.Navigate(new TestPage());
        }

        private void BtTrackControlVert_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            app.logger.Info("Пользователь произвел переход на страницу Track Y");
            parentWindow.MainFrame.NavigationService.Navigate(new TrackYPage());
        }

        private void BtTrackControlHoriz_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            app.logger.Info("Пользователь произвел переход на страницу Track X");
            parentWindow.MainFrame.NavigationService.Navigate(new TrackXPage());
        }

      

      

        private void BtDiscretControlVert_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            app.logger.Info("Пользователь произвел переход на страницу Discrete Y");
            parentWindow.MainFrame.NavigationService.Navigate(new DiscreteYPage());
        }

        private void BtDiscretControlHoriz_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            app.logger.Info("Пользователь произвел переход на страницу Discrete X");
            parentWindow.MainFrame.NavigationService.Navigate(new DiscreteXPage());
        }

        private void OnUnload(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
        }
    }
}
