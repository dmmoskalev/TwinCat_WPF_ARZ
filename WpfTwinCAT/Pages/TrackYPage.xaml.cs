using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Threading;
using System.Xml.Serialization;
using ICPDAS;
using TwinCAT.Ads;
using static WpfTwinCAT.MainWindow;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для TrackYPage.xaml
    /// </summary>
    public partial class TrackYPage : Page
    {
        //private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private HighPrecisionTimer hpt;
        private ObservableCollection<Track> TrackList = new ObservableCollection<Track>();
        private ObservableCollection<DataPoint> RecordedTrack = new ObservableCollection<DataPoint>();
        private ObservableCollection<MoveAbsolutInstruction[]> MoveTrack;
        private ViewModel lViewModel;
        private CommunicationManager cm;
        private App app = (App)App.Current;
        private ManagementEventWatcher watcher = new ManagementEventWatcher();
        //private WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 or EventType = 3");
        private string driveName;
        private int eventType;
        private bool nothingtodo;
        private bool productionmode = false;
        private bool busy = false;
        private TC_Axis axis_1 = new TC_Axis();
        private TC_TR tr_1 = new TC_TR();
        private TC_Axis axis_2 = new TC_Axis();
        private TC_TR tr_2 = new TC_TR();
        private Axis axis1 = new Axis(); // subclass of TC_Axis+TC_TR to store settings in XML file
        private Axis axis2 = new Axis(); // subclass of TC_Axis+TC_TR to store settings in XML file
        private static float Vmax = 50.0f;
        private static int gap_in_mm = 100; // the slack of ARZ position in mm
        private static int gap_in_steps = 0; // the slack of position in encoder steps
        private double SceneWidth = 10000; // the half of scene width in millimeters
        private int encoder_auto_increment = 0; // only for test buttons simulation
        private int test_button_start_pressed = 0; // test buttons simulation
        private int State = 0; // 0 - init, 1- move along track, 2 - track completed
        private int TrackNumber = 0; // the number of track in MoveTrack collection
        private int MX2TrackNumber = 0; // the number of track in MX2 TL.TrackNumber structure


        static readonly ushort USBIO_2060 = ICPDAS_USBIO.USB2060;
        private ICPDAS_USBIO m_USBIO;
        //static readonly int COMM_TIMEOUT = 250;
        private byte[] byDOValue = new byte[1];
        private byte[] byDIValue = new byte[1];
        private bool driveIsMoving = false;
        private bool openRequestWindow = false;
        private bool continueTrackMovement = false;
        private bool trackComleted = false;

        public TrackYPage()
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
            //hide instruction boxes
            BrdReadFileInstruction.Visibility = Visibility.Hidden;
            BrdWriteFileInstruction.Visibility = Visibility.Hidden;
            parentWindow.PageTitle.Content = "Вертикальное перемещение ПОЗ по треку";
            productionmode = parentWindow.PRODUCTION_MODE;
            if (productionmode)
            {
                StPanelInfo.Visibility = Visibility.Hidden;
            }


            ReadTracksListXML();
            LvTrackList.ItemsSource = TrackList;
            LvTrackList.SelectedIndex = 0;

            cm = new CommunicationManager(851);
            //dispatcherTimer.Start();

            // USB media insertion detection route          
            ManagementEventWatcher watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 or EventType = 3");

            watcher.EventArrived += (s, e) =>
            {
                driveName = e.NewEvent.Properties["DriveName"].Value.ToString();
                eventType = Convert.ToInt16(e.NewEvent.Properties["EventType"].Value);
                Console.WriteLine("{0}: {1} {2:d}", DateTime.Now, driveName, eventType);
            };

            watcher.Query = query;
            watcher.Start();

            //check USB IO device is ready?
            if (productionmode)
            {
                if (!USB2060load())
                {
                    TxbRun.Text = "USB-IO не готов, нет связи с устройством";
                    ImgRun.Visibility = Visibility.Hidden;
                }
            }
            //read settings from XML file
            cm.ReadXMLSettings(axis1, axis2, axis_1, axis_2, tr_1, tr_2);
            //********* for test only ************
            if (!productionmode)
            {
                tr_2.PositionValue = tr_2.LowLimitInSteps;
                axis_2.Remote = true;
            }
            //************************************
            //set the scene chart wide
            SceneWidth = ( tr_2.UpLimitInSteps - tr_2.LowLimitInSteps) / tr_2.EncStepsPerOneMM;
            gap_in_steps = (int)(gap_in_mm * tr_2.EncStepsPerOneMM);
            //draw the chart
            // set initial ranges for x and y axis
            xAxis.Maximum = SceneWidth / 1000;
            xAxis.Interval = 1;
            yAxis.Maximum = 1.0;
            yAxis.Interval = 0.2;
            try
            {
                lViewModel = new ViewModel(TrackChart, TrackList[0].Points);
                DataContext = lViewModel;
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }

            //load acquired settings to TC real engine
            cm.tryConnect();
            if (!cm.LoadXMLSettings(axis_1, axis_2, tr_1, tr_2))
            {
                TxbRun.Text = cm.ERR_MSG;
                ImgRun.Visibility = Visibility.Hidden;
            }
            busy = false;
        }

        private void ReadTracksListXML()
        {
            // read tracklist* .xml file collection            
            string[] tracks;
            try
            {
                TrackList.Clear();

                tracks = Directory.GetFiles(Environment.CurrentDirectory + "/xml/trackY", "tracklist*");
                int ind = 0;

                //Console.WriteLine("The number of files starting with c is {0}.", tracks.Length);
                foreach (string track in tracks)
                {
                    XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<DataPoint>));
                    TextReader reader = new StreamReader(track);
                    RecordedTrack = ser.Deserialize(reader) as ObservableCollection<DataPoint>;
                    reader.Close();
                    ind += 1;
                    string fileindex = IndexReader(track);
                    //TrackList.Add(new Track("Track_" + String.Format("{0:d2}", ind), track, RecordedTrack));
                    TrackList.Add(new Track("Track_" + fileindex, track, RecordedTrack));
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("The process failed: {0}", err.ToString());
            }
        }
        private string IndexReader(string path)
        {
            string fileindex;
            int position = path.IndexOf("tracklist");
            fileindex = path.Substring(position + 10, 15);
            return fileindex;
        }
        //private void Timer_Tick(object sender, EventArgs e)
        private void Hpt_Tick(object sender, HighPrecisionTimer.TickEventArgs e)
        {
            if (!busy)
            {
                busy = true;
                Dispatcher.BeginInvoke((Action)(() => {
                    try
                    {
                        // time to time the twincat is not ready and cm=null
                        if (cm is null)
                        {
                            throw new ArgumentNullException(nameof(cm));
                        }
                        else { cm.tryConnect(); }

                        if (cm.IsConnected)
                        {
                            //read Axis and TR vars from real TC engine
                            cm.AxisVarReadMidFast(axis_2, "Axis_2");
                            //**** for test only *******
                            if (productionmode)
                            {
                                cm.TRVarReadFast( tr_2, "TR_2");
                                if (!cm.GSInputsCheck(axis_2.D005_MF_InputsMonitor, axis_2.Remote))
                                {
                                    TxbAlarmInfo.Text = "АВАРИЙНЫЙ СТОП ПОЗ!";
                                    StopMoving();
                                    BrdAlarmInfo.Visibility = Visibility.Visible;
                                }
                            }
                            else { axis_2.Remote = true; }
                            //*****************************

                            //draw the progress box
                            uint pmax = (uint)(ClmnChartCanvas.Width.Value);
                            uint p = 0;
                            if (productionmode)
                            {
                                p = axis_2.Remote ? (uint)(( tr_2.UpLimitInSteps - tr_2.PositionValue) * pmax / (SceneWidth * tr_2.EncStepsPerOneMM)) : 0;
                            }
                            else
                            {//******** autoincrement for test only ******************
                                if (encoder_auto_increment == 1)
                                {
                                    tr_2.PositionValue = tr_2.PositionValue > ( tr_2.UpLimitInSteps - 10) ? tr_2.UpLimitInSteps : tr_2.PositionValue + 100;
                                }
                                p = (uint)(( tr_2.UpLimitInSteps - tr_2.PositionValue) / tr_2.EncStepsPerOneMM);
                                p = (uint)(p * (pmax / SceneWidth));
                                LbStateInfo1.Content = string.Format("St: {0:d}", State);
                                LbStateInfo2.Content = string.Format("MX: {0:d}", MX2TrackNumber);
                                LbStateInfo3.Content = string.Format("TrN: {0:d}", TrackNumber);
                            }

                            ProgressBox.Width = p;
                            LbProgress.Content = String.Format("{0:f0} мм", ((int)( tr_2.PositionValue - tr_2.LowLimitInSteps) / tr_2.EncStepsPerOneMM));

                            /*if ((p * 100 / ClmnChartCanvas.Width.Value) < 30)
                            {
                                LbProgress.Visibility = Visibility.Hidden;
                            }
                            else
                            {
                                LbProgress.Visibility = Visibility.Visible;
                            }*/


                        }
                        else
                        {
                            TxbRun.Text = "Отсутствет подключение";
                            ImgRun.Visibility = Visibility.Hidden;
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("The connection to PLC failed: {0}", err.ToString());
                    }


                    if (ReadDIValue())
                    {
                        //first check if AC power is OK. If not - try to poweroff
                        int DI_3 = (int)byDIValue[0] & 8; // mask 1000  for input #3
                        //** for test only ***************
                        if (!productionmode) { DI_3 = 8; }
                        //*********************************
                        else { }
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
                            // turn on reley RL5 (feed +24V to e-Stop line) + RL0 + RL1 (connect the accumulator to UPS +12V power double line)
                            // control byte = 100011 <RL5=1:RL4=0:RL3=0:RL2=0:RL1=1:RL0=1> = 35
                            byDOValue[0] = Convert.ToByte(35);
                            _ = WriteDOValue(byDOValue);

                            //parse byDIValue               
                            int DI_1 = (int)byDIValue[0] & 2; // mask 010
                                                              //** for test only ***************
                            if (!productionmode) { DI_1 = 2; }
                            //*********************************
                            if (DI_1 != 2)
                            {
                                // power supply +24V is NG, wait for ready staff                   
                                TxbRun.Text = "Отсутствует +24В на линии E-Stop. Проверьте исправность блока питания 24В КПУ";
                                ImgRun.Visibility = Visibility.Hidden;
                                /*release RL5
                                byDOValue[0] = Convert.ToByte(0);
                                bool res = WriteDOValue(byDOValue);*/
                            }
                            else
                            {
                                /*
                                State: 
                                0 - init, 
                                1 – start move to track beginning,
                                2 – during move to track beginning
                                3 – start moving along the track
                                4 – during moving along the track
                                5 – start move to track end in case of break
                                6 – during moving to the track end after break 
                                7 – stop during track segment movement
                                8 – final stop 
                                */

                                ImgRun.Visibility = Visibility.Visible;

                                int DI_0 = (int)byDIValue[0] & 1; // mask 001 button Start is pressed
                                                                  //** for test only ***************
                                if (!productionmode) { DI_0 = test_button_start_pressed; } // denends of test start or stop button is pressed start = 1, stop = 0
                                                                                           //*********************************
                                if (DI_0 == 1)
                                {
                                    driveIsMoving = true;
                                    switch (State)
                                    {
                                        case 1:
                                            goto case 2;
                                        case 2:
                                            driveIsMoving = true;
                                            MoveToTrackBegining(RecordedTrack);
                                            TxbRun.Text = axis_2.Remote ? "Перемещение ПОЗ к началу трека" : "ПОЗ не подключен";
                                            State = 2;
                                            break;
                                        case 3:
                                            goto case 4;
                                        case 4:
                                            TxbRun.Text = axis_2.Remote ? "Движение по треку" : "ПОЗ не подключен";
                                            try
                                            {//read track number from mx2
                                                MX2TrackNumber = (UInt16)cm.client.ReadSymbol("TL.TrackNumber", typeof(UInt16), false);
                                            }
                                            catch (AdsException)
                                            {
                                                TxbRun.Text = "Ошибка чтения трека";
                                                ImgRun.Visibility = Visibility.Hidden;
                                            }
                                            if (TrackNumber == MX2TrackNumber)
                                            {
                                                // Start button is pressed, start moving along track
                                                driveIsMoving = true;
                                                StartMoving(TrackNumber);
                                                State = 4;
                                            }
                                            else
                                            {
                                                // if MX2tracknumber > tracknumber => current track is completed
                                                StopMoving();
                                                State = 7;
                                            }
                                            break;
                                        case 5:
                                            goto case 6;
                                        case 6:
                                            //  stop during moving along the track => move to track END
                                            driveIsMoving = true;
                                            MoveToTrackEnd(TrackNumber);
                                            TxbRun.Text = axis_2.Remote ? String.Format("Перемещение к точке остановки по треку {0:d}", TrackNumber) : "ПОЗ не подключен";
                                            State = 6;
                                            break;
                                        case 7:
                                            break;

                                        case 8:
                                            // moving comleted
                                            //StopMoving();
                                            break;

                                    }
                                }
                                else
                                { // start button released
                                    if (driveIsMoving)
                                    {
                                        Thread.Sleep(100); 
                                        StopMoving();
                                        driveIsMoving = false;
                                    }
                                    switch (State)
                                    {
                                        case 0:
                                            if (RecordedTrack.Count > 0) goto case 2;
                                            break;
                                        case 1:
                                            TxbRun.Text = axis_2.Remote ? "Переместить ПОЗ в начало трека" : "ПОЗ не подключен";
                                            break;
                                        case 2:
                                            int xp = (int)( tr_2.PositionValue);
                                            int ll = (int)( tr_2.LowLimitInSteps);

                                            if (Math.Abs(xp - RecordedTrack[0].Dist * tr_2.EncStepsPerOneMM * 1000 - ll) < gap_in_steps)
                                            {
                                                TxbRun.Text = axis_2.Remote ? String.Format("ПОЗ находится в начале трека {0:d}", TrackNumber) : "ПОЗ не подключен";
                                                // reset tracknumber in TL structure MX2
                                                if (cm.IsConnected)
                                                {    // reset tracknumber in TL structure in MX2
                                                    // wait 100 msec after stop instruction sent in previuse step
                                                    Thread.Sleep(100);
                                                    cm.client.WriteSymbol("TL.TrackNumber", 0, reloadInfo: false);
                                                    MX2TrackNumber = 0;
                                                }
                                                State = 3;
                                            }
                                            else
                                            {
                                                State = 1;
                                            }
                                            break;
                                        case 3:
                                            TxbRun.Text = axis_2.Remote ? String.Format("ПОЗ готов к движению по треку {0:d}", TrackNumber) : "ПОЗ не подключен";
                                            break;
                                        case 4:
                                            goto case 6;
                                        case 5:
                                            if (openRequestWindow)
                                            {
                                                // open request window to ask about track contunue
                                                BrdContunueTrackRequest.Visibility = Visibility.Visible;
                                                openRequestWindow = false;
                                            }
                                            else
                                            {
                                                if (BrdContunueTrackRequest.Visibility == Visibility.Hidden)
                                                {
                                                    State = continueTrackMovement ? State : 8;
                                                }
                                            }
                                            break;
                                        case 6:
                                            goto case 7;
                                        case 7:
                                            TxbRun.Text = axis_2.Remote ? String.Format("Остановка по треку {0:d}", TrackNumber) : "ПОЗ не подключен";
                                            ImgRun.Visibility = Visibility.Visible;
                                            // check if ARZ is near by track endpoint
                                            xp = (int)( tr_2.PositionValue);
                                            MoveAbsolutInstruction ls = GetLastSegmentInTrack(TrackNumber);
                                            int endpoint = (int)ls.TargetDist;
                                            int decelerationrunout = (int)(ls.Deceleration * V_convert_Hz_to_MS(ls.Frequency_in) * tr_2.EncStepsPerOneMM * 1000 / 2);
                                            if (Math.Abs(xp - endpoint - decelerationrunout) < gap_in_steps)
                                            {
                                                TrackNumber = MX2TrackNumber;
                                                if (TrackNumber > MoveTrack.Count - 1)
                                                { State = 8; }
                                                else
                                                { State = 3; }
                                            }
                                            else
                                            {
                                                State = 5;
                                                openRequestWindow = true;
                                                //*********** for test
                                                //MX2TrackNumber = (UInt16)cm.client.ReadSymbol("TL.TrackNumber", typeof(UInt16), false);
                                                // wait 100 msec after stop instruction sent in previuse step
                                                Thread.Sleep(100);
                                                cm.client.WriteSymbol("TL.TrackNumber", TrackNumber, reloadInfo: false);
                                            }

                                            break;

                                        case 8:
                                            if (!trackComleted)
                                            {
                                                trackComleted = true;
                                                // reset tracknumber in TL structure MX2
                                                if (!(cm is null))
                                                {
                                                    // wait 100 msec after stop instruction sent in previuse step
                                                    Thread.Sleep(100);
                                                    cm.client.WriteSymbol("TL.TrackNumber", 0, reloadInfo: false);
                                                }
                                                TrackNumber = 0;
                                                MX2TrackNumber = 0;
                                                TxbRun.Text = axis_2.Remote ? "Движение по треку завершено" : "ПОЗ не подключен";
                                                ImgRun.Visibility = Visibility.Visible;
                                            }

                                            break;
                                    }


                                }

                            }
                        }
                    }

                }));
                busy = false;
            }
        }

        private void StartMoving(int tracknumber)
        { // moving along the track from MoveTrack collection
            //************* for test only****
            if (!productionmode) { encoder_auto_increment = 1; }
            //*******************************
            if (MoveTrack.Count > 0)
            {
                // run through all MoveTrack instructions
                try
                {
                    int handle_array = cm.client.CreateVariableHandle("TL.Tracklist");
                    cm.client.WriteAny(handle_array, MoveTrack[tracknumber]);
                    cm.client.WriteSymbol("Axis_2.EnableAbsolute", true, reloadInfo: false);
                    cm.client.DeleteVariableHandle(handle_array);
                    app.logger.Info("Движение по треку");
                }
                catch (AdsException)
                {
                    TxbRun.Text = "Нет связи с ПЛК";
                    ImgRun.Visibility = Visibility.Hidden;
                }
                catch (Exception e)
                {
                    TxbRun.Text = "Ошибка подключения: " + e.Message;
                }
            }
        }
        private void StopMoving()
        {
            //************* for test only*****************
            if (!productionmode) { encoder_auto_increment = 0; }
            //********************************************
            try
            {
                //if (cm.IsConnected && axis_2.Remote)
                if (cm.IsConnected)
                {
                    cm.client.WriteSymbol("Axis_2.Rev", false, reloadInfo: false);
                    cm.client.WriteSymbol("Axis_2.Fwd", false, reloadInfo: false);
                    cm.client.WriteSymbol("Axis_2.EnableAbsolute", false, reloadInfo: false);
                    //TxbRun.Text = axis_2.Remote ? TxbRun.Text : "ПОЗ не подключен";
                    ImgRun.Visibility = Visibility.Visible;
                    app.logger.Info("Остановка движения по треку");
                }
                else
                {
                    TxbRun.Text = "Нет связи с ПЧ";
                    ImgRun.Visibility = Visibility.Hidden;
                }
            }
            catch (AdsException)
            {
                TxbRun.Text = "Нет связи с ПЛК";
                ImgRun.Visibility = Visibility.Hidden;
            }
            catch (Exception e)
            {
                TxbRun.Text = "Ошибка подключения: " + e.Message;
            }

        }
        private void MoveToTrackBegining(ObservableCollection<DataPoint> rt)
        {
            //************* for test only**** absolut move simulation
            if (!productionmode)
            {
                int xp = (int)tr_2.PositionValue;
                int dist = (int)(rt[0].Dist * tr_2.EncStepsPerOneMM * 1000 + tr_2.LowLimitInSteps);
                encoder_auto_increment = xp >= dist ? 0 : 1;
                if (xp >= dist)
                {
                    encoder_auto_increment = 0;
                    tr_2.PositionValue = (uint)dist;
                    //MX2TrackNumber = 1; // means moving to the track begining is completed
                }
                else
                {
                    encoder_auto_increment = 1;
                }
            }
            //*******************************


            MoveAbsolutInstruction[] ms = MoveSetInit();
            ms[0].Acceleration = 3.0f;
            ms[0].Deceleration = 0.2f;
            ms[0].Frequency_in = 40f;
            ms[0].FWD = false; // mx2 will check the moving direction according actual position
            ms[0].TargetDist = (uint)(rt[0].Dist * tr_2.EncStepsPerOneMM * 1000 + tr_2.LowLimitInSteps);
            try
            {
                int handle_array = cm.client.CreateVariableHandle("TL.Tracklist");
                cm.client.WriteAny(handle_array, ms);
                cm.client.WriteSymbol("Axis_2.EnableAbsolute", true, reloadInfo: false);
                ImgRun.Visibility = Visibility.Visible;
                cm.client.DeleteVariableHandle(handle_array);
                app.logger.Info("Движение к началу трека");
            }
            catch (AdsException)
            {
                TxbRun.Text = "Нет связи с ПЛК";
                ImgRun.Visibility = Visibility.Hidden;
            }

        }
        private void MoveToTrackEnd(int tracknumber)
        {
            // get last segment parameters
            MoveAbsolutInstruction ls = GetLastSegmentInTrack(tracknumber);

            //************* for test only**** absolut move simulation
            if (!productionmode)
            {
                int xp = (int) tr_2.PositionValue;
                int decelerationrunout = (int)(ls.Deceleration * V_convert_Hz_to_MS(ls.Frequency_in) * tr_2.EncStepsPerOneMM * 1000 / 2);
                int dist = (int)ls.TargetDist + decelerationrunout;
                encoder_auto_increment = xp >= dist ? 0 : 1;
                if (xp >= dist)
                {
                    encoder_auto_increment = 0;
                    tr_2.PositionValue = (uint)dist;
                    MX2TrackNumber = 1; // means moving to the track begining is completed
                }
                else
                {
                    encoder_auto_increment = 1;
                }
            }
            //*******************************


            MoveAbsolutInstruction[] ms = MoveSetInit();
            ms[0].Acceleration = 3.0f;
            ms[0].Deceleration = ls.Deceleration;
            ms[0].Frequency_in = 40f;
            ms[0].FWD = false; // mx2 will check the moving direction according actual position
            ms[0].TargetDist = ls.TargetDist;
            try
            {
                int handle_array = cm.client.CreateVariableHandle("TL.Tracklist");
                cm.client.WriteAny(handle_array, ms);
                cm.client.WriteSymbol("Axis_2.EnableAbsolute", true, reloadInfo: false);
                ImgRun.Visibility = Visibility.Visible;
                cm.client.DeleteVariableHandle(handle_array);
                app.logger.Info("Движение к началу трека");
            }
            catch (AdsException)
            {
                TxbRun.Text = "Нет связи с ПЛК";
                ImgRun.Visibility = Visibility.Hidden;
            }

        }
        private MoveAbsolutInstruction GetLastSegmentInTrack(int tracknumber)
        {
            MoveAbsolutInstruction lastsegment = new MoveAbsolutInstruction
            {
                Acceleration = 0f,
                Deceleration = 0f,
                Frequency_in = 0f,
                TargetDist = 0,
                FWD = false,
                Index = 1
            };
            foreach (MoveAbsolutInstruction mai in MoveTrack[tracknumber])
            {
                lastsegment = mai.TargetDist > 0 ? mai : lastsegment;
            }
            return lastsegment;
        }
        private void Lv_isSelected(object sender, RoutedEventArgs e)
        {
            //*********** for test only **************
            if (!productionmode) { tr_2.PositionValue = tr_2.LowLimitInSteps; }
            //****************************************
            if (((ListViewItem)sender).Content is Track track)
            {
                //load new chart                
                foreach (Track t in TrackList)
                {
                    if (t.Title == track.Title)
                    {
                        lViewModel = new ViewModel(TrackChart, t.Points);
                        DataContext = lViewModel;
                        State = 0;
                        TrackNumber = 0;
                        BtDelete.Tag = track.Title;
                        BtDeleteFileRequestOK.Tag = track.Title;
                        RecordedTrack = t.Points;
                        // trasform track points to collection of the MoveAbsolut set of instructions
                        MoveTrack = TransformTrackToMoveAbsSet(RecordedTrack);
                        if (cm.IsConnected)
                        {    // reset tracknumber in TL structure in MX2
                            cm.client.WriteSymbol("TL.TrackNumber", 0, reloadInfo: false);
                            MX2TrackNumber = 0;
                            trackComleted = false;
                        }
                        // show the track direction image
                        if (t.Points[0].Dist < t.Points[3].Dist)
                        {
                            ChartDirectionUpImg.Visibility = Visibility.Visible;
                            ChartDirectionDownImg.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            ChartDirectionUpImg.Visibility = Visibility.Hidden;
                            ChartDirectionDownImg.Visibility = Visibility.Visible;
                        }
                    }
                }

            }
        }
        private ObservableCollection<MoveAbsolutInstruction[]> TransformTrackToMoveAbsSet(ObservableCollection<DataPoint> rt)
        {
            // here we transform 4 points to 1 moveabsolut moving parameters
            // all tracks consist of n*3+1 point, where n - qty of track segments
            // if i+3 point is not 0 => do interpolation
            ObservableCollection<MoveAbsolutInstruction[]> movetrack = new ObservableCollection<MoveAbsolutInstruction[]>();

            int index = 0;
            int i = 0;

            int count = RecordedTrack.Count > 49 ? 49 : RecordedTrack.Count; // qty points on track must be less than max array dimention: 16 set by 3 points (16*3+1)

            while (i < (count - 3))
            {
                bool go_ahead = true;
                MoveAbsolutInstruction[] ms = MoveSetInit(); // create new array and fill it by zeros
                while (go_ahead)
                {

                    ms[index].Acceleration = (float)Math.Abs(2 * (rt[i].Dist - rt[i + 1].Dist) / (rt[i + 1].Velocity + rt[i].Velocity));
                    ms[index].Deceleration = (float)Math.Abs(2 * (rt[i + 3].Dist - rt[i + 2].Dist) / (rt[i + 2].Velocity + rt[i + 3].Velocity));
                    ms[index].TargetDist = (uint)(rt[i + 2].Dist * tr_2.EncStepsPerOneMM * 1000 + tr_2.LowLimitInSteps);
                    ms[index].Frequency_in = Math.Min(V_convert_MS_to_Hz(rt[i + 1].Velocity), Vmax);
                    ms[index].FWD = (rt[i].Dist - rt[i + 1].Dist) <= 0;
                    ms[index].Index = (ushort)index;

                    if (rt[i + 3].Velocity > 0.0)
                    {
                        index += 1;
                        i += 3;
                        go_ahead = true;
                    }
                    else
                    {
                        index = 0;
                        go_ahead = false;
                    }

                }
                movetrack.Add(ms);
                i += 3;
            }

            return movetrack;
        }
        private MoveAbsolutInstruction[] MoveSetInit()
        {
            MoveAbsolutInstruction[] ms = new MoveAbsolutInstruction[16];
            for (int i = 0; i < 16; i++)
            {
                MoveAbsolutInstruction mai = new MoveAbsolutInstruction
                {
                    Acceleration = 0f,
                    Deceleration = 0f,
                    Frequency_in = 0f,
                    TargetDist = 0,
                    FWD = false,
                    Index = (ushort)(i + 1)
                };
                ms[i] = mai;
            }
            return ms;
        }
        private float V_convert_MS_to_Hz(double v)
        {
            return (float)(v * 60000 / axis_2.DistancePerRevInMM * Vmax * axis_2.ReductionCx / axis_2.MotorMaxRevPerMin);
        }
        private float V_convert_Hz_to_MS(double v)
        {
            return (float)(v * axis_2.MotorMaxRevPerMin / (Vmax * axis_2.ReductionCx) * axis_2.DistancePerRevInMM / 60000);
        }


        private bool ReadDIValue()
        {
            //** for test only ***************
            if (!productionmode) { return true; }
            //*********************************

            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR == (iErrCode = m_USBIO.DI_ReadValue(byDIValue)))
            {
                return true;
            }
            else
            {
                //TxbErrorMsg.Text = "Failed to read DO value. ErrCode:[" + iErrCode.ToString() + "]";
                TxbRun.Text = "Нет связи с USB-IO";
                ImgRun.Visibility = Visibility.Hidden;
                return false;
            }
        }
        private bool WriteDOValue(byte[] _byDOValue)
        {

            //** for test only ***************
            if (!productionmode) { return true; }
            //*********************************

            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.DO_WriteValue(_byDOValue)))
            {
                //TxbErrorMsg.Text = "Failed to write DO value. ErrCode:[" + iErrCode.ToString() + "]";
                TxbRun.Text = "Нет связи с USB-IO";
                ImgRun.Visibility = Visibility.Hidden;
                return false;
            }
            else
                return true;
        }

        private bool USB2060load()
        {

            //** for test only ***************
            if (!productionmode) { return true; }
            //*********************************

            int iErrCode;

            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.OpenDevice(USBIO_2060, 1)))
            {
                //TxbErrorMsg.Text = "Failed to open USB-2060. ErrCode:[" + iErrCode.ToString() + "]";
                TxbRun.Text = "Нет связи с USB-IO";
                ImgRun.Visibility = Visibility.Hidden;
                return false;
            }
            else
            {
                return true;
            }
        }
        private void OnUnload(object sender, RoutedEventArgs e)
        {
            //dispatcherTimer.Stop();
            cm.Dispose();
            watcher.Stop();
            LvTrackList.ItemsSource = null;
            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.CloseDevice()))
            {
                Console.WriteLine("Failed to close USB-IO. ErrCode:[" + iErrCode.ToString() + "]");
            }
            hpt.Dispose();
        }
        // class of chart points
        public class DataPoint
        {
            public double Dist { get; set; }
            public double Velocity { get; set; }

        }

        // Track class describes one track points array
        public class Track
        {
            public string Title { get; set; }
            public string XMLfile { get; set; }
            public ObservableCollection<DataPoint> Points { get; private set; }
            // constructor
            public Track(string _title, string _path, ObservableCollection<DataPoint> _points)
            {
                Title = _title;
                XMLfile = _path;
                Points = _points;
            }
        }
        // class
        public class ViewModel
        {
            private readonly Chart _Chart;
            public ObservableCollection<DataPoint> Points { get; private set; }

            // constructor
            public ViewModel(Chart xChart, ObservableCollection<DataPoint> _trackpoints)
            {
                _Chart = xChart;
                Points = _trackpoints;

            }
        }




        private void BtReadFile_Click(object sender, RoutedEventArgs e)
        {
            // read file from USB root source
            //show instruction box
            if ((driveName != null) && (eventType == 2))
            {
                //usb media inserted
                TxbReadFileInstruction.Text = "Прочитать файлы tracklist-*.xml с USB диска " + driveName + "?";
            }
            else
            {
                TxbReadFileInstruction.Text = "Установите карту памяти в USB слот. Файл конфигурации трека должен находиться в корневой директории диска";
            }
            nothingtodo = false;
            BrdReadFileInstruction.Visibility = Visibility.Visible;
        }

        private void BtWriteFile_Click(object sender, RoutedEventArgs e)
        {
            // write track file to USB disk
            //show instruction box
            if ((driveName != null) && (eventType == 2))
            {
                //usb media inserted
                TxbWriteFileInstruction.Text = "Записать файлы tracklist-*.xml на диск " + driveName + "? Файлы будут записаны в каталог <trackY> USB носителя";
            }
            else
            {
                TxbWriteFileInstruction.Text = "Установите карту памяти в USB слот (если карта уже установлена, извлеките ее и подключите снова)";
            }
            nothingtodo = false;
            BrdWriteFileInstruction.Visibility = Visibility.Visible;
        }

        private void BtReadFileBack_Click(object sender, RoutedEventArgs e)
        {
            BrdReadFileInstruction.Visibility = Visibility.Hidden;
        }

        private void BtReadFileOK_Click(object sender, RoutedEventArgs e)
        {
            BrdReadFileInstruction.Visibility = Visibility.Hidden;
            // read file from USB flash here
            if ((driveName != null) && (eventType == 2) && !nothingtodo)
            {
                //observe the usb disc to find tracklist-*.xml files in the root
                string[] tracks;
                try
                {
                    TrackList.Clear();
                    string sourceDir = driveName + "/";
                    string copyDir = Environment.CurrentDirectory + "/xml/trackY/";
                    tracks = Directory.GetFiles(sourceDir, "tracklist*");

                    //Console.WriteLine("The number of files starting with c is {0}.", tracks.Length);
                    foreach (string track in tracks)
                    {
                        int attempts = 5;
                        Exception cannotReadException = null;
                        // Remove path from the file name.
                        string fName = track.Substring(sourceDir.Length);
                        while (attempts > 0)
                        {
                            try
                            {
                                //copy file to Environment.CurrentDirectory + "/xml/trackY/" directory
                                File.Copy(Path.Combine(sourceDir, fName), Path.Combine(copyDir, fName), true);
                                attempts = 0;
                            }
                            catch (Exception exception)
                            {
                                cannotReadException = exception;
                                System.Threading.Thread.Sleep(100);
                                attempts--;
                            }
                        }
                        if (cannotReadException != null)
                        {
                            TxbInfo.Text = "Ошибка копирования файлов: " + cannotReadException.Message.ToString();
                            BrdInfo.Visibility = Visibility.Visible;
                            nothingtodo = true;
                        }
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("The process failed: {0}", err.ToString());
                }
                TxbInfo.Text = "Чтение файлов треков с диска успешно завершено";
                BrdInfo.Visibility = Visibility.Visible;
                nothingtodo = true;
                ReadTracksListXML();
                LvTrackList.ItemsSource = TrackList;
                LvTrackList.SelectedIndex = TrackList.Count - 1;
            }

        }

        private void BtWriteFileOK_Click(object sender, RoutedEventArgs e)
        {
            BrdWriteFileInstruction.Visibility = Visibility.Hidden;
            // write file to USB flash here
            if ((driveName != null) && (eventType == 2) && !nothingtodo)
            {
                //observe the usb disc to find tracklist-*.xml files in the root
                string[] tracks;
                try
                {
                    string copyDir = driveName + "/trackY/";
                    string sourceDir = Environment.CurrentDirectory + "/xml/trackY/";
                    tracks = Directory.GetFiles(sourceDir, "tracklist*");
                    if (!Directory.Exists(copyDir))
                    {
                        Directory.CreateDirectory(copyDir);
                    }
                    foreach (string track in tracks)
                    {
                        int attempts = 5;
                        Exception cannotReadException = null;
                        // Remove path from the file name.
                        string fName = track.Substring(sourceDir.Length);
                        while (attempts > 0)
                        {
                            try
                            {
                                //copy file to Environment.CurrentDirectory + "/xml/trackY/" directory
                                File.Copy(Path.Combine(sourceDir, fName), Path.Combine(copyDir, fName), true);
                                attempts = 0;
                            }
                            catch (Exception exception)
                            {
                                cannotReadException = exception;
                                System.Threading.Thread.Sleep(100);
                                attempts--;
                            }
                        }
                        if (cannotReadException != null)
                        {
                            TxbInfo.Text = "Ошибка копирования файлов: " + cannotReadException.Message.ToString();
                            BrdInfo.Visibility = Visibility.Visible;
                            nothingtodo = true;
                        }
                    }

                }
                catch (Exception err)
                {
                    Console.WriteLine("The process failed: {0}", err.ToString());
                }
                TxbInfo.Text = "Запись файлов успешно завершена";
                BrdInfo.Visibility = Visibility.Visible;
                nothingtodo = true;
            }

        }

        private void BtWriteFileBack_Click(object sender, RoutedEventArgs e)
        {
            BrdWriteFileInstruction.Visibility = Visibility.Hidden;
        }

        private void BtCreate_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.MainFrame.NavigationService.Navigate(new TrackYRecordPage());
        }

        private void BtDelete_Click(object sender, RoutedEventArgs e)
        {
            Button btdel = sender as Button;
            TxbDeleteFileRequest.Text = "Удалить " + btdel.Tag.ToString() + "?";
            BrdDeleteFileRequest.Visibility = Visibility.Visible;

        }

        private void BtDeleteFileRequestOK_Click(object sender, RoutedEventArgs e)
        {
            BrdDeleteFileRequest.Visibility = Visibility.Hidden;
            //delete selected track*.xml file            
            Button btdel = sender as Button;
            int indx = TrackList.Count + 1;
            string xmlfilepath = "";
            foreach (Track t in TrackList)
            {
                if (t.Title == btdel.Tag.ToString())
                {
                    xmlfilepath = t.XMLfile;
                    break;
                }
            }

            File.Delete(xmlfilepath);
            ReadTracksListXML();
            LvTrackList.ItemsSource = TrackList;
            LvTrackList.SelectedIndex = TrackList.Count - 1;
        }

        private void BtDeleteFileRequestBack_Click(object sender, RoutedEventArgs e)
        {
            BrdDeleteFileRequest.Visibility = Visibility.Hidden;
        }

        private void BtInfoOK_Click(object sender, RoutedEventArgs e)
        {
            BrdInfo.Visibility = Visibility.Hidden;
        }

        private void BtInfoBack_Click(object sender, RoutedEventArgs e)
        {
            BrdInfo.Visibility = Visibility.Hidden;
        }

        private void BtAlarmInfoOK_Click(object sender, RoutedEventArgs e)
        {
            BrdAlarmInfo.Visibility = Visibility.Hidden;
        }

        private void teststart(object sender, RoutedEventArgs e)
        {
            test_button_start_pressed = 1;
        }

        private void teststop(object sender, RoutedEventArgs e)
        {
            test_button_start_pressed = 0;
        }

        private void BtYes_Click(object sender, RoutedEventArgs e)
        {
            BrdContunueTrackRequest.Visibility = Visibility.Hidden;
            continueTrackMovement = true;
        }

        private void BtNo_Click(object sender, RoutedEventArgs e)
        {
            BrdContunueTrackRequest.Visibility = Visibility.Hidden;
            continueTrackMovement = false;
        }
    }
}