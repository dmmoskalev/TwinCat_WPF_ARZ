using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TwinCAT.Ads;
using static WpfTwinCAT.MainWindow;
using ICPDAS;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Globalization;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для HomePage.xaml
    /// </summary>
    public partial class DiscreteYPage : Page
    {
        private CommunicationManager cm;
        private ObservableCollection<Point> SetOfDots = new ObservableCollection<Point>();
        private ObservableCollection<Point> SetOfDistances = new ObservableCollection<Point>();
        private TC_Axis axis_1 = new TC_Axis();
        private TC_TR tr_1 = new TC_TR();
        private TC_Axis axis_2 = new TC_Axis();
        private TC_TR tr_2 = new TC_TR();
        private Axis axis1 = new Axis(); // subclass of TC_Axis+TC_TR to store settings in XML file
        private Axis axis2 = new Axis(); // subclass of TC_Axis+TC_TR to store settings in XML file
        private double freqIn = 40;
        private float distance = 0;
        private int statemode = 0; // 0 -nothing todo; 1- move to point (dot), 2 - move to distance fwd, 3- move to distance reverse
        private int UPDN_ws_state = 0; // work stop state
        private bool productionmode = false;
        private bool busy = false;
        private string selectedbtn = "";
        //private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private HighPrecisionTimer hpt;
        private App app = (App)App.Current;


        static readonly ushort USBIO_2060 = ICPDAS_USBIO.USB2060;
        private ICPDAS_USBIO m_USBIO;
        //static readonly int COMM_TIMEOUT = 250;
        private byte[] byDOValue = new byte[1];
        private byte[] byDIValue = new byte[1];
        private bool driveIsMoving = false;

        public DiscreteYPage()
        {
            InitializeComponent();
            m_USBIO = new ICPDAS_USBIO();
            //  DispatcherTimer setup
            //dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            //dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, COMM_TIMEOUT);
            hpt = new HighPrecisionTimer(app.COMM_TIMEOUT);
            hpt.Tick += Hpt_Tick;

        }



        private void OnLoad(object sender, RoutedEventArgs e) // this event was added in HomePage.xaml header
        {
            busy = true;
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.NavPanelBottom.Visibility = Visibility.Visible;
            parentWindow.bBack.Visibility = Visibility.Hidden;
            parentWindow.brdAdmin.Visibility = Visibility.Hidden;
            parentWindow.PageTitle.Content = "Вертикальное перемещение ПОЗ по пресетам";
            productionmode = parentWindow.PRODUCTION_MODE;
            statemode = 0;
            //check USB IO device is ready?
            cm = new CommunicationManager(851);
            //dispatcherTimer.Start();
            if (productionmode)
            {
                BtnTestStart.Visibility = Visibility.Hidden;
                BtnTestStop.Visibility = Visibility.Hidden;

                if (!USB2060load())
                {
                    TxbErrorMsg.Text = "USB-IO модуль не подключен";
                }
            }


            //read settings from XML file
            cm.ReadXMLSettings(axis1, axis2, axis_1, axis_2, tr_1, tr_2);

            // read presets xml files
            SetOfDots = ReadPresetXML("dot.xml");
            SetOfDistances = ReadPresetXML("dist.xml");
            // write presets to form
            FillValues("BtDotPreset", SetOfDots);
            FillValues("BtDistPreset", SetOfDistances);

            //load acquired settings to TC real engine
            cm.tryConnect();
            if (!cm.LoadXMLSettings(axis_1, axis_2, tr_1, tr_2))
            {
                TxbErrorMsg.Text = "Нет связи с ПЛК";
            }
            busy = false;
        }

        //private void Timer_Tick(object sender, EventArgs e)
        private void Hpt_Tick(object sender, HighPrecisionTimer.TickEventArgs e)
        {
            if (!busy)
            {
                busy = true;

                Dispatcher.BeginInvoke((Action)(() => {
            try {
                        //check TWINCAT is ready?
                        // time to time the twincat is not ready and cm=null
                        if (cm is null)
                        {
                            throw new ArgumentNullException(nameof(cm));
                        }
                        else { cm.tryConnect(); }

                        if (cm.IsConnected)
                {
                    //TxbErrorMsg.Text = "";
                    //read Axis and TR vars from real TC engine
                    cm.AxisVarReadFast(axis_2, "Axis_2");
                    cm.TRVarReadFast(tr_2, "TR_2");

                    TxbErrorMsg.Text += cm.ERR_MSG;
               
                    if (!cm.GSInputsCheck(axis_2.D005_MF_InputsMonitor, axis_2.Remote))
                    {
                        TxbAlarmInfo.Text = "АВАРИЙНЫЙ СТОП ПОЗ! Проверить наличие E-Stop и положение привода";
                        stopMoving();
                        BrdAlarmInfo.Visibility = Visibility.Visible;
                    }
                    if (!cm.WSInputCheck(axis_2.D005_MF_InputsMonitor, axis_2.Remote))
                    {
                        if (UPDN_ws_state == 0)
                        {
                            stopMoving();
                            // inform the user about working stop event
                            switch (statemode)
                            {
                                case 0:

                                    int ll = (int)(tr_2.LowLimitInSteps);
                                    int ul = (int)(tr_2.UpLimitInSteps);
                                    int xp = (int)(tr_2.PositionValue);
                                    if ((Math.Abs(ll - xp) - Math.Abs(ul - xp)) < 0)
                                    {
                                        UPDN_ws_state = 3;
                                        TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме спуска ПОЗ";
                                        BrdAlarmInfo.Visibility = Visibility.Visible;
                                    }
                                    else
                                    {
                                        TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме подъема ПОЗ";
                                        BrdAlarmInfo.Visibility = Visibility.Visible;
                                        UPDN_ws_state = 2;
                                    }
                                    break;
                                case 1:
                                    TxbAlarmInfo.Text = "Достигнут рабочий концевик ПОЗ";
                                    BrdAlarmInfo.Visibility = Visibility.Visible;
                                    UPDN_ws_state = statemode;
                                    break;
                                case 2:
                                    TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме подъема ПОЗ";
                                    BrdAlarmInfo.Visibility = Visibility.Visible;
                                    UPDN_ws_state = statemode;
                                    break;
                                case 3:
                                    TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме спуска ПОЗ";
                                    BrdAlarmInfo.Visibility = Visibility.Visible;
                                    UPDN_ws_state = statemode;
                                    break;
                            }
                            // reset control buttons 
                        
                            //reset state 
                            statemode = 0;
                        }
                    }
                    else
                    { // resume working stop state
                        UPDN_ws_state = 0;
                    }

                    double currentposition = axis_2.Remote?(tr_2.PositionValue - tr_2.LowLimitInSteps) / tr_2.EncStepsPerOneMM:0;
                    LvTRPositionValue.Content = string.Format("{0:f2}мм", currentposition);

                    ResetBackgroundColor();

                }
                else
                {
                    TxbErrorMsg.Text = "Нет связи с ПЛК";
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("The connection to PLC failed: {0}", err.ToString());
            }

                    if (productionmode)
            {

                if (ReadDIValue())
                {
                    //parse byDIValue     

                    //first check if AC power is OK. If not - try to poweroff
                    int DI_3 = (int)byDIValue[0] & 8; // mask 1000  for input #3
                    if (DI_3 != 8)
                    { //Power 220v lossed
                      // to shutdown the PC send 1 to RL2. Also need to hold closed RL0&RL1 to keep UPS power. Control byte = 000111 <RL5=0:RL4=0:RL3=0:RL2=1:RL1=1:RL0=1> = 7
                        hpt.Dispose();
                        byDOValue[0] = Convert.ToByte(7);
                        _ = WriteDOValue(byDOValue);
                        cm.Dispose();
                        //dispatcherTimer.Stop();
                    }
                    else
                    {
                        int DI_1 = (int)byDIValue[0] & 2; // mask 010
                        if (DI_1 == 2) //check PS+24V is OK
                        {
                            // turn on reley RL5 (feed +24V to e-Stop line) + RL0 + RL1 (connect the accumulator to UPS +12V power double line)
                            // control byte = 100011 <RL5=1:RL4=0:RL3=0:RL2=0:RL1=1:RL0=1> = 35
                            byDOValue[0] = Convert.ToByte(35);
                            _ = WriteDOValue(byDOValue);

                            int DI_0 = (int)byDIValue[0] & 1; // mask 001
                            if (DI_0 == 1)
                            {
                                // Start button is pressed, start moving
                                startMoving();
                                driveIsMoving = true;
                            }
                            else
                            {
                                if (driveIsMoving)
                                {
                                    stopMoving();
                                    driveIsMoving = false;
                                }
                            }


                        }
                        else
                        {
                            // power supply +24V is NG, wait for ready staff
                            TxbErrorMsg.Text = "Отсутствует +24В на линии E-Stop";
                        }

                    }

                }

            }
            }));
                busy = false;
            }
        }
       
        private bool USB2060load()
        {
            int iErrCode;

            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.OpenDevice(USBIO_2060, 1)))
            {
                TxbErrorMsg.Text = "Failed to open USB-2060. ErrCode:[" + iErrCode.ToString() + "]";
                return false;
            }
            else
            {
                return true;
            }
        }
        private bool ReadDOValue()
        {
            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR == (iErrCode = m_USBIO.DO_ReadValue(byDOValue)))
            {
                return true;
            }
            else
            {
                TxbErrorMsg.Text = "Failed to read DO value. ErrCode:[" + iErrCode.ToString() + "]";
                return false;
            }
        }
        private bool ReadDIValue()
        {
            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR == (iErrCode = m_USBIO.DI_ReadValue(byDIValue)))
            {
                return true;
            }
            else
            {
                TxbErrorMsg.Text = "Failed to read DO value. ErrCode:[" + iErrCode.ToString() + "]";
                return false;
            }
        }
        private bool WriteDOValue(byte[] _byDOValue)
        {
            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.DO_WriteValue(_byDOValue)))
            {
                TxbErrorMsg.Text = "Failed to write DO value. ErrCode:[" + iErrCode.ToString() + "]";
                return false;
            }
            else
                return true;
        }

        private ObservableCollection<Point> ReadPresetXML(string filename)
        {
            // read preset .xml file    
            ObservableCollection<Point> SetOFPoints = new ObservableCollection<Point>();
            string[] presets;
            try
            {
                presets = Directory.GetFiles(Environment.CurrentDirectory + "/xml/presetY/", filename);
                XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<Point>));
                TextReader reader = new StreamReader(presets[0]);
                SetOFPoints = ser.Deserialize(reader) as ObservableCollection<Point>;
                reader.Close();

            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }

            return SetOFPoints;
        }

        private void FillValues(string buttonname, ObservableCollection<Point> setofpoints)
        {
            //fill the contents of BtDotPreset or BtDistPreset
            for (int i = 0; i < 8; i++)
            {
                if (!(FindName(buttonname + String.Format("{0:d}", i)) is Button btn))
                {
                    throw new Exception("Can't find resource " + buttonname + String.Format("{0:d}", i));
                }
                else
                {
                    btn.Content = setofpoints[i].Dist.ToString();
                }
            }

        }
        private void ValueEditCall(string sourcename, string xmlfilepath, int index)
        {
            if (!(FindName(sourcename) is Button btn_caller))
            {
                throw new Exception("Can't find resource " + sourcename);
            }

            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.ValueToEdit = (string)btn_caller.Content;
            parentWindow.SourceName = btn_caller.Name;
            parentWindow.XMLFileName = xmlfilepath;
            parentWindow.Prefix = index.ToString();
            parentWindow.TextTagBuffer = "Редактирование параметра";
            parentWindow.TextHelpBuffer = "Кнопками вверх/вниз установите значение каждого разряда";
            parentWindow.MainFrame.NavigationService.Navigate(new PresetEditPage());


        }

        public class Point
        {
            public float Dist { get; set; }
            public int Index { get; set; }

        }
        private MoveAbsolutInstruction[] MoveSetInit()
        {
            MoveAbsolutInstruction[] ms = new MoveAbsolutInstruction[16];
            for (int i = 0; i < 16; i++)
            {
                MoveAbsolutInstruction mai = new MoveAbsolutInstruction();
                mai.Acceleration = 0f;
                mai.Deceleration = 0f;
                mai.Frequency_in = 0f;
                mai.TargetDist = 0;
                mai.FWD = false;
                mai.Index = (ushort)(i + 1);
                ms[i] = mai;
            }
            return ms;
        }
        private void startMoving()
        {
            switch (statemode)
            {
                case 0:
                    //nothing todo
                    break;
                case 1:
                    // start absolute move
                    if (cm.IsConnected && axis_2.Remote)
                    {
                        MoveAbsolutInstruction[] ms = MoveSetInit();
                        ms[0].Acceleration = 3.0f;
                        ms[0].Deceleration = 1.0f;
                        ms[0].Frequency_in = 40f;
                        ms[0].FWD = false; // mx2 will check the moving direction according actual position
                        ms[0].TargetDist = (uint)(distance * tr_2.EncStepsPerOneMM * 1000 + tr_2.LowLimitInSteps); // value in encoder step
                        try
                        {
                            int handle_array = cm.client.CreateVariableHandle("TL.Tracklist");
                            cm.client.WriteAny(handle_array, ms);

                            cm.client.WriteSymbol("Axis_2.EnableAbsolute", true, reloadInfo: false);
                            TxbInfoMsg.Text = axis_2.Remote ? ("Перемещение ПОЗ на точку " + String.Format("{0:f1}", distance)) : "ПОЗ не подключен";
                            app.logger.Info("Перемещение ПОЗ по пресетам на точку " + String.Format("{0:f1}", distance));
                            cm.client.DeleteVariableHandle(handle_array);
                            statemode = 0;
                        }
                        catch (AdsException)
                        {
                            TxbErrorMsg.Text = "Нет связи с ПЛК";
                        }

                    }
                    else
                    {
                        TxbErrorMsg.Text = "Нет связи с ПЧ";
                    }
                    break;
                case 2: // relative move in fwd direction
                    if (UPDN_ws_state != 2)
                    {
                        /* if (cm.IsConnected && axis_2.Remote)
                         {
                             UInt32 targetdistance = (uint)(distance * 1000); // value in mm
                             cm.client.WriteSymbol("TR_2.DistanceTarget", targetdistance, reloadInfo: false);
                             cm.client.WriteSymbol("TR_2.DirectionRelative", true, reloadInfo: false); // true - forward direction                       
                             cm.client.WriteSymbol("Axis_2.Frequency_in", freqIn, reloadInfo: false);
                             cm.client.WriteSymbol("Axis_2.Acceleration", axis_2.Acceleration, reloadInfo: false);
                             cm.client.WriteSymbol("Axis_2.Deceleration", axis_2.Deceleration, reloadInfo: false);
                             cm.client.WriteSymbol("Axis_2.EnableRelative", true, reloadInfo: false);
                             TxbInfoMsg.Text = axis_2.Remote ? ("Перемещение ПОЗ на расстояние +" + String.Format("{0:f1}", distance)) : "ПОЗ не подключен";
                             app.logger.Info("Перемещение ПОЗ по пресетам на расстояние " + String.Format("{0:f1}", distance));
                             TxbErrorMsg.Text = "";
                         }
                         else
                         {
                             TxbErrorMsg.Text = "Нет связи с ПЧ";
                         }*/
                        if (cm.IsConnected && axis_2.Remote)
                        {
                            MoveAbsolutInstruction[] ms = MoveSetInit();
                            ms[0].Acceleration = 3.0f;
                            ms[0].Deceleration = 1.0f;
                            ms[0].Frequency_in = 40f;
                            ms[0].FWD = false; // mx2 will check the moving direction according actual position
                            ms[0].TargetDist = (uint)(distance * tr_2.EncStepsPerOneMM * 1000 + tr_2.PositionValue); // value in encoder step
                            try
                            {
                                int handle_array = cm.client.CreateVariableHandle("TL.Tracklist");
                                cm.client.WriteAny(handle_array, ms);
                                cm.client.WriteSymbol("Axis_2.EnableAbsolute", true, reloadInfo: false);                                
                                cm.client.DeleteVariableHandle(handle_array);

                                TxbInfoMsg.Text = axis_2.Remote ? ("Перемещение ПОЗ на расстояние +" + String.Format("{0:f1}", distance)) : "ПОЗ не подключен";
                                app.logger.Info("Перемещение ПОЗ по пресетам на расстояние " + String.Format("{0:f1}", distance));
                                TxbErrorMsg.Text = "";
                                statemode = 0;
                            }
                            catch (AdsException)
                            {
                                TxbErrorMsg.Text = "Нет связи с ПЛК";
                            }

                        }
                        else
                        {
                            TxbErrorMsg.Text = "Нет связи с ПЧ";
                        }
                    }
                    break;
                case 3:// relative move in reverse direction
                    if (UPDN_ws_state != 3)
                    {
                        /*if (cm.IsConnected && axis_2.Remote)
                        {
                            UInt32 targetdistance = (uint)(distance * 1000);
                            cm.client.WriteSymbol("TR_2.DistanceTarget", targetdistance, reloadInfo: false);
                            cm.client.WriteSymbol("TR_2.DirectionRelative", false, reloadInfo: false); // false - reverse direction                       
                            cm.client.WriteSymbol("Axis_2.Frequency_in", freqIn, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Acceleration", axis_2.Acceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Deceleration", axis_2.Deceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableRelative", true, reloadInfo: false);
                            TxbInfoMsg.Text = axis_2.Remote ? ("Перемещение ПОЗ на расстояние -" + String.Format("{0:f1}", distance)) : "ПОЗ не подключен";
                            app.logger.Info("Перемещение ПОЗ по пресетам на расстояние " + String.Format("{0:f1}", distance));
                            TxbErrorMsg.Text = "";
                        }
                        else
                        {
                            TxbErrorMsg.Text = "Нет связи с ПЧ";
                        }*/
                        if (cm.IsConnected && axis_2.Remote)
                        {
                            MoveAbsolutInstruction[] ms = MoveSetInit();
                            ms[0].Acceleration = 3.0f;
                            ms[0].Deceleration = 1.0f;
                            ms[0].Frequency_in = 40f;
                            ms[0].FWD = false; // mx2 will check the moving direction according actual position
                            ms[0].TargetDist = (uint)( tr_2.PositionValue - distance * tr_2.EncStepsPerOneMM * 1000); // value in encoder step
                            try
                            {
                                int handle_array = cm.client.CreateVariableHandle("TL.Tracklist");
                                cm.client.WriteAny(handle_array, ms);
                                cm.client.WriteSymbol("Axis_2.EnableAbsolute", true, reloadInfo: false);
                                cm.client.DeleteVariableHandle(handle_array);

                                TxbInfoMsg.Text = axis_2.Remote ? ("Перемещение ПОЗ на расстояние +" + String.Format("{0:f1}", distance)) : "ПОЗ не подключен";
                                app.logger.Info("Перемещение ПОЗ по пресетам на расстояние " + String.Format("{0:f1}", distance));
                                TxbErrorMsg.Text = "";
                                statemode = 0;
                            }
                            catch (AdsException)
                            {
                                TxbErrorMsg.Text = "Нет связи с ПЛК";
                            }

                        }
                        else
                        {
                            TxbErrorMsg.Text = "Нет связи с ПЧ";
                        }
                    }
                    break;


            }

        }
        private void stopMoving()
        {
            TxbInfoMsg.Text = "";
            try
            {
                switch (statemode)
                {
                    case 0:
                        //nothing todo
                        if (cm.IsConnected && axis_2.Remote)
                        {
                            cm.client.WriteSymbol("Axis_2.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableAbsolute", false, reloadInfo: false);
                            TxbErrorMsg.Text = "";
                            //selectedbtn = "";
                            app.logger.Info("Остановка движения по пресетам");
                            
                        }
                        else
                        {
                            TxbErrorMsg.Text = "Нет связи с ПЧ";
                        }
                        break;
                    case 1:
                        // stop absolute move
                        if (cm.IsConnected && axis_2.Remote)
                        {
                            cm.client.WriteSymbol("Axis_2.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableAbsolute", false, reloadInfo: false);
                            TxbErrorMsg.Text = "";
                            //selectedbtn = "";
                            app.logger.Info("Остановка движения по пресетам");
                            statemode = 0;
                        }
                        else
                        {
                            TxbErrorMsg.Text = "Нет связи с ПЧ";
                        }
                        break;
                    case 2: // stop relative move
                        if (cm.IsConnected && axis_2.Remote)
                        {
                            cm.client.WriteSymbol("Axis_2.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableAbsolute", false, reloadInfo: false);
                            TxbErrorMsg.Text = "";
                            //selectedbtn = "";
                            app.logger.Info("Остановка движения по пресетам");
                            statemode = 0;
                        }
                        else
                        {
                            TxbErrorMsg.Text = "Нет связи с ПЧ";
                        }
                        break;
                    case 3: // stop relative move
                        if (cm.IsConnected && axis_2.Remote)
                        {
                            cm.client.WriteSymbol("Axis_2.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableAbsolute", false, reloadInfo: false);
                            TxbErrorMsg.Text = "";
                            //selectedbtn = "";
                            app.logger.Info("Остановка движения по пресетам");
                            statemode = 0;
                        }
                        else
                        {
                            TxbErrorMsg.Text = "Нет связи с ПЧ";
                        }
                        break;


                }

            }
            catch (AdsException)
            {
                TxbErrorMsg.Text = "Потеряна связь с ПЛК";

            }
        }


        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            int iErrCode;
            cm.Dispose();
            //dispatcherTimer.Stop();

            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.CloseDevice()) & productionmode)
            {
                TxbErrorMsg.Text = "Failed to close USB-IO. ErrCode:[" + iErrCode.ToString() + "]";
            }
            hpt.Dispose();

        }



        private void BtPresetOn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            selectedbtn = btn.Name;
            int index = int.Parse(btn.Name.Substring(btn.Name.Length - 1, 1));
            string btnname = btn.Name[0..^1];
            switch (btnname)
            {
                case "BtDotPreset":
                    //move to the point
                    statemode = 1;
                    break;

                case "BtDistPreset":
                    //move to the distance
                    statemode = index <= 3 ? 2 : 3;
                    break;

            }
            distance = float.Parse((string)btn.Content);
            btn.Background = new SolidColorBrush(Colors.Blue);
            ResetBackgroundColor();

        }
        private void ResetBackgroundColor()
        {
            uint newdotposition;
            for (int i = 0; i < 8; i++)
            {
                if (!(FindName("BtDotPreset" + String.Format("{0:d}", i)) is Button btndot))
                {
                    throw new Exception("Can't find resource BtDotPreset" + String.Format("{0:d}", i));
                }
                else
                {
                    if (btndot.Name != selectedbtn)
                    {
                        newdotposition = (uint)(float.Parse((string)btndot.Content) * 1000 * tr_2.EncStepsPerOneMM + tr_2.LowLimitInSteps);
                        if (newdotposition < tr_2.UpLimitInSteps)
                        {
                            btndot.Background = new SolidColorBrush(Color.FromArgb(255, 89, 89, 89));
                            btndot.IsEnabled = true;
                        }
                        else
                        {
                            btndot.Background = new SolidColorBrush(Colors.Red);
                            btndot.IsEnabled = false;
                        }
                    }
                }
                if (!(FindName("BtDistPreset" + String.Format("{0:d}", i)) is Button btndist))
                {
                    throw new Exception("Can't find resource BtDistPreset" + String.Format("{0:d}", i));
                }
                else
                {
                    if (btndist.Name != selectedbtn)
                    {
                        newdotposition = i <= 3 ? (uint)(tr_2.PositionValue + (float.Parse((string)btndist.Content) * 1000 * tr_2.EncStepsPerOneMM)) :
                        (uint)(tr_2.PositionValue - (float.Parse((string)btndist.Content) * 1000 * tr_2.EncStepsPerOneMM));

                        if (tr_2.LowLimitInSteps < newdotposition & newdotposition < tr_2.UpLimitInSteps)
                        {
                            btndist.Background = new SolidColorBrush(Color.FromArgb(255, 89, 89, 89));
                            btndist.IsEnabled = true;
                        }
                        else
                        {
                            btndist.Background = new SolidColorBrush(Colors.Red);
                            btndist.IsEnabled = false;
                        }
                    }
                }
            }


        }

        private void BtSet_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            int index = int.Parse(btn.Name.Substring(btn.Name.Length - 1, 1));
            string btnname = btn.Name[0..^1];
            switch (btnname)
            {

                case "BtDotSet":
                    // set the preset value
                    ValueEditCall("BtDotPreset" + String.Format("{0:d}", index), "presetY/dot.xml", index);
                    break;

                case "BtDistSet":
                    // set the preset value
                    ValueEditCall("BtDistPreset" + String.Format("{0:d}", index), "presetY/dist.xml", index);
                    break;

            }
        }

        private void teststart(object sender, RoutedEventArgs e)
        {
            startMoving();
        }

        private void teststop(object sender, RoutedEventArgs e)
        {
            stopMoving();
        }

        private void BtAlarmInfoOK_Click(object sender, RoutedEventArgs e)
        {
            BrdAlarmInfo.Visibility = Visibility.Hidden;
        }
    }
}

