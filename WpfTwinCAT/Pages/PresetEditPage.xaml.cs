using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для ValueEditPage.xaml
    /// </summary>
    public partial class PresetEditPage : Page
    {
        private string _sourcename;
        private string _valueBuffer;
        private string _prefixBuffer;
        private string _xmlfilepath;
        private int _indicator;
        private string[] digValue = { "0", "0", "0", "0", "0", "0" };
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        public PresetEditPage()
        {
            InitializeComponent();
            //  DispatcherTimer setup
            dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 60);
        }
        private void OnLoad(object sender, RoutedEventArgs e) // this event was added in HomePage.xaml header
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;

            parentWindow.PageTitle.Content = "Установка пресета";

            // read value from buffer
            _valueBuffer = parentWindow.ValueToEdit;
            _prefixBuffer = parentWindow.Prefix;
            _sourcename = parentWindow.SourceName;
            _xmlfilepath = parentWindow.XMLFileName;
            StringSplitter(_valueBuffer);
            _indicator = 5;
            txbTag.Text = parentWindow.TextTagBuffer;
            txbHelp.Text = parentWindow.TextHelpBuffer;

            var onLoadStoryboard = GetMyStoryboard("Flash_5");
            onLoadStoryboard.Begin(this, true);
            dispatcherTimer.Start();

        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            //return to home page
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.MainFrame.NavigationService.Navigate(new HomePage());
        }

        private Storyboard GetMyStoryboard(string storyBoardName)
        {
            if (!(TryFindResource(storyBoardName) is Storyboard result)) throw new Exception("Can't find resource 'MyStoryboard'");
            return result;
        }

        private void BtSelector_click(object sender, RoutedEventArgs e)
        {            
            var prevStoryboard = GetMyStoryboard("Flash_" + _indicator.ToString());
            prevStoryboard.Remove(this);           
            _indicator -= 1;
            if (_indicator < 0)
            {
                SettingUpdate(_xmlfilepath);
            }
            else
            {
                var nextStoryboard = GetMyStoryboard("Flash_" + _indicator.ToString());
                nextStoryboard.Begin(this,true);
            }

        }
        private void SettingUpdate(string xmlfilepath)
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.ValueToEdit = ValueBuilder(digValue);
            XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<Point>));

            // read preset .xml file    
            ObservableCollection<Point> SetOFPoints = new ObservableCollection<Point>();
            try
            {
                string[] presets;
           
                presets = Directory.GetFiles(Environment.CurrentDirectory + "/xml/", xmlfilepath);                
                TextReader reader = new StreamReader(presets[0]);
                SetOFPoints = ser.Deserialize(reader) as ObservableCollection<Point>;
                reader.Close();
                // edit target value on base of prefix
                int index = int.Parse(_prefixBuffer);
                SetOFPoints[index].Dist = float.Parse(parentWindow.ValueToEdit, CultureInfo.InvariantCulture);
                //save file with new value                
                TextWriter writer = new StreamWriter(presets[0], false, System.Text.Encoding.UTF8); //false here means 'to rewrite file' if true - append new staff to the end of file
                ser.Serialize(writer, SetOFPoints);
                writer.Close();
                //return to settings page
                if (xmlfilepath.Contains("presetX"))
                {
                    parentWindow.MainFrame.NavigationService.Navigate(new DiscreteXPage());
                }
                else
                {
                    parentWindow.MainFrame.NavigationService.Navigate(new DiscreteYPage());
                }
               
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }
           

        }
        public class Point
        {
            public float Dist { get; set; }
            public int Index { get; set; }

        }
        private void BtCancel_click(object sender, RoutedEventArgs e)
        {
            // just get out from page
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.MainFrame.NavigationService.Navigate(new SettingsPage());
        }

        private void BtUp_click(object sender, RoutedEventArgs e)
        {// up the value of the current indicator
            if (!(FindName("LbValue_" + (_indicator).ToString()) is Label indicatorRank))
            {
                throw new Exception("Can't find resource 'LbValue_" + (_indicator).ToString());
            }
            if ((digValue[_indicator]==".") | (digValue[_indicator]==","))
            {
                digValue[_indicator] = "-1";
            }
            if ((int.Parse(digValue[_indicator]) + 1) > 9)
            {
                digValue[_indicator] = ".";
                indicatorRank.Content = ".";
            }
            else
            {
                indicatorRank.Content = (int.Parse(digValue[_indicator]) + 1).ToString();
                digValue[_indicator] = (int.Parse(digValue[_indicator]) + 1).ToString();
            }
        
        }

        private void BtDown_click(object sender, RoutedEventArgs e)
        {
            // down the value of the current indicator
            if (!(FindName("LbValue_" + (_indicator).ToString()) is Label indicatorRank))
            {
                throw new Exception("Can't find resource 'LbValue_" + (_indicator).ToString());
            }
            if ((digValue[_indicator] == ".") | (digValue[_indicator] == ","))
            {
                digValue[_indicator] = "10";
            }
            if ((int.Parse(digValue[_indicator]) - 1) <0)
            {
                digValue[_indicator] = ".";
                indicatorRank.Content = ".";
            }
            else
            {
                indicatorRank.Content = (int.Parse(digValue[_indicator]) - 1).ToString();
                digValue[_indicator] = (int.Parse(digValue[_indicator]) - 1).ToString();
            }
        }

        private void StringSplitter(string inputValue)
        {
            //clear previouse value
            //for (int i = 0; i < 6; i++) { digValue[i] = "0"; }

            // split input string value to the separate indicators
            // cut the number with more then 6 digits in base
            if (inputValue.Length > 6)
            {
                inputValue = inputValue.Substring(inputValue.Length-6,6);
            }

            int mantisa = inputValue.Length;  // condition? consequent : alternative
            for ( int m=0; m<inputValue.Length; m++)
            {
                digValue[mantisa-m-1] = inputValue.Substring(m, 1);
                if (!(FindName("LbValue_" + (mantisa - m-1).ToString()) is Label indicatorRank))
                {
                    throw new Exception("Can't find resource 'LbValue_" + (mantisa - m-1).ToString());
                }
                indicatorRank.Content = digValue[mantisa - m-1];
            }
            
        }
        private string ValueBuilder(string[] outputValue)
        {//build string in the opposite direction
            StringBuilder result = new StringBuilder("");
            int isDotCommaInText = 0;
            for(int i = 0; i < outputValue.Length; i++)
            {     // check for redundent dots         
                if (outputValue[outputValue.Length - i - 1] == ".")
                {
                    isDotCommaInText += 1;
                }
                if ((isDotCommaInText>1) & (outputValue[outputValue.Length - i - 1] == "."))
                {
                    result.Append("");
                }
                else
                    {                        
                    result.Append(outputValue[outputValue.Length - i - 1]);
                    }
                
            }
            // if first value is "." add "0" as beginning of string
            string s_result= result.ToString();
            s_result = s_result.TrimStart('0');
            s_result = s_result.Replace(",", ".");
            if (s_result.Substring(0, 1) == ".") { s_result = String.Concat("0", s_result); }
            return s_result;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
        }
    }
}
