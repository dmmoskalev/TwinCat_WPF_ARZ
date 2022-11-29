using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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
using System.Xml.Serialization;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для LoginPase.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private string _keybuilder;
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private App app = (App)App.Current;
        public LoginPage()
        {
            InitializeComponent();
            _keybuilder = "";
            //  DispatcherTimer setup
            dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 30);
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            //return to home page
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.MainFrame.NavigationService.Navigate(new HomePage());
        }


            private void OnLoad(object sender, RoutedEventArgs e) // this event was added in HomePage.xaml header
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.NavPanelBottom.Visibility = Visibility.Hidden;
            parentWindow.bBack.Visibility = Visibility.Hidden;
            parentWindow.brdAdmin.Visibility = Visibility.Hidden;
            parentWindow.PageTitle.Content = "Введите пароль Администратора";
            dispatcherTimer.Start();
        }

        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            // check the password if OK then navigate to track page
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            if (PassCheck(_keybuilder))
            {
                app.logger.Info("Осуществлен успешный вход в режим администрирования");
                parentWindow.MainFrame.NavigationService.Navigate(new AdminMenuPage());
            }
            else
            {
                app.logger.Warn("Попытка входа в режим администрирования, введен неверный пароль");
                parentWindow.MainFrame.NavigationService.Navigate(new HomePage());
            }
           

        }
        private bool PassCheck(string code)
        {
            //check code combination with sha256 key
            byte[] number = Encoding.ASCII.GetBytes(code);
            Key key = new Key();
            string outsha256 = "";

            using (SHA256 mySHA256 = SHA256.Create())
            {
                byte[] hashValue = mySHA256.ComputeHash(number);                
                for(int i=0; i < 32; i++)
                {
                    outsha256 += hashValue[i].ToString();
                }                
                //get original key                          
                if (File.Exists(key.XML_KEY_FileName))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Key));
                    TextReader reader = new StreamReader(key.XML_KEY_FileName);
                    key = ser.Deserialize(reader) as Key;
                    reader.Close();                    
                }
                else { throw new Exception("File " + key.XML_KEY_FileName + " is not founded"); }                            
            }
                return (key.KeyValue == outsha256);
        }

        private void Bt09_Click(object sender, RoutedEventArgs e)
        {
            // handler for all digit buttons
            Button senderBtn = sender as Button;
            // get index of the edited parameter            
            _keybuilder += senderBtn.Name.Substring((senderBtn.Name.Length - 1), 1);
            LbPassword.Content = _keybuilder.Replace("0","*").Replace("1", "*").Replace("2", "*").Replace("3", "*")
                .Replace("4", "*").Replace("5", "*").Replace("6", "*")
                .Replace("7", "*").Replace("8", "*").Replace("9", "*");
        }

        private void BtCor_Click(object sender, RoutedEventArgs e)
        {
            // correct the last digit
            if (_keybuilder.Length > 0) 
            { 
            _keybuilder = _keybuilder[0..^1];
            LbPassword.Content = _keybuilder.Replace("0", "*").Replace("1", "*").Replace("2", "*").Replace("3", "*")
                .Replace("4", "*").Replace("5", "*").Replace("6", "*")
                .Replace("7", "*").Replace("8", "*").Replace("9", "*");
            }
        }

        private void BtBack_Click(object sender, RoutedEventArgs e)
        {
            //erase all digits
            _keybuilder = "";
            LbPassword.Content = "********";
        }

        private void OnUnload(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
        }
    }
}
