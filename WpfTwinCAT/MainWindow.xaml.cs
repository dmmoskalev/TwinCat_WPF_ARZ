using System;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using NLog;
using TCatSysManagerLib;
using TwinCAT.Ads;
using WpfTwinCAT.Pages;


namespace WpfTwinCAT
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
        private string _valueToEdit;
        private string _sourceName;
        private string _textTagBuffer;
        private string _textHelpBuffer;
        private string _xmlfilename;
        private string _prefix;
        private bool usbopened = false;
        private App app = (App)App.Current;
        public bool PRODUCTION_MODE { get; set; }

        private void OnLoad(object sender, RoutedEventArgs e)
        {                       
            app.logger.Info("Application started");
        }
        
       
        public string SourceName
        {
            get => _sourceName;
            set => _sourceName = value;
        }
        public string XMLFileName
        {
            get => _xmlfilename;
            set => _xmlfilename = value;
        }
        public string TextTagBuffer
        {
            get => _textTagBuffer;
            set => _textTagBuffer = value;
        }
        public string TextHelpBuffer
        {
            get => _textHelpBuffer;
            set => _textHelpBuffer = value;
        }
        public string ValueToEdit
        {
            get => _valueToEdit;
            set => _valueToEdit = value;
        }
        public string Prefix
        {
            get => _prefix;
            set => _prefix = value;
        }
        public bool USBOPENED
        {
            get => usbopened;
            set => usbopened = value;
        }


        private void goAdmin(object sender, RoutedEventArgs e)
        {
            MainFrame.NavigationService.Navigate(new LoginPage());
        }

        private void goHome(object sender, RoutedEventArgs e)
        {
            MainFrame.NavigationService.Navigate(new HomePage());
        }

        private void goTest(object sender, RoutedEventArgs e)
        {
            MainFrame.NavigationService.Navigate(new TestPage());
        }

        private void goTrack(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            switch (btn.Name)
            {
                case "bTrackXL":
                    MainFrame.NavigationService.Navigate(new TrackXPage());
                    break;
                case "bTrackXD":
                    MainFrame.NavigationService.Navigate(new DiscreteXPage());
                    break;
                case "bTrackYL":
                    MainFrame.NavigationService.Navigate(new TrackYPage());
                    break;
                case "bTrackYD":
                    MainFrame.NavigationService.Navigate(new DiscreteYPage());
                    break;
            }      
        }

        private void goSetting(object sender, RoutedEventArgs e)
        {
            // MainFrame.NavigationService.Navigate(new SettingsPage());
            // according customer request the setting page only must be under admin password
            MainFrame.NavigationService.Navigate(new LoginPage());
        }

        private void goDefault(object sender, RoutedEventArgs e)
        {

        }

        private void goBack(object sender, RoutedEventArgs e)
        {
            MainFrame.NavigationService.GoBack();
        }

        private void SaveAxisSettingsTEST()

        {
            // Axis parameters packing scheme
            Param p0 = new Param("1", "2", "3", "4", "5", "6");
            Param p1 = new Param("1", "2", "3", "4", "5", "6");
            Param p2 = new Param("1", "2", "3", "4", "5", "6");
            Param p3 = new Param("1", "2", "3", "4", "5", "6");
            Param[] ArrayOfParam = new Param[] { p0, p1, p2, p3 };

            Group g0 = new Group("00", "Параметры Энкодера", ArrayOfParam);
            Group g1 = new Group("01", "Параметры Конвертора", ArrayOfParam);
            Group[] ArrayOfGroup = new Group[] { g0, g1 };

            Axis axis1 = new Axis("00", "1", ArrayOfGroup);
            Axis axis2 = new Axis("01", "2", ArrayOfGroup);
            Axis[] ArrayOfAxes = new Axis[] { axis1, axis2 };

            XmlSerializer ser = new XmlSerializer(typeof(Axis[]));
            TextWriter writer = new StreamWriter(axis1.XMLFileName, false, System.Text.Encoding.UTF8); //false here means 'to rewrite file' if true - append new staff to the end of file
            ser.Serialize(writer, ArrayOfAxes);
            writer.Close();
        }
        private void LoadAxisSettings()
        {         
            Axis axis1 = new Axis();
            Axis axis2 = new Axis();
            Axis[] ArrayOfAxes = new Axis[] { axis1, axis2 };
                              
            if (File.Exists(axis1.XMLFileName))
            {
                XmlSerializer ser = new XmlSerializer(typeof(Axis[]));
                TextReader reader = new StreamReader(axis1.XMLFileName);
                ArrayOfAxes = ser.Deserialize(reader) as Axis[];
                reader.Close();
               // example of element addressing
                //ArrayOfAxes[0].ArrayOfGroupes[0].ArrayOfParameters[0].Descriptor
            }
            else { throw new Exception("File " + axis1.XMLFileName + " is not founded"); }
        }

        private void OnClose(object sender, EventArgs e)
        {
            app.logger.Info("Application closed");
            Environment.Exit(0);
        }

        public class CommunicationManager : IDisposable
        {
            private int port;
            public TcAdsClient client = new TcAdsClient();           
            private bool connected;
            private string errormsg;
            public StateInfo RouterState;
            private App app = (App)App.Current;
            public string ERR_MSG
            {
                get { return errormsg; }
            }
            private DateTime? lastErrorTime = null;
            public CommunicationManager(int port)
            {
                this.port = port;
            }
            public bool IsConnected
            {
                get { return connected; }
            }           
           
            public void tryConnect()
            {
                if (!connected)
                {
                    if (lastErrorTime.HasValue)
                    {
                        // wait a bit before re-establishing connection
                        var elapsed = DateTime.Now.Subtract(lastErrorTime.Value);
                        if(elapsed.TotalMilliseconds < 3000)
                        {
                            return;
                        }
                    }
                    try
                    {
                        client.Connect(port);
                        connected = client.IsConnected;                  
                        RouterState = client.ReadState();
                    }
                    catch (AdsException)
                    {
                        connectError();
                    }             
                }
               else
                {
                    try
                    {
                        if(!client.Disposed) RouterState = client.ReadState();
                    }
                    catch (AdsException)
                    {
                        connectError();
                    }
                }
            }
            private void connectError()
            {
                connected = false;
                lastErrorTime = DateTime.Now;
                app.logger.Error("Error during connection to TwinCAT PLC");
            }
            public void AxisVarRead(TC_Axis axis, string axisname)
            {
                //load axis variables            
                if (connected)
                {
                    axis.Name = axisname;                   
                    int Alarm_Fault_Handle = 0;
                    int Alarm_Status_Handle = 0;

                    try
                    {
                        axis.ADS_address = DecodeAmsAddr((AmsAddr)client.ReadSymbol(axisname + ".ADS_address", typeof(AmsAddr), false));
                        // read STRING vars      
                        Alarm_Fault_Handle = client.CreateVariableHandle(axisname + ".Alarm_Fault");
                        Alarm_Status_Handle = client.CreateVariableHandle(axisname + ".Alarm_Status");                        
                        axis.Alarm_Fault = client.ReadAnyString(Alarm_Fault_Handle, 80, Encoding.Default);
                        axis.Alarm_Status = client.ReadAnyString(Alarm_Status_Handle, 80, Encoding.Default);                        
                        client.DeleteVariableHandle(Alarm_Fault_Handle);
                        client.DeleteVariableHandle(Alarm_Status_Handle);
                        //read Status bits
                        //axis.Status = (UInt16)client.ReadSymbol(axisname + ".Status", typeof(UInt16), false);
                        axis.Fault = (bool)client.ReadSymbol(axisname + ".Fault", typeof(bool), false);
                        axis.During_Fwd = (bool)client.ReadSymbol(axisname + ".During_Fwd", typeof(bool), false);
                        axis.During_Rev = (bool)client.ReadSymbol(axisname + ".During_Rev", typeof(bool), false);
                        axis.Warning = (bool)client.ReadSymbol(axisname + ".Warning", typeof(bool), false);
                        axis.Remote = (bool)client.ReadSymbol(axisname + ".Remote", typeof(bool), false);
                        axis.Connection_Error = (bool)client.ReadSymbol(axisname + ".Connection_Error", typeof(bool), false);
                        axis.Freq_Match = (bool)client.ReadSymbol(axisname + ".Freq_Match", typeof(bool), false);
                        //read move controls                        
                        axis.EnableFreeRun = (bool)client.ReadSymbol(axisname + ".EnableFreeRun", typeof(bool), false);
                        axis.EnableAbsolute = (bool)client.ReadSymbol(axisname + ".EnableAbsolute", typeof(bool), false);
                        axis.EnableRelative = (bool)client.ReadSymbol(axisname + ".EnableRelative", typeof(bool), false);
                        axis.Fwd = (bool)client.ReadSymbol(axisname + ".Fwd", typeof(bool), false);
                        axis.Rev = (bool)client.ReadSymbol(axisname + ".Rev", typeof(bool), false);
                        axis.Fault_Reset = (bool)client.ReadSymbol(axisname + ".Fault_Reset", typeof(bool), false);
                        // read velocity params
                        axis.Frequency_in = (double)client.ReadSymbol(axisname + ".Frequency_in", typeof(double), false);
                        axis.FreqReference = (UInt16)client.ReadSymbol(axisname + ".FreqReference", typeof(UInt16), false);
                        axis.Frequency_in_SlowMotion = (double)client.ReadSymbol(axisname + ".Frequency_in_SlowMotion", typeof(double), false);
                        axis.Acceleration = (double)client.ReadSymbol(axisname + ".Acceleration", typeof(double), false);
                        axis.F002_1st_Acceleration = (UInt32)client.ReadSymbol(axisname + ".F002_1st_Acceleration", typeof(UInt32), false);
                        axis.Deceleration = (double)client.ReadSymbol(axisname + ".Deceleration", typeof(double), false);
                        axis.F003_1st_Deceleration = (UInt32)client.ReadSymbol(axisname + ".F003_1st_Deceleration", typeof(UInt32), false);
                        // read fault states
                        axis.D081_FaultCause = (UInt16)client.ReadSymbol(axisname + ".D081_FaultCause", typeof(UInt16), false);
                        axis.D081_FaultInverterStatus = (UInt16)client.ReadSymbol(axisname + ".D081_FaultInverterStatus", typeof(UInt16), false);
                        axis.D005_MF_InputsMonitor = (UInt16)client.ReadSymbol(axisname + ".D005_MF_InputsMonitor", typeof(UInt16), false);
                        axis.WorkingLimitNotAchieved = (bool)client.ReadSymbol(axisname + ".WorkingLimitNotAchieved", typeof(bool), false);
                        //read motor settings
                        axis.MotorMaxRevPerMin = (UInt16)client.ReadSymbol(axisname + ".MotorMaxRevPerMin", typeof(UInt16), false);
                        axis.ReductionCx = (double)client.ReadSymbol(axisname + ".ReductionCx", typeof(double), false);
                        axis.DistancePerRevInMM = (double)client.ReadSymbol(axisname + ".DistancePerRevInMM", typeof(double), false);
                        axis.JogDistanceInMM = (double)client.ReadSymbol(axisname + ".JogDistanceInMM", typeof(double), false);
                        axis.ScanPeriod = (UInt32)client.ReadSymbol(axisname + ".ScanPeriod", typeof(UInt32), false);
                        #region
                        /*
                            ADS_address AT %I*:AMSADDR;+++++++++++
	                        Output_Frequency_Monitor AT %I*: UINT;
	                        Output_Current_Monitor AT %I*: UINT;
	                        //slave state PLC report
	                        State AT %I*: UINT;
	                        //invertor status
	                        Status AT %I*: UINT;+++++++++++++++++++++
	                        //monitor status vars
	                        D081_FaultCause AT %I*: INT;+++++++++++++++++++
	                        D081_FaultInverterStatus AT %I*: INT;++++++++++++++++++++++
	                        D005_MF_InputsMonitor AT %I*: INT;+++++++++++++++++++++++++
	                        MotorMaxRevPerMin:UINT:=1350; // maximum revolutions per minute on max frequency 50Hz - motor parametr
	                        ReductionCx:REAL:=20.0; //reduction coefficient in case the reductor is installed
	                        DistancePerRevInMM:REAL:=680.0; //distance passed during one revolution of drum driver in mm
	                        JogDistanceInMM:REAL :=50; // override lomit back jog distance (jump back) in mm
	
	                        Alarm_Fault: STRING[256];++++++++++++++++++++
	                        Alarm_Status: STRING[256];+++++++++++++++++++++++
	                        //output command and parameters
	                        Command AT %Q*: UINT;
	
	                        // move monitor status
	                        During_Fwd AT %Q*: BOOL;
	                        During_Rev AT %Q*: BOOL;
	                        Fault AT %Q*: BOOL;+++++++++++++
	                        Warning AT %Q*: BOOL;+++++++++++++++
	                        Remote AT %Q*: BOOL;++++++++++++++++++
	                        Freq_Match AT %Q*: BOOL;
	                        Connection_Error AT %Q*: BOOL;++++++++++++++++++++++
	                        Frequency_out AT %Q*: REAL;
	
	                        //move controls
	
	                        EnableFreeRun AT %Q*: BOOL;
	                        EnableAbsolute AT %Q*: BOOL;
	                        EnableRelative AT %Q*: BOOL;
	                        Fwd AT %Q*:BOOL;
	                        Rev AT %Q*:BOOL;
                            Fault_Reset AT %Q*: BOOL;

	                        Frequency_in AT %Q*: REAL;
                            FreqReference AT %Q*: UINT;

	                        Frequency_in_SlowMotion AT %Q*: REAL:=5.0;

	                        Acceleration AT %Q*: REAL;
                            F002_1st_Acceleration AT %Q*: DINT;

	                        Deceleration AT %Q*: REAL;	        
	                        F003_1st_Deceleration AT %Q* : DINT;	        
	        
	                        WorkingLimitNotAchieved AT %Q*: BOOL;+++++++++++++++++
	                        ScanPeriod:UDINT :=10; // system PlcTask scan period in millisec++++*/
                        #endregion
                    }
                    catch (Exception e)
                    {
                        errormsg = "Ошибка подключения: " + e.Message + "\r\n";
                    }

                }
            }
            public void AxisVarReadMidFast(TC_Axis axis, string axisname)
            {
                //load axis variables            
                if (connected)
                {
                    axis.Name = axisname;
                    
                    try
                    {                        
                        axis.Remote = (bool)client.ReadSymbol(axisname + ".Remote", typeof(bool), false);
                        axis.D005_MF_InputsMonitor = (UInt16)client.ReadSymbol(axisname + ".D005_MF_InputsMonitor", typeof(UInt16), false);                      
                        axis.MotorMaxRevPerMin = (UInt16)client.ReadSymbol(axisname + ".MotorMaxRevPerMin", typeof(UInt16), false);
                        axis.ReductionCx = (double)client.ReadSymbol(axisname + ".ReductionCx", typeof(double), false);
                        axis.DistancePerRevInMM = (double)client.ReadSymbol(axisname + ".DistancePerRevInMM", typeof(double), false);                       
                       
                    }
                    catch (Exception e)
                    {
                        errormsg = "Ошибка подключения: " + e.Message + "\r\n";
                    }

                }
            }
            public void AxisVarReadFast(TC_Axis axis, string axisname)
            {
                //load axis variables            
                if (connected)
                {
                    axis.Name = axisname;
                    
                    try
                    {                       
                        axis.Remote = (bool)client.ReadSymbol(axisname + ".Remote", typeof(bool), false);                      
                        axis.D005_MF_InputsMonitor = (UInt16)client.ReadSymbol(axisname + ".D005_MF_InputsMonitor", typeof(UInt16), false);                   
                      
                    }
                    catch (Exception e)
                    {
                        errormsg = "Ошибка подключения: " + e.Message + "\r\n";
                    }

                }
            }
            public void TRVarRead(TC_TR tr, string trname)
            {
                //load axis variables            
                if (connected)
                {
                    tr.Name = trname;   
                    try
                    {
                        tr.ADS_address = DecodeAmsAddr((AmsAddr)client.ReadSymbol(trname + ".ADS_address", typeof(AmsAddr), false));                       

                        tr.PositionValue = (UInt32)client.ReadSymbol(trname + ".PositionValue", typeof(UInt32), false);
                        tr.TimeStamp = (UInt32)client.ReadSymbol(trname + ".TimeStamp", typeof(UInt32), false);
                        tr.State = (UInt16)client.ReadSymbol(trname + ".State", typeof(UInt16), false);

                        tr.EncStepsPerOneMM = (double)client.ReadSymbol(trname + ".EncStepsPerOneMM", typeof(double), false);
                        tr.PositionTarget = (UInt32)client.ReadSymbol(trname + ".PositionTarget", typeof(UInt32), false);
                        tr.DistanceTarget = (UInt32)client.ReadSymbol(trname + ".DistanceTarget", typeof(UInt32), false);
                        tr.DirectionRelative = (bool)client.ReadSymbol(trname + ".DirectionRelative", typeof(bool), false);
                        tr.Alarm = (UInt16)client.ReadSymbol(trname + ".Alarm", typeof(UInt16), false);

                        tr.UpLimitInSteps = (UInt32)client.ReadSymbol(trname + ".UpLimitInSteps", typeof(UInt32), false);
                        tr.LowLimitInSteps = (UInt32)client.ReadSymbol(trname + ".LowLimitInSteps", typeof(UInt32), false);
                        #region
                        /*
                       ADS_address AT %I*:AMSADDR;
                       PositionValue AT %I*:UDINT;
                       TimeStamp AT %I*:UDINT;
                       EncStepsPerOneMM : REAL :=12.04; //= 8192 steps per rev/680mm = 12.04
                       PositionTarget:UDINT; // new position target in mm
                       DistanceTarget:UDINT; // distance for relative moving in mm
                       DirectionRelative:BOOL; // true = fwd, false = backward
                       Alarm AT %I*:UINT;
                       State AT %I*:UINT;
                       UpLimitInSteps:UDINT := 245760; // 30 revolutions (apr 20.4m), each rev= 8192 steps
                       LowLimitInSteps:UDINT :=300;*/
                        #endregion
                    }
                    catch (Exception e)
                    {
                        errormsg = "Ошибка подключения: " + e.Message + "\r\n";
                    }

                }
            }
            public void TRVarReadFast(TC_TR tr, string trname)
            {
                //load only high usable axis variables            
                if (connected)
                {
                    tr.Name = trname;
                    try
                    {
                        tr.PositionValue = (UInt32)client.ReadSymbol(trname + ".PositionValue", typeof(UInt32), false);  
                        tr.EncStepsPerOneMM = (double)client.ReadSymbol(trname + ".EncStepsPerOneMM", typeof(double), false);  
                        tr.UpLimitInSteps = (UInt32)client.ReadSymbol(trname + ".UpLimitInSteps", typeof(UInt32), false);
                        tr.LowLimitInSteps = (UInt32)client.ReadSymbol(trname + ".LowLimitInSteps", typeof(UInt32), false);  
                    }
                    catch (Exception e)
                    {
                        errormsg = "Ошибка подключения: " + e.Message + "\r\n";
                    }

                }
            }
            private string DecodeAmsAddr(AmsAddr structure)
            {
                string resAddr = "NetID: ";
                for (int i = 0; i < 6; i++)
                {
                    resAddr += i == 5 ? (structure.netId[i].ToString()) : (structure.netId[i].ToString() + ".");
                }
                resAddr += ", port: " + structure.port.ToString();
                return resAddr;
            }
            public void ReadXMLSettings(Axis axis1, Axis axis2, TC_Axis axis_1, TC_Axis axis_2, TC_TR tr_1, TC_TR tr_2)
            {
                // read settings from XML file
                Axis[] ArrayOfAxes = new Axis[] { axis1, axis2 };

                int ind = -1;
                int group = -1;
                int axn = -1;
                string adr = "";

                if (File.Exists(axis1.XMLFileName))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Axis[]));
                    TextReader reader = new StreamReader(axis1.XMLFileName);
                    ArrayOfAxes = ser.Deserialize(reader) as Axis[];
                    reader.Close();
                    foreach (Axis a in ArrayOfAxes)
                    {
                        axn += 1;
                        if (axn > 0)
                            axis_2.Name = a.Name;
                        else
                            axis_1.Name = a.Name;


                        foreach (Group g in a.ArrayOfGroupes)
                        {
                            group += 1;
                            ind = 0;
                            if (group == 2) { group = 0; }; //page shift

                            foreach (Param p in g.ArrayOfParameters)
                            {
                                
                               // if (ind == 7) { ind = 0; }; //page shift
                                                            // build the address of parameter
                                adr = string.Format("{0:d2}", axn) + string.Format("{0:d2}", group) + string.Format("{0:d2}", ind);

                                switch (adr)
                                {
                                    case "000000": tr_1.Resolution = uint.Parse(p.Value); break;
                                    case "000001": tr_1.EncStepsPerOneMM = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "000002": tr_1.LowLimitInSteps = uint.Parse(p.Value); break; // here it keeps value in MM (late edition) 
                                    case "000003": tr_1.UpLimitInSteps = uint.Parse(p.Value); break; // here it keeps value in MM (late edition) 
                                    case "000004": axis_1.JogDistanceInMM = double.Parse(p.Value, CultureInfo.InvariantCulture); break;//replace
                                    case "000005": axis_1.DistancePerRevInMM = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "000006": 
                                        { 
                                            tr_1.ZeroLevel = uint.Parse(p.Value);
                                            tr_1.LowLimitInSteps = (uint)((tr_1.LowLimitInSteps + tr_1.ZeroLevel) * tr_1.EncStepsPerOneMM); // now it keeps value in Steps and include ZeroLevel base (late edition 03-2022) 
                                            tr_1.UpLimitInSteps = (uint)((tr_1.UpLimitInSteps + tr_1.ZeroLevel) * tr_1.EncStepsPerOneMM); // now it keeps value in Steps and include ZeroLevel base (late edition 03-2022) 
                                            break; 
                                        }
                                    case "000100": axis_1.Frequency_in_SlowMotion = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "000101": axis_1.Acceleration = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "000102": axis_1.Deceleration = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "000103": axis_1.MotorMaxRevPerMin = uint.Parse(p.Value); break;
                                    case "000104": axis_1.ReductionCx = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "000105": axis_1.ScanPeriod = uint.Parse(p.Value); break;

                                    case "010000": tr_2.Resolution = uint.Parse(p.Value); break;
                                    case "010001": tr_2.EncStepsPerOneMM = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "010002": tr_2.LowLimitInSteps = uint.Parse(p.Value); break; // here it keeps value in MM (late edition)
                                    case "010003": tr_2.UpLimitInSteps = uint.Parse(p.Value); break; // here it keeps value in MM (late edition)
                                    case "010004": axis_2.JogDistanceInMM = double.Parse(p.Value, CultureInfo.InvariantCulture); break;//replace
                                    case "010005": axis_2.DistancePerRevInMM = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "010006":
                                        {
                                            tr_2.ZeroLevel = uint.Parse(p.Value);
                                            tr_2.LowLimitInSteps = (uint)((tr_2.LowLimitInSteps + tr_2.ZeroLevel) * tr_2.EncStepsPerOneMM); // now it keeps value in Steps and include ZeroLevel base (late edition 03-2022) 
                                            tr_2.UpLimitInSteps = (uint)((tr_2.UpLimitInSteps + tr_2.ZeroLevel) * tr_2.EncStepsPerOneMM); // now it keeps value in Steps and include ZeroLevel base (late edition 03-2022) 
                                            break;
                                        }
                                    case "010100": axis_2.Frequency_in_SlowMotion = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "010101": axis_2.Acceleration = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "010102": axis_2.Deceleration = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "010103": axis_2.MotorMaxRevPerMin = uint.Parse(p.Value); break;
                                    case "010104": axis_2.ReductionCx = double.Parse(p.Value, CultureInfo.InvariantCulture); break;
                                    case "010105": axis_2.ScanPeriod = uint.Parse(p.Value); break;
                                }
                                ind += 1;
                            }
                        }
                    }

                }
                else { throw new Exception("File " + axis1.XMLFileName + " is not founded"); }

            }
            public bool LoadXMLSettings(TC_Axis axis_1, TC_Axis axis_2, TC_TR tr_1, TC_TR tr_2)
            {
                //load preliminary acquired settings from XML file to TC real engine
                bool res;
                if (connected)
                {
                    try
                    {
                        client.WriteSymbol("Axis_1.Rev", false, reloadInfo: true);
                        client.WriteSymbol("Axis_1.Fwd", false, reloadInfo: true);

                        client.WriteSymbol("Axis_1.JogDistanceInMM", axis_1.JogDistanceInMM, reloadInfo: true);
                        client.WriteSymbol("Axis_1.DistancePerRevInMM", axis_1.DistancePerRevInMM, reloadInfo: true);
                        client.WriteSymbol("Axis_1.Frequency_in_SlowMotion", axis_1.Frequency_in_SlowMotion, reloadInfo: true);
                        client.WriteSymbol("Axis_1.Acceleration", axis_1.Acceleration, reloadInfo: true);
                        client.WriteSymbol("Axis_1.Deceleration", axis_1.Deceleration, reloadInfo: true);
                        client.WriteSymbol("Axis_1.MotorMaxRevPerMin", axis_1.MotorMaxRevPerMin, reloadInfo: true);
                        client.WriteSymbol("Axis_1.ReductionCx", axis_1.ReductionCx, reloadInfo: true);
                        client.WriteSymbol("Axis_1.ScanPeriod", axis_1.ScanPeriod, reloadInfo: true);

                        client.WriteSymbol("tr_1.Resolution", tr_1.Resolution, reloadInfo: true);
                        client.WriteSymbol("tr_1.EncStepsPerOneMM", tr_1.EncStepsPerOneMM, reloadInfo: true);
                        client.WriteSymbol("tr_1.LowLimitInSteps", tr_1.LowLimitInSteps, reloadInfo: true);
                        client.WriteSymbol("tr_1.UpLimitInSteps", tr_1.UpLimitInSteps, reloadInfo: true);
                        res = true;
                    }
                    catch (Exception e)
                    {
                        errormsg = "Ошибка записи настроек в АРЗ: " + e.Message + "\r\n";
                        return false;
                    }
                    try
                    {
                        client.WriteSymbol("Axis_2.Rev", false, reloadInfo: true);
                        client.WriteSymbol("Axis_2.Fwd", false, reloadInfo: true);

                        client.WriteSymbol("Axis_2.JogDistanceInMM", axis_2.JogDistanceInMM, reloadInfo: true);
                        client.WriteSymbol("Axis_2.DistancePerRevInMM", axis_2.DistancePerRevInMM, reloadInfo: true);
                        client.WriteSymbol("Axis_2.Frequency_in_SlowMotion", axis_2.Frequency_in_SlowMotion, reloadInfo: true);
                        client.WriteSymbol("Axis_2.Acceleration", axis_2.Acceleration, reloadInfo: true);
                        client.WriteSymbol("Axis_2.Deceleration", axis_2.Deceleration, reloadInfo: true);
                        client.WriteSymbol("Axis_2.MotorMaxRevPerMin", axis_2.MotorMaxRevPerMin, reloadInfo: true);
                        client.WriteSymbol("Axis_2.ReductionCx", axis_2.ReductionCx, reloadInfo: true);
                        client.WriteSymbol("Axis_2.ScanPeriod", axis_2.ScanPeriod, reloadInfo: true);

                        client.WriteSymbol("tr_2.Resolution", tr_2.Resolution, reloadInfo: true);
                        client.WriteSymbol("tr_2.EncStepsPerOneMM", tr_2.EncStepsPerOneMM, reloadInfo: true);
                        client.WriteSymbol("tr_2.LowLimitInSteps", tr_2.LowLimitInSteps, reloadInfo: true);
                        client.WriteSymbol("tr_2.UpLimitInSteps", tr_2.UpLimitInSteps, reloadInfo: true);
                        res = true;
                    }
                    catch (Exception e)
                    {
                        errormsg = "Ошибка записи настроек в ПОЗ: " + e.Message + "\r\n";
                        return false;
                    }

                }
                else
                {
                    errormsg = "Сервис ПЛК не активен";
                    return false;
                }
                return res;

            }
            public bool LoadTrackList(MoveAbsolutInstruction[] moveset)
            {
                bool res;
                if (connected)
                {
                    try
                    { //TL.Tracklist correspond to moveset array of moveAbsInstructuions
                        int handle_array = client.CreateVariableHandle("TL.Tracklist");
                        client.WriteAny(handle_array, moveset);
                        client.DeleteVariableHandle(handle_array);
                        res = true;
                    }
                    catch (Exception e)
                    {
                        errormsg = "Ошибка записи трека в АРЗ: " + e.Message + "\r\n";
                        return false;
                    }

                }
                else res = false;
                return res;
            }
            public bool GSInputsCheck(int d005, bool remote)
            {   //check for safety inputs GS1 (E-Stop), GS2 (Emergency Stop)
                if (remote)
                {
                    byte[] byD005Value = new byte[1];
                    byD005Value[0] = Convert.ToByte(d005);
                    int GS1 = (int)byD005Value[0] & 4; // mask 00000100 => pin3 in convertor MF inputs
                    int GS2 = (int)byD005Value[0] & 8; // mask 00001000 => pin4 in convertor MF inputs
                    return (GS1 == 4) & (GS2 == 8);
                }
                else
                {
                    // axis is not active - pass it
                    return true;
                }

            }

            public bool WSInputCheck(int d005, bool remote)
            {   //check for work (not alarm) stop (6 pin of MF input)
                if (remote)
                {
                    byte[] byD005Value = new byte[1];
                    byD005Value[0] = Convert.ToByte(d005);
                    int WS = (int)byD005Value[0] & 32; // mask 00100000 => pin6 in convertor MF inputs                   
                    return WS == 32;
                }
                else
                {
                    // axis is not active - pass it
                    return true;
                }

            }

            public void Dispose()
            {
                client.Dispose();
            }
            public void Shutdown()
            {
                app.logger.Info("Application shutdown");
                ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
                mcWin32.Get();

                // You can't shutdown without security privileges
                mcWin32.Scope.Options.EnablePrivileges = true;
                ManagementBaseObject mboShutdownParams =
                         mcWin32.GetMethodParameters("Win32Shutdown");

                // Flag 1 means we want to shut down the system. Use "2" to reboot. 8 - shutdown with poweroff 
                //mboShutdownParams["Flags"] = "1";
                mboShutdownParams["Flags"] = "8";
                mboShutdownParams["Reserved"] = "0";
                foreach (ManagementObject manObj in mcWin32.GetInstances())
                {
                    ManagementBaseObject mboShutdown = manObj.InvokeMethod("Win32Shutdown",
                                   mboShutdownParams, null);
                }
            }
        }
       
        public class SystemManager: ITcSysManager
        {
            public void OpenConfiguration(string bstrFile = "")
            {
                throw new NotImplementedException();
            }

            public void SaveConfiguration(string bstrFile = "")
            {
                throw new NotImplementedException();
            }

            public void NewConfiguration()
            {
                throw new NotImplementedException();
            }

            public void ActivateConfiguration()
            {
                throw new NotImplementedException();
            }

            public ITcSmTreeItem LookupTreeItem(string bstrItem)
            {
                throw new NotImplementedException();
            }

            public void StartRestartTwinCAT()
            {
                throw new NotImplementedException();
            }

            public bool IsTwinCATStarted()
            {
                throw new NotImplementedException();
            }

            public void LinkVariables(string bstrV1, string bstrV2, int offs1 = 0, int offs2 = 0, int size = 0)
            {
                throw new NotImplementedException();
            }

            public void UnlinkVariables(string bstrV1, string bstrV2 = "")
            {
                throw new NotImplementedException();
            }
        }
        


    }
    

}
