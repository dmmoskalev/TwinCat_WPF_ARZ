using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Serialization;
using static WpfTwinCAT.MainWindow;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для ValueEditPage.xaml
    /// </summary>
    public partial class ValueEditPage : Page
    {

        private string _valueBuffer;
        private string _prefixBuffer;
        private int _indicator;
        private string[] digValue = { "0", "0", "0", "0", "0", "0" };
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        public ValueEditPage()
        {
            InitializeComponent();
            //  DispatcherTimer setup
            dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 60);
        }
        private void OnLoad(object sender, RoutedEventArgs e) // this event was added in HomePage.xaml header
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;

            parentWindow.PageTitle.Content = "Редактирование параметра";

            // read value from buffer
            _valueBuffer = parentWindow.ValueToEdit;
            _prefixBuffer = parentWindow.Prefix;
            if (_prefixBuffer=="000006" | _prefixBuffer== "010006") // in case of new calibration (prefix 000006 or 010006) the 0 value nedd to be shown on display
            {
                StringSplitter("0");
            }
            else
            {
                StringSplitter(_valueBuffer);
            }
            
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
            if (_indicator > -1)
            {
                var prevStoryboard = GetMyStoryboard("Flash_" + _indicator.ToString());
                prevStoryboard.Remove(this);

                _indicator -= 1;
                if (_indicator < 0)
                {
                    SettingUpdate();
                }
                else
                {
                    var nextStoryboard = GetMyStoryboard("Flash_" + _indicator.ToString());
                    nextStoryboard.Begin(this, true);
                }
            }

        }
        private void SettingUpdate()
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.ValueToEdit = ValueBuilder(digValue);
            
            //read setting file
            Axis axis1 = new Axis();
            Axis axis2 = new Axis();
            Axis[] ArrayOfAxes = new Axis[] { axis1, axis2 };
            XmlSerializer ser = new XmlSerializer(typeof(Axis[]));

            if (File.Exists(axis1.XMLFileName))
            {              
                TextReader reader = new StreamReader(axis1.XMLFileName);
                ArrayOfAxes = ser.Deserialize(reader) as Axis[];
                reader.Close();
                // example of element addressing
                //ArrayOfAxes[0].ArrayOfGroupes[0].ArrayOfParameters[0].Descriptor
            }
            else { throw new Exception("File " + axis1.XMLFileName + " is not founded"); }
            // edit target value on base of prefix
            int ax = int.Parse(parentWindow.Prefix.Substring(0, 2));
            int gr = int.Parse(parentWindow.Prefix.Substring(2, 2));
            int p = int.Parse(parentWindow.Prefix.Substring(4, 2));
            ArrayOfAxes[ax].ArrayOfGroupes[gr].ArrayOfParameters[p].Value = parentWindow.ValueToEdit;

            // check if prefix == 000006 or 010006 - these are indexes of calibration buttons - call ZeroLevel calculation function
            if (parentWindow.Prefix == "000006") { ArrayOfAxes[ax].ArrayOfGroupes[gr].ArrayOfParameters[p].Value = ZeroLevelCalibration("TR_1", parentWindow.ValueToEdit); }
            if (parentWindow.Prefix == "010006") { ArrayOfAxes[ax].ArrayOfGroupes[gr].ArrayOfParameters[p].Value = ZeroLevelCalibration("TR_2", parentWindow.ValueToEdit); }
            //save file with new value
            //XmlSerializer ser = new XmlSerializer(typeof(Axis[]));
            TextWriter writer = new StreamWriter(axis1.XMLFileName, false, System.Text.Encoding.UTF8); //false here means 'to rewrite file' if true - append new staff to the end of file
            ser.Serialize(writer, ArrayOfAxes);
            writer.Close();

            if (_prefixBuffer != "000006" & _prefixBuffer != "010006")
            {
                //return to settings page
                parentWindow.MainFrame.NavigationService.Navigate(new SettingsPage());
            }

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
            //string s_result= (result.ToString() == "")? "000000": result.ToString();
            string s_result = result.ToString();
            if (s_result != "000000")
            {
                s_result = s_result.TrimStart('0');
                if (s_result.Substring(0, 1) == ".") { s_result = String.Concat("0", s_result); }
            }
            else { s_result = "0"; }
            s_result = s_result.Replace(",", ".");
            return s_result;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
        }
        private string ZeroLevelCalibration(string tr_name, string basevalue)
        { // new zerolevel calculation on base of current TR position and base shift value regarding floor level
            CommunicationManager cm;
            cm = new CommunicationManager(851);
            TC_TR tr = new TC_TR();
            string newZeroLevel = "0";
            cm.tryConnect();
            if (cm.IsConnected)
            {
                cm.TRVarReadFast(tr, tr_name);
                uint currentPosition = (uint)(tr.PositionValue / tr.EncStepsPerOneMM);
                if (currentPosition > uint.Parse(basevalue))
                {
                    uint ZL = currentPosition - uint.Parse(basevalue);
                    newZeroLevel = string.Format("{0:d}", ZL);
                    txbHelp.Text = "Калибровка произведена успешно";
                }
                else { txbHelp.Text = "Не удалось произвести калибровку: начальное положение энкодера находится вне рабочего диапазона"; }
            }
            else
            {
                txbHelp.Text = "Нет связи с ПЛК, не удалось произвести калибровку";
            }
            cm.Dispose();
            return newZeroLevel;
        }
    }
}
