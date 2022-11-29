using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using ICPDAS;
using TwinCAT.Ads;
using static WpfTwinCAT.MainWindow;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для TrackXRecordPage.xaml
    /// </summary>
    public partial class TrackXRecordPage : Page
    {
        //private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private HighPrecisionTimer hpt;
        private DispatcherTimer controlTimer = new DispatcherTimer(); // timer to serve the control up-down value edit
        private ObservableCollection<Track> TrackList = new ObservableCollection<Track>();
        private ObservableCollection<DataPoint> RecordTrack = new ObservableCollection<DataPoint>();
        private ViewModel lViewModel;
        private double SceneWidth = 10000; // the half of scene width in millimeters
        private float currentPoint = 12.0f; //simulation
        private float Vmax = 0.8f;
        private double Vdef = 0.6; // velocity default = 0.75Vmax (Vmax = 0.8m/sec)
        private double Treact = 1.1; // should be save in settings - heuristic value = Ta + Td minimum achievable in system
        private double durmin; // keep minimal achievable duration time for selected track
        private App app = (App)App.Current;
        private bool BtnVelocityIsPressed = false;
        private bool BtnDurIsPressed = false;
        private bool AddOperand = false;
        private double incrementalStep = 0.01;
        private bool busy = false;
        private int controltimer_timeout = 200;
        private bool productionmode = false;
        private int statemode = 0; // 0 - mothing todo, 1- open move, 2- close move
        private double freqIn = 40.0; // the velocity in Hz, need to set moving velocity during track creation
        private bool driveIsMoving = false;
        private CommunicationManager cm;
        private TC_Axis axis_1 = new TC_Axis();
        private TC_TR tr_1 = new TC_TR();
        private TC_Axis axis_2 = new TC_Axis();
        private TC_TR tr_2 = new TC_TR();
        private Axis axis1 = new Axis(); // subclass of TC_Axis+TC_TR to store settings in XML file
        private Axis axis2 = new Axis(); // subclass of TC_Axis+TC_TR to store settings in XML file

        static readonly ushort USBIO_2060 = ICPDAS_USBIO.USB2060;
        private ICPDAS_USBIO m_USBIO;
        //static readonly int COMM_TIMEOUT = 500;
        private byte[] byDOValue = new byte[1];
        private byte[] byDIValue = new byte[1];
        public TrackXRecordPage()
        {
            InitializeComponent();
            m_USBIO = new ICPDAS_USBIO();
            //  DispatcherTimer setup
            //dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            //dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, app.COMM_TIMEOUT);
            hpt = new HighPrecisionTimer(app.COMM_TIMEOUT);
            hpt.Tick += Hpt_Tick;
            controlTimer.Tick += new EventHandler(Control_Tick);
            controlTimer.Interval = new TimeSpan(0, 0, 0, 0, controltimer_timeout);
        }
       

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            busy = true;
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.NavPanelBottom.Visibility = Visibility.Visible;
            parentWindow.bBack.Visibility = Visibility.Visible;
            parentWindow.brdAdmin.Visibility = Visibility.Hidden;
            productionmode = parentWindow.PRODUCTION_MODE;
            parentWindow.PageTitle.Content = "Создание трека";
            //double currentPoint = tr_1.PositionValue / tr_1.EncStepsPerOneMM;
           
            cm = new CommunicationManager(851);
            //dispatcherTimer.Start();
            controlTimer.Start();
            if (productionmode)
            {
                //check USB IO device is ready?
                if (!USB2060load())
                {
                    TxbError.Text = "USB-IO не готов, нет связи с устройством";
                    TxbError.Visibility = Visibility.Visible;
                    StPnRun.Visibility = Visibility.Hidden;
                }
            }
            //read settings from XML file
            cm.ReadXMLSettings(axis1, axis2, axis_1, axis_2, tr_1, tr_2);
            if (!productionmode)
            {
                tr_1.PositionValue = tr_1.LowLimitInSteps - (uint)(90*tr_1.EncStepsPerOneMM);
                //tr_1.PositionValue = tr_1.LowLimitInSteps;
                //tr_1.PositionValue = tr_1.UpLimitInSteps + (uint)(90 * tr_1.EncStepsPerOneMM);
            }
            // chart init
            //set the scene chart wide
            SceneWidth = (tr_1.UpLimitInSteps - tr_1.LowLimitInSteps) / tr_1.EncStepsPerOneMM;
            //draw the chart
            // set initial ranges for x and y axis
            xAxis.Maximum = SceneWidth / 1000;
            xAxis.Interval = 1;
            yAxis.Maximum = 1.0;
            yAxis.Interval = 0.2;
            SetChartStartPoint(0.0f);
            busy = false;
        }
        //private void Timer_Tick(object sender, EventArgs e)
        private void Hpt_Tick(object sender, HighPrecisionTimer.TickEventArgs e)
        {
            if (!busy)
            {
                busy = true;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    // time to time the twincat is not ready and cm=null
                    if (cm is null)
                    {
                        throw new ArgumentNullException(nameof(cm));
                    }
                    else { cm.tryConnect(); }

                    if (cm.IsConnected)
                    {
                        //read TR vars from real TC engine
                        cm.AxisVarReadFast(axis_1, "Axis_1");
                        //******* if construction is only for test **************
                        if (productionmode) cm.TRVarReadFast(tr_1, "TR_1");
                        //**********
                        if (!cm.GSInputsCheck(axis_1.D005_MF_InputsMonitor, axis_1.Remote))
                        {
                            TxbAlarmInfo.Text = "АВАРИЙНЫЙ СТОП АРЗ!";
                            stopMoving();
                            BrdAlarmInfo.Visibility = Visibility.Visible;
                        }

                        //draw the progress box
                        uint pmax = (uint)(ClmnChartCanvas.Width.Value);
                        uint p = 0;
                        int distanceInSteps = (int)((tr_1.UpLimitInSteps - tr_1.PositionValue)) > 0 ? (int)((tr_1.UpLimitInSteps - tr_1.PositionValue)):0;
                        if (productionmode)
                        {
                            p = axis_1.Remote? (uint)(distanceInSteps * pmax / (SceneWidth * tr_1.EncStepsPerOneMM)):0;                            
                        }
                        else 
                        {
                            p = (uint)(distanceInSteps / tr_1.EncStepsPerOneMM);
                            p = (uint)( p*(pmax /SceneWidth)); 
                        }
                       
                        ProgressBox.Width = p;                        
                        LbProgress.Content = String.Format("{0:f0} мм", ((int)(tr_1.PositionValue - tr_1.LowLimitInSteps) / tr_1.EncStepsPerOneMM));
                    }
                    else
                    {
                        TxbError.Text = "Отсутствет подключение";
                        TxbError.Visibility = Visibility.Visible;
                        StPnRun.Visibility = Visibility.Hidden;
                    }
                    // check the track is ready for recording - it should consist of more then 3 points
                    BtTrackSave.Visibility = RecordTrack.Count > 3 ? Visibility.Visible : Visibility.Hidden;

                    if (productionmode)
                    {
                        if (ReadDIValue())
                        {
                            //first check if AC power is OK. If not - try to shutdown
                            int DI_3 = (int)byDIValue[0] & 8; // mask 1000  for input #3
                            if (DI_3 != 8)
                            {   //Power 220v lossed
                                // to shutdown the PC send 1 to RL2. Also need to hold closed RL0&RL1 to keep UPS power. Control byte = 000111 <RL5=0:RL4=0:RL3=0:RL2=1:RL1=1:RL0=1> = 7
                                hpt.Dispose();
                                byDOValue[0] = Convert.ToByte(7);
                                _ = WriteDOValue(byDOValue);
                                cm.Dispose();
                                //dispatcherTimer.Stop();
                            }
                            else
                            {   // turn on reley RL5 (feed +24V to e-Stop line) + RL0 + RL1 (connect the accumulator to UPS +12V power double line)
                                // control byte = 100011 <RL5=1:RL4=0:RL3=0:RL2=0:RL1=1:RL0=1> = 35
                                byDOValue[0] = Convert.ToByte(35);
                                _ = WriteDOValue(byDOValue);

                                //parse byDIValue               
                                int DI_1 = (int)byDIValue[0] & 2; // mask 010
                                if (DI_1 != 2)
                                {
                                    // power supply +24V is NG, wait for ready staff
                                    TxbError.Text = "Отсутствует +24В на линии E-Stop";

                                }
                                else
                                {
                                    int DI_0 = (int)byDIValue[0] & 1; // mask 001
                                    if (DI_0 == 1)
                                    {
                                        // Start button is pressed, start moving
                                        startMoving();
                                        driveIsMoving = true;
                                    }
                                    else
                                    {
                                        if(driveIsMoving)
                                        {
                                            stopMoving();
                                            driveIsMoving = false;
                                        }
                                    }
                                }

                            }


                        }
                        else
                        {
                            TxbError.Text = "USB-IO не готов, нет связи с устройством";
                        }
                    }
                }));
                busy = false;
            }

        }
        private void startMoving()
        {
            try { 
                switch (statemode)
                {
                    case 0:
                        // nothing todo
                        TxbError.Text = "";
                        break;

                    case 1:// run open button set
                        if (cm.IsConnected && axis_1.Remote)
                        {
                            cm.client.WriteSymbol("Axis_1.Fwd", true, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Frequency_in", freqIn, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Acceleration", axis_1.Acceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Deceleration", axis_1.Deceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.EnableFreeRun", true, reloadInfo: false);
                            TxbRun1.Text = "Открытие занавеса";
                            TxbRun2.Text = "";
                            TxbRun3.Text = "";
                            ImgLeft.Visibility = Visibility.Hidden;
                            ImgRight.Visibility = Visibility.Hidden;
                            TxbError.Text = "";
                            app.logger.Info("Открытие занавеса в режиме записи трека");
                        }
                        else
                        {
                            TxbError.Text = "Нет связи с ПЧ";
                        }
                        break;

                    case 2: // run close button set
                        if (cm.IsConnected && axis_1.Remote)
                        // if (cm.IsConnected)
                        {
                            cm.client.WriteSymbol("Axis_1.Rev", true, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Frequency_in", freqIn, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Acceleration", axis_1.Acceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Deceleration", axis_1.Deceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.EnableFreeRun", true, reloadInfo: false);
                            TxbRun1.Text = "Закрытие занавеса";
                            TxbRun2.Text = "";
                            TxbRun3.Text = "";
                            ImgLeft.Visibility = Visibility.Hidden;
                            ImgRight.Visibility = Visibility.Hidden;
                            TxbError.Text = "";
                            app.logger.Info("Закрытие занавеса в режиме записи трека");
                        }
                        else
                        {
                            TxbError.Text = "Нет связи с ПЧ";
                        }
                        break;
                   

                }
            }
            catch (AdsException)
            {
                TxbError.Text = "Потеряна связь с ПЛК";
            }
            catch (Exception e)
            {
                TxbError.Text = "Ошибка подключения: " + e.Message;
            }

        }
        private void stopMoving()
        {
            try
            {
                switch (statemode)
                {
                    case 0:
                        // nothing todo
                        TxbError.Text = "";
                        break;
                   
                    case 1: // stop close button set
                        if (cm.IsConnected && axis_1.Remote)
                        {
                            cm.client.WriteSymbol("Axis_1.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.EnableFreeRun", false, reloadInfo: false);
                            TxbError.Text = "";
                            TxbRun1.Text = "Выбран режим закрытия занавеса";
                            app.logger.Info("Остановка занавеса в режиме записи трека");
                        }
                        else
                        {
                            TxbError.Text = "Нет связи с ПЧ";
                        }
                        break;

                    case 2: // stop open button set
                        if (cm.IsConnected && axis_1.Remote)
                        {
                            cm.client.WriteSymbol("Axis_1.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.EnableFreeRun", false, reloadInfo: false);
                            TxbError.Text = "";
                            TxbRun1.Text = "Выбран режим открытия занавеса";
                            app.logger.Info("Остановка занавеса в режиме записи трека");
                        }
                        else
                        {
                            TxbError.Text = "Нет связи с ПЧ";
                        }
                        break;
                }

            }
            catch (AdsException)
            {
                TxbError.Text = "Потеряна связь с ПЛК";
            }
            catch (Exception e)
            {
                TxbError.Text = "Ошибка подключения: " + e.Message;
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
                TxbError.Text = "Failed to read DO value. ErrCode:[" + iErrCode.ToString() + "]";
                return false;
            }
        }
        private bool WriteDOValue(byte[] _byDOValue)
        {
            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.DO_WriteValue(_byDOValue)))
            {
                TxbError.Text = "Failed to write DO value. ErrCode:[" + iErrCode.ToString() + "]";
                return false;
            }
            else
                return true;
        }
        private void SetChartStartPoint(float currentpoint)
        {
            TrackList.Clear();
            TrackList.Add(new Track("Start", currentpoint, 0, 0, new ObservableCollection<DataPoint>
                                    {
                                        new DataPoint() { Dist = currentpoint, Velocity = 0 },

                                    }));
            LvTrackList.ItemsSource = TrackList;
            LvTrackList.SelectedIndex = 0;
            //draw the chart
            lViewModel = new ViewModel(TrackChart, TrackList[0].Points);
            try
            {
                DataContext = lViewModel;
            }
            catch(Exception)
            {
                Console.WriteLine("Error during DataContext = lViewModel ");
            }
            
        }
        private void Lv_isSelected(object sender, RoutedEventArgs e)
        {
            if (((ListViewItem)sender).Content is Track track)
            {
                //load new chart                
                foreach (Track t in TrackList)
                {
                    if (t.Title == track.Title)
                    {
                        lViewModel = new ViewModel(TrackChart, t.Points);
                        DataContext = lViewModel;                      

                    }
                }

            }
        }
        private bool USB2060load()
        {
            int iErrCode;

            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.OpenDevice(USBIO_2060, 1)))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        private void Lv_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (((ListViewItem)sender).Content is Track track)
            {
                if (track.Title != "Start")
                {
                    // show the edit value box
                    BrdEditBox.Visibility = Visibility.Visible;
                    LbVelocityValue.Content = track.TargetVelocity;
                    LbDurValue.Content = track.Duration;
                    durmin = track.Points[^2].Velocity * track.Duration / Vmax;
                    durmin = (durmin < Treact) ? track.Duration : durmin; // minimum duration is limited by system reactivity Treact approx 1.1 sec - heuristic value 
                    LbMinDurValue.Content = String.Format("Minimum {0:f2}", durmin); // with axis_1.Acceleration + axis_1.Deceleration minimum values
                    BtVelocityDown.Visibility = (track.TargetVelocity <= 0.0)? Visibility.Hidden : Visibility.Visible;
                    BtVelocityUp.Visibility = (track.TargetVelocity >= Vmax) ? Visibility.Hidden : Visibility.Visible;
                    BtDurDown.Visibility = (track.Duration <= durmin)? Visibility.Hidden : Visibility.Visible;
                    BtDelete.Tag = track.Title;
                    BtSave.Tag = track.Title;
                }
            }
        }
        private void OnUnload(object sender, RoutedEventArgs e)
        {
            //dispatcherTimer.Stop();
            controlTimer.Stop();
            cm.Dispose();
            //LvTrackList.ItemsSource = null;
            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.CloseDevice()))
            {
                Console.WriteLine("Failed to close USB-IO. ErrCode:[" + iErrCode.ToString() + "]");
            }
            hpt.Dispose();
        }

        private void BtMoveOpen_Click(object sender, RoutedEventArgs e)
        {
            statemode = 1;
            TxbRun1.Text = "Выбран режим открытия занавеса";
            TxbRun2.Text = "";
            TxbRun3.Text = "";
            ImgLeft.Visibility = Visibility.Hidden;
            ImgRight.Visibility = Visibility.Hidden;
            TxbError.Text = "";
            ImgMoveClose.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_90.png"));
            ImgMoveOpen.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_00.png"));
            if (!productionmode)
            {
                tr_1.PositionValue += 10000;
                tr_1.PositionValue = tr_1.PositionValue > tr_1.UpLimitInSteps ? tr_1.UpLimitInSteps - 100 : tr_1.PositionValue;
            }
        }

        private void BtMoveClose_Click(object sender, RoutedEventArgs e)
        {
            statemode = 2;            
            TxbRun1.Text = "Выбран режим закрытия занавеса";
            TxbRun2.Text = "";
            TxbRun3.Text = "";
            ImgLeft.Visibility = Visibility.Hidden;
            ImgRight.Visibility = Visibility.Hidden;
            TxbError.Text = "";           
            ImgMoveOpen.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_90.png"));
            ImgMoveClose.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_00.png"));
            if (!productionmode)
            {
                tr_1.PositionValue -= 10000;
                tr_1.PositionValue = tr_1.PositionValue < tr_1.LowLimitInSteps ? tr_1.LowLimitInSteps : tr_1.PositionValue;
            }
        }

    
        public class DataPoint
        {
            public double Dist { get; set; }
            public double Velocity { get; set; } // velocity in the middle of track = Dist/Duration where Duration= Tacceleration + T const + Tdecelartion
           

        }

        // Track class describes one track points array
        public class Track
        {
            public string Title { get; set; }
            public double TargetVelocity { get; set; } // velocity in the track end
            public double TargetDist { get; set; } // distance choosen by user << >>
            public double Duration { get; set; }  // Duration= Tacceleration + T const + Tdecelartion
            public ObservableCollection<DataPoint> Points { get; private set; }
            // constructor
            public Track(string _title, double _td, double _tv, double _dur, ObservableCollection<DataPoint> _points)
            {
                Title = _title;
                TargetDist = _td;
                TargetVelocity = _tv;
                Duration = _dur;
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

        private void BtCreatePoint_Click(object sender, RoutedEventArgs e)
        {
            //show request for start or intermediate point
            if (RecordTrack.Count>0)
            {
                BrdAddPointRequest.Visibility = Visibility.Visible; //start pointexist, let add next point
               
            }
            else
            {
                BrdStartPointRequest.Visibility = Visibility.Visible; // the list is empty let add start point
            }
            
        }

        private void BtStartPointRequestBack_Click(object sender, RoutedEventArgs e)
        {
            BrdStartPointRequest.Visibility = Visibility.Hidden;
        }

        private void BtStartPointRequestOK_Click(object sender, RoutedEventArgs e)
        {
            BrdStartPointRequest.Visibility = Visibility.Hidden;
            //check bounds and add start point to track in LV_tracklist
            int currentPointInSteps = (int)(tr_1.PositionValue - tr_1.LowLimitInSteps);
            currentPointInSteps = currentPointInSteps > 0 ? currentPointInSteps : 0;            
            bool overHiLimitDetected = (int)(tr_1.UpLimitInSteps - tr_1.PositionValue) < 0;
            if (overHiLimitDetected) 
            { 
                currentPointInSteps = (int)(tr_1.UpLimitInSteps - tr_1.LowLimitInSteps); 
            }
            currentPoint = (float)(currentPointInSteps / tr_1.EncStepsPerOneMM / 1000);            
            SetChartStartPoint(currentPoint);               
            RecordTrack.Add(new DataPoint() { Dist = currentPoint, Velocity = 0 });    
        }

        private void BtAddPointRequestBack_Click(object sender, RoutedEventArgs e)
        {
            BrdAddPointRequest.Visibility = Visibility.Hidden;
        }

        private void BtAddPointRequestOK_Click(object sender, RoutedEventArgs e)
        {
            BrdAddPointRequest.Visibility = Visibility.Hidden;
            // add intermediate point in LV_tracklist
           
            // request last recorded point
            double prevpoint = RecordTrack[^1].Dist;  // index operator ^ - pint the index from the end of collection

            /*var randDist = new Random();// simulation
            //double midpoint = 24.5; // simulation
            double midpoint = randDist.NextDouble() * prevpoint;// simulation
            midpoint = Math.Round(midpoint, 2, MidpointRounding.AwayFromZero); */

            currentPoint = (float)((tr_1.PositionValue - tr_1.LowLimitInSteps) / tr_1.EncStepsPerOneMM / 1000);
           

            double[] trackParams = TrackParamsCalc(prevpoint, currentPoint, 0.0);
           
            // add new points to the track
            RecordTrack.Add(new DataPoint() { Dist = trackParams[1], Velocity = trackParams[0] });
            RecordTrack.Add(new DataPoint() { Dist = trackParams[2], Velocity = trackParams[0] });
            RecordTrack.Add(new DataPoint() { Dist = currentPoint, Velocity = 0});
            //draw the part of track             
            TrackList.Add(new Track(TrackList.Count.ToString(), RecordTrack[^1].Dist, RecordTrack[^1].Velocity, trackParams[3], RecordTrack));
            LvTrackList.ItemsSource = TrackList;
            LvTrackList.SelectedIndex = LvTrackList.Items.Count-1;           
        }
        private double[] TrackParamsCalc(double prevpoint, double midpoint, double T=0.0)
        {
            // S = Sa + Sc + Sd (Sa path during acceleration, Sc - path during constant velocity, Sd - path during  deceleration)
            double S = (prevpoint - midpoint) > 0 ? (prevpoint - midpoint) : (midpoint - prevpoint); 
            T = (T==0.0) ? (S/Vdef) : T; // if T was not indicated  => use default Vdef to determine T
            double Ta = 2.0; // acceleration time axis_1.Acceleration
            double Td = 0.5; // deceleration time axis_1.Deceleration
            double Ta_min = 0.5; // minimum acceleration time heuristic value =>> Ta_min + Td_min = Treact
            double Td_min = 0.2; // minimum deceleration time heuristic value    
                                 // = axis_1.MotorMaxRevPerMin * axis_1.DistancePerRevInMM/axis_1.ReductionCx/60000
            double Vc;
            if ((Ta + Td) > T) //Vmax = 0.8 in this case we need to reduce Ta and Td to stay within Vmax
            {
                Ta = (T * Ta / (Ta + Td)) < Ta_min ? Ta_min : (T * Ta / (Ta + Td));
                Td = (T * Td / (Ta + Td)) < Td_min ? Td_min : (T * Td / (Ta + Td));
                Vc = S / (Ta + Td);
            }
            else
            {
                Vc = 2.0 * S / ((2.0 * T) - Ta - Td);
                if (Vc > Vmax)
                {
                    Ta = Ta * Vmax / Vc;
                    Td = Td * Vmax / Vc;
                    Vc = Vmax;
                }
            }
            double Sa = prevpoint > midpoint ? (prevpoint - Vc * Ta / 2) : (prevpoint + Vc * Ta / 2);
            double Sd = prevpoint > midpoint ? (midpoint + Vc * Td / 2) : (midpoint - Vc * Td / 2);
            Vc = Math.Round(Vc, 2, MidpointRounding.AwayFromZero);
            T = Math.Round(T, 2, MidpointRounding.AwayFromZero);
            double[] res = new double[] { Vc, Sa, Sd, T };
            return res;
        }

      

        private void BtEditBoxBack_Click(object sender, RoutedEventArgs e)
        {
            BrdEditBox.Visibility = Visibility.Hidden;
        }

        #region Velocity and Duration edit box
        private void BtVelocityUp_Click(object sender, RoutedEventArgs e)
        {
            
            double vel = double.Parse(LbVelocityValue.Content.ToString());
            vel += incrementalStep;
            //BtnVelocityIsPressed = true;
            BtnDurIsPressed = false;
            AddOperand = true;
            LbVelocityValue.Content = String.Format("{0:f2}", vel);
            if (vel > 0.0) BtVelocityDown.Visibility = Visibility.Visible;
            if (vel >= Vmax)
            {
                BtVelocityUp.Visibility = Visibility.Hidden;
                LbVelocityValue.Content = String.Format("{0:f2}", Vmax);
                BtnVelocityIsPressed = false;
            } 
        }
        private void BtVelocityUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BtnVelocityIsPressed = true;
            BtnDurIsPressed = false;
            AddOperand = true;
        }
        private void BtVelocityUp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnVelocityIsPressed = false;
            BtnDurIsPressed = false;
        }
        private void BtVelocityDown_Click(object sender, RoutedEventArgs e)
        {
            double vel = double.Parse(LbVelocityValue.Content.ToString());
            vel -= incrementalStep;
            //BtnVelocityIsPressed = true;
            BtnDurIsPressed = false;
            AddOperand = false;
            LbVelocityValue.Content = String.Format("{0:f2}", vel);
            if (vel <= 0.0)
            {
                BtVelocityDown.Visibility = Visibility.Hidden;
                LbVelocityValue.Content = "0";
                BtnVelocityIsPressed = false;
            }
        }

        private void BtVelocityDown_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnVelocityIsPressed = false;
            BtnDurIsPressed = false;

        }
        private void BtVelocityDown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BtnVelocityIsPressed = true;
            BtnDurIsPressed = false;
            AddOperand = false;
        }
        private void Control_Tick(object sender, EventArgs e)
        {
            if (BtnVelocityIsPressed)
            {
                double vel = double.Parse(LbVelocityValue.Content.ToString());
                vel = AddOperand? (vel + incrementalStep) : (vel - incrementalStep);
                LbVelocityValue.Content = String.Format("{0:f2}", vel);
                if (vel > 0.0) BtVelocityDown.Visibility = Visibility.Visible;
                if (vel >= Vmax)
                {
                    BtVelocityUp.Visibility = Visibility.Hidden;
                    LbVelocityValue.Content = String.Format("{0:f2}", Vmax);
                }
                if (vel <= 0.0)
                {
                    BtVelocityDown.Visibility = Visibility.Hidden;
                    LbVelocityValue.Content = "0";
                   
                }

            }
            if (BtnDurIsPressed)
            {
                double dur = double.Parse(LbDurValue.Content.ToString());
                dur = AddOperand ? (dur + incrementalStep*50) : (dur - incrementalStep*50);
                LbDurValue.Content = String.Format("{0:f2}", dur);
                if (dur > durmin)
                {
                    BtDurDown.Visibility = Visibility.Visible;
                }
                if (dur <= durmin)
                {
                    BtDurDown.Visibility = Visibility.Hidden;
                    LbDurValue.Content = String.Format("{0:f2}", durmin);
                }                    
            }
            
        }

        

        private void BtDurUp_Click(object sender, RoutedEventArgs e)
        {
                      
            double dur = double.Parse(LbDurValue.Content.ToString());
            dur += incrementalStep;
            AddOperand = true;
            LbDurValue.Content = String.Format("{0:f2}", dur);
            if (dur > durmin)
            {
                BtDurDown.Visibility = Visibility.Visible;
                //BtnDurIsPressed = false;
            }
            BtnVelocityIsPressed = false;
            //BtnDurIsPressed = true;
        }

        private void BtDurDown_Click(object sender, RoutedEventArgs e)
        {
           
            double dur = double.Parse(LbDurValue.Content.ToString());
            dur -= incrementalStep;
            AddOperand = false;
            LbDurValue.Content = String.Format("{0:f2}", dur);
            if (dur <= durmin)
            {
                BtDurDown.Visibility = Visibility.Hidden;
                LbDurValue.Content= String.Format("{0:f2}", durmin);
                BtnDurIsPressed = false;
            }
            BtnVelocityIsPressed = false;
            //BtnDurIsPressed = true;
        }
        private void BtDurUp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnVelocityIsPressed = false;
            BtnDurIsPressed = false;
        }

        private void BtDurUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BtnVelocityIsPressed = false;
            BtnDurIsPressed = true;
            AddOperand = true;
        }

        private void BtDurDown_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnDurIsPressed = false;
            BtnVelocityIsPressed = false;
        }

        private void BtDurDown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BtnVelocityIsPressed = false;
            BtnDurIsPressed = true;
            AddOperand = false;
        }
        #endregion

        private void BtSave_Click(object sender, RoutedEventArgs e)
        {
            BrdEditBox.Visibility = Visibility.Hidden;
            //save the track 
            Button btsave = sender as Button;
            int tracklistCount = TrackList.Count - 1;
            int indx = 0;
            foreach (Track t in TrackList)
            {
                if (t.Title == btsave.Tag.ToString())
                {
                    indx = int.Parse(t.Title);                    
                    t.TargetVelocity= double.Parse(LbVelocityValue.Content.ToString());
                    RecordTrack[indx * 3].Velocity = t.TargetVelocity;
                    t.Duration = double.Parse(LbDurValue.Content.ToString());
                    double prevpoint = RecordTrack[indx * 3-3].Dist;                    
                    double midpoint = RecordTrack[indx * 3].Dist;
                    double[] trackParams = TrackParamsCalc(prevpoint, midpoint, t.Duration);
                    //update track params
                    RecordTrack[indx * 3 - 2].Dist = trackParams[1];
                    RecordTrack[indx * 3 - 2].Velocity = trackParams[0];
                    RecordTrack[indx * 3 - 1].Dist = trackParams[2];
                    RecordTrack[indx * 3 - 1].Velocity = trackParams[0];
                    break;
                }
            }
            //reset listview and chart datacontext
            LvTrackList.ItemsSource = null;
            DataContext = null;
            LvTrackList.ItemsSource = TrackList;
            lViewModel = new ViewModel(TrackChart, TrackList[indx].Points);
            DataContext = lViewModel;
            LvTrackList.SelectedIndex = indx;
        }

        private void BtDelete_Click(object sender, RoutedEventArgs e)
        {
            // delete the selected part of the track and all heirs
            BrdEditBox.Visibility = Visibility.Hidden;
            Button btdel = sender as Button;
            int tracklistCount = TrackList.Count - 1;
            int indx = TrackList.Count +1;
            foreach (Track t in TrackList)
            {
                if (t.Title == btdel.Tag.ToString())
                {
                    indx = int.Parse(t.Title);
                    break;  
                }
            }
            for (int i = tracklistCount; i > indx - 1; i--) 
            { 
                TrackList.RemoveAt(i);
                RecordTrack.RemoveAt(i * 3);
                RecordTrack.RemoveAt(i * 3 - 1);
                RecordTrack.RemoveAt(i * 3 - 2);
            }
            LvTrackList.ItemsSource = TrackList;
            LvTrackList.SelectedIndex = indx-1;
        }

        private void BtTrackSave_Click(object sender, RoutedEventArgs e)
        {
            // save completed track
            TxtFileName.Text = (DateTime.Now).ToString("yyMMddHHmmssfff");
            BrdSaveTrackRequest.Visibility = Visibility.Visible;
            
        }
        private void SaveTrack(string trackfilename)
        {
            string fileindex = (DateTime.Now).ToString("yyMMddHHmmssfff");
            string spaceholder = "_______________";
            //save new track in separate file
            if (trackfilename.Length > 0)
            {
                trackfilename = trackfilename.Replace(" ","_") + spaceholder;
                fileindex = trackfilename.Substring(0, 15);
            }            
            string filename = Environment.CurrentDirectory + "/xml/trackX/tracklist-" + fileindex + ".xml";
            bool append = false;
            XmlSerializer ser = new XmlSerializer(typeof(ObservableCollection<DataPoint>));
            TextWriter writer = new StreamWriter(filename, append, System.Text.Encoding.UTF8); //false here means 'to rewrite file' if true - append new staff to the end of file
            ser.Serialize(writer, RecordTrack);
            writer.Close();
            // navigate to trackpage
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;            
            parentWindow.MainFrame.NavigationService.Navigate(new TrackXPage());
        }
       

        private void BtSaveTrackBack_Click(object sender, RoutedEventArgs e)
        {
            BrdSaveTrackRequest.Visibility = Visibility.Hidden;
        }

        private void BtSaveTrackOK_Click(object sender, RoutedEventArgs e)
        {
            string newtrackfilename = TxtFileName.Text;
            BrdSaveTrackRequest.Visibility = Visibility.Hidden;
            SaveTrack(newtrackfilename);
        }

        private void TxtFileNamePreview(object sender, TextCompositionEventArgs e)
        {
            if (!((Char.IsLetterOrDigit(e.Text, 0) || e.Text.Equals("_")) && (TxtFileName.Text.Length<15)) )         
            {
                e.Handled = true;
            }
        }

        private void BtAlarmInfoOK_Click(object sender, RoutedEventArgs e)
        {
            BrdAlarmInfo.Visibility = Visibility.Hidden;
        }
    }
}
