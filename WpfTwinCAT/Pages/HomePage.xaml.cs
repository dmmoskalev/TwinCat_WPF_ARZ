using System;
using System.Collections;
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
using TwinCAT.Ads;
using static WpfTwinCAT.MainWindow;
using ICPDAS;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        private CommunicationManager cm;
        private TC_Axis axis_1 = new TC_Axis();
        private TC_TR tr_1 = new TC_TR();
        private TC_Axis axis_2 = new TC_Axis();
        private TC_TR tr_2 = new TC_TR();
        private Axis axis1 = new Axis(); // subclass of TC_Axis+TC_TR to store settings in XML file
        private Axis axis2 = new Axis(); // subclass of TC_Axis+TC_TR to store settings in XML file
        private double freqIn;
        private int statemode = 0;
        private int ARZ_ws_state = 0; // ARZ work stop achieved status
        private int UPDN_ws_state = 0; // UPDN work stop achieved status
        private bool productionmode = false;
        private bool busy = false;
        private bool driveIsMoving = false;
        private int drop = 0;
        private int alarmInfoTimerCounter = 0; // to automatically hide info window after several seconds interval
        //private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private HighPrecisionTimer hpt;

        private Stopwatch stopwatch = new Stopwatch();
        private Stopwatch sw = new Stopwatch();
       
        private int openBtCount = 0;
        private int closeBtCount = 0;
        private int upBtCount = 0;
        private int downBtCount = 0;
        private long max_read_latency = 0;
        private long min_read_latency = 1000;
        static readonly ushort USBIO_2060 = ICPDAS_USBIO.USB2060;
        private ICPDAS_USBIO m_USBIO;
        //static readonly int COMM_TIMEOUT = 100;
        private byte[] byDOValue = new byte[1];
        private byte[] byDIValue = new byte[1];

        private App app = (App)App.Current;
        /* 
        mode index description: 
            0 = unselected mode, 

            13 = open 30% velocity, 
            16= open 60% velocity, 
            19= open 90% velocity,

            23 = close on 30% velocity, 
            26= close on 60% velocity, 
            29= close on 90% velocity,

            33 = up on 30% velocity, 
            36= up on 60% velocity, 
            39= up on 90% velocity, 

            43 = down on 30% velocity, 
            46= down on 60% velocity, 
            49= down on 9ресур0% velocity
        */
        public HomePage()
        {
            InitializeComponent();
            m_USBIO = new ICPDAS_USBIO();
            //  DispatcherTimer setup
           // dispatcherTimer.Tick += new EventHandler(Timer_Tick);
           // dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, COMM_TIMEOUT);
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
            parentWindow.PageTitle.Content = "Системы управления INTOKU DYNAMICS";
            productionmode = parentWindow.PRODUCTION_MODE;
            //check USB IO device is ready?
            cm = new CommunicationManager(851);
           // dispatcherTimer.Start();
            if (productionmode)
            {
                SP_Info_Connect.Visibility = Visibility.Hidden;
                if (!USB2060load())
                {
                    LbSplash.Content = "USB-IO не готов, нет связи с устройством";
                    LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                    HideControls();
                }
            }

            TxbErrorMsg.Text = "";
            //read settings from XML file
            cm.ReadXMLSettings(axis1, axis2, axis_1, axis_2, tr_1, tr_2);
            //load acquired settings to TC real engine
            cm.tryConnect();
            if (!cm.LoadXMLSettings(axis_1, axis_2, tr_1, tr_2))
            {
                LbSplash.Content = cm.ERR_MSG;
                LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                HideControls();
            }
            busy = false;

            // test
            //BrdInfo.Visibility = Visibility.Visible;
            //alarmInfoTimerCounter = 0;
        }
       
        // private void Timer_Tick(object sender, EventArgs e)
        private void Hpt_Tick(object sender, HighPrecisionTimer.TickEventArgs e)
        {
            if (!busy)
            {
                busy = true;
                drop = 0;
                //check TWINCAT is ready?
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (!sw.IsRunning) sw.Start();
                    stopwatch.Start();
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
                            LbSplash.Content = axis_1.Remote ? "АРЗ готов к работе. " : "АРЗ не подключен. ";
                            LbSplash.Content += axis_2.Remote ? "ПОЗ готов к работе" : "ПОЗ не подключен";
                            LbSplash.Foreground = new SolidColorBrush(Colors.White);
                            ShowControls();

                        }
                        else
                        {
                            LbSplash.Content = "Пульт не готов, нет связи с ПЛК";
                            LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                            HideControls();
                        }

                        stopwatch.Stop();
                        //LbTC_ConTimeValue.Content = stopwatch.ElapsedMilliseconds; 
                        stopwatch.Reset();
                        //read GVLs from TC
                        stopwatch.Start();
                        if (cm.IsConnected)
                        {
                            //read Axis and TR vars from real TC engine
                           
                            cm.AxisVarReadFast(axis_1, "Axis_1");
                            cm.TRVarReadFast(tr_1, "TR_1");
                            cm.AxisVarReadFast(axis_2, "Axis_2");
                            cm.TRVarReadFast(tr_2, "TR_2");
                            //TxbErrorMsg.Text = cm.ERR_MSG;

                            if (cm.ERR_MSG != null)
                            {
                                LbSplash.Content = cm.ERR_MSG;
                                LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                                HideControls();
                            }
                            if (!cm.GSInputsCheck(axis_1.D005_MF_InputsMonitor, axis_1.Remote))
                            {
                                TxbAlarmInfo.Text = "АВАРИЙНЫЙ СТОП АРЗ! ПРОВЕРЬТЕ НАЛИЧИЕ СИГНАЛА E-STOP И ПОЛОЖЕНИЕ ПРИВОДА АРЗ";
                                stopMoving();
                                BrdInfo.Visibility = Visibility.Visible;
                                alarmInfoTimerCounter = 0;
                            }
                            if (!cm.GSInputsCheck(axis_2.D005_MF_InputsMonitor, axis_2.Remote))
                            {
                                TxbAlarmInfo.Text = "АВАРИЙНЫЙ СТОП ПОЗ! ПРОВЕРЬТЕ НАЛИЧИЕ СИГНАЛА E-STOP И ПОЛОЖЕНИЕ ПРИВОДА ПОЗ";
                                stopMoving();
                                BrdInfo.Visibility = Visibility.Visible;
                                alarmInfoTimerCounter = 0;
                            }
                           
                            // info alarm info window shoud be hiden after 3 sec
                            alarmInfoTimerCounter += 1;
                            if (alarmInfoTimerCounter > 30)
                            {
                                BrdInfo.Visibility = Visibility.Hidden;
                                alarmInfoTimerCounter = 0;
                            }

                            // check for working stop state
                            if (!cm.WSInputCheck(axis_1.D005_MF_InputsMonitor, axis_1.Remote))
                            {
                                if (ARZ_ws_state == 0)
                                {

                                    // inform the user about working stop event
                                    switch (statemode)
                                    {
                                        case 0:
                                            int ll = (int)(tr_1.LowLimitInSteps);
                                            int ul = (int)(tr_1.UpLimitInSteps);
                                            int xp = (int)(tr_1.PositionValue);
                                            if ((Math.Abs(ll - xp) - Math.Abs(ul - xp)) < 0)
                                            {
                                                ARZ_ws_state = 2;
                                                TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме закрытия АРЗ";
                                                BrdInfo.Visibility = Visibility.Visible;
                                                ImCloseLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_stop.png"));
                                                ImCloseRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_stop.png"));
                                            }
                                            else
                                            {
                                                TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме открытия АРЗ";
                                                BrdInfo.Visibility = Visibility.Visible;
                                                ARZ_ws_state = 1;
                                                ImOpenLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_stop.png"));
                                                ImOpenRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_stop.png"));
                                            }
                                            break;
                                        case 1:
                                            stopMoving();
                                            TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме открытия АРЗ";
                                            BrdInfo.Visibility = Visibility.Visible;
                                            ARZ_ws_state = statemode;
                                            // reset control buttons 
                                            PageUpdate(0);
                                            ImOpenLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_stop.png"));
                                            ImOpenRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_stop.png"));
                                            //reset state 
                                            statemode = 0;
                                            break;
                                        case 2:
                                            stopMoving();
                                            TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме закрытия АРЗ";
                                            BrdInfo.Visibility = Visibility.Visible;
                                            ARZ_ws_state = statemode;
                                            // reset control buttons 
                                            PageUpdate(0);
                                            ImCloseLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_stop.png"));
                                            ImCloseRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_stop.png"));
                                            //reset state 
                                            statemode = 0;
                                            break;
                                    }

                                }
                            }
                            else
                            { // resume working stop state
                                ARZ_ws_state = 0;

                            }
                            if (!cm.WSInputCheck(axis_2.D005_MF_InputsMonitor, axis_2.Remote))
                            {
                                if (UPDN_ws_state == 0)
                                {

                                    // inform the user about working stop event
                                    switch (statemode)
                                    {

                                        case 0:

                                            int ll = (int)(tr_2.LowLimitInSteps);
                                            int ul = (int)(tr_2.UpLimitInSteps);
                                            int xp = (int)(tr_2.PositionValue);
                                            if ((Math.Abs(ll - xp) - Math.Abs(ul - xp)) < 0)
                                            {
                                                UPDN_ws_state = 4;
                                                TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме спуска ПОЗ";
                                                BrdInfo.Visibility = Visibility.Visible;
                                                ImDownLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_stop.png"));
                                                ImDownRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_stop.png"));
                                            }
                                            else
                                            {
                                                TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме подъема ПОЗ";
                                                BrdInfo.Visibility = Visibility.Visible;
                                                UPDN_ws_state = 3;
                                                ImUpLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_stop.png"));
                                                ImUpRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_stop.png"));
                                            }
                                            break;
                                        case 3:
                                            stopMoving();
                                            TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме подъема ПОЗ";
                                            BrdInfo.Visibility = Visibility.Visible;
                                            UPDN_ws_state = statemode;
                                            // reset control buttons 
                                            PageUpdate(0);
                                            ImUpLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_stop.png"));
                                            ImUpRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_stop.png"));
                                            //reset state 
                                            statemode = 0;
                                            break;
                                        case 4:
                                            stopMoving();
                                            TxbAlarmInfo.Text = "Достигнут рабочий концевик в режиме спуска ПОЗ";
                                            BrdInfo.Visibility = Visibility.Visible;
                                            UPDN_ws_state = statemode;
                                            // reset control buttons 
                                            PageUpdate(0);                                            
                                            ImDownLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_stop.png"));
                                            ImDownRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_stop.png"));
                                            //reset state 
                                            statemode = 0;
                                            break;
                                    }

                                }
                            }
                            else
                            { // resume working stop state
                                UPDN_ws_state = 0;
                            }
                            

                            // need to check the consistance of ZeroLevel value to prevent error data and arise notification about calibration setup
                            if (axis_1.Remote & ARZ_ws_state == 0)
                            {
                                if (tr_1.LowLimitInSteps < tr_1.PositionValue & tr_1.PositionValue < tr_1.UpLimitInSteps)
                                {
                                    LbOpenClosePositionValue.Content = string.Format("{0:f1}", axis_1.Remote ? ((tr_1.PositionValue - tr_1.LowLimitInSteps) / tr_1.EncStepsPerOneMM) : 0);
                                }
                                else
                                {
                                    /*TxbAlarmInfo.Text = "Требуется калибровка энкодера АРЗ!";
                                    stopMoving();
                                    BrdInfo.Visibility = Visibility.Visible;*/
                                }
                            }
                            if (axis_2.Remote & UPDN_ws_state == 0)
                            {
                                if (tr_2.LowLimitInSteps < tr_2.PositionValue & tr_2.PositionValue < tr_2.UpLimitInSteps)
                                {
                                    LbUpDownPositionValue.Content = string.Format("{0:f1}", axis_2.Remote ? ((tr_2.PositionValue - tr_2.LowLimitInSteps) / tr_2.EncStepsPerOneMM) : 0);
                                }
                                else
                                {
                                    /*TxbAlarmInfo.Text = "Требуется калибровка энкодера ПОЗ!";
                                    stopMoving();
                                    BrdInfo.Visibility = Visibility.Visible;*/
                                }
                            }
                        }
                    }
                    catch (ArgumentNullException err)
                    {
                        Console.WriteLine("The connection to PLC failed: {0}", err.ToString());
                    }
                    //tttttttttttttttttttttttttttt
                    //startMoving();
                    //stopMoving();
                    //ttttttttttttttttttttttttttt
                    stopwatch.Stop();
                    max_read_latency = (max_read_latency < stopwatch.ElapsedMilliseconds) ? stopwatch.ElapsedMilliseconds : max_read_latency;
                    min_read_latency = (min_read_latency > stopwatch.ElapsedMilliseconds) ? stopwatch.ElapsedMilliseconds : min_read_latency;
                    LbTCMAXvalue.Content = string.Format("{0:d}", max_read_latency);
                    LbTCMINvalue.Content = string.Format("{0:d}", min_read_latency);
                    LbTCCUR.Foreground = new SolidColorBrush(Colors.Green);
                    LbTCCUR.Content = string.Format("DR:{0:d}", drop);
                    LbTCCURvalue.Content = string.Format("{0:d}", stopwatch.ElapsedMilliseconds);
                    stopwatch.Reset();
                    // check USBIO DI inputs status
                    stopwatch.Start();
                    if (productionmode)
                    {
                        if (ReadDIValue())
                        {
                            //first check if AC power is OK. If not - try to poweroff
                            int DI_3 = (int)byDIValue[0] & 8; // mask 1000  for input #3
                            if (DI_3 != 8)
                            {
                                // try to shutdown
                                //cm.Shutdown();
                                // to shutdown the PC send 1 to RL2. Also need to hold closed RL0&RL1 to keep UPS power. Control byte = 000111 = 7
                                hpt.Dispose();
                                byDOValue[0] = Convert.ToByte(7);
                                _ = WriteDOValue(byDOValue);
                                cm.Dispose();
                                //dispatcherTimer.Stop();
                                sw.Stop();
                            }
                            else
                            {
                                // to turn on reley RL5 (feed +24V to e-Stop line) + RL0 + RL1 (connect the accumulator to UPS +12V power double line)
                                // need to send the byte: 100011 {RL5=1:RL4=0:RL3=0:RL2=0:RL1=1:RL0=1) = 35
                                byDOValue[0] = Convert.ToByte(35);
                                _ = WriteDOValue(byDOValue);

                                //parse byDIValue               
                                int DI_1 = (int)byDIValue[0] & 2; // mask 010
                                if (DI_1 != 2)
                                {
                                    // power supply +24V is NG, wait for ready staff

                                    LbSplash.Content = "Отсутствует +24В на линии E-Stop";
                                    LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                                    HideControls();

                                }
                                else
                                {
                                    int DI_0 = (int)byDIValue[0] & 1; // mask 001
                                    if (DI_0 == 1)
                                    {
                                        // Start button is pressed, start moving
                                        startMoving();
                                        driveIsMoving = true;
                                        TimeSpan ts = sw.Elapsed;
                                        string currentTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                                        if ((statemode == 1 | statemode == 2) & axis_1.Remote) LbOpenCloseDurationValue.Content = currentTime;
                                        if ((statemode == 3 | statemode == 4) & axis_2.Remote) LbUpDownDurationValue.Content = currentTime;

                                    }
                                    else
                                    {
                                        if (driveIsMoving)
                                        {
                                            stopMoving();
                                            driveIsMoving = false;
                                        }
                                        sw.Reset();
                                    }
                                }
                            }


                        }
                        else
                        {

                            LbSplash.Content = "USB-IO не готов, нет связи с устройством";
                            LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                            HideControls();

                        }
                    }

                    stopwatch.Stop();
                    //LbUSBIO_ConTimeValue.Content = stopwatch.ElapsedMilliseconds;
                    stopwatch.Reset();
                    busy = false;
                }));
            }
            else
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    drop += 1;
                    LbTCCUR.Foreground = new SolidColorBrush(Colors.Red);
                    LbTCCUR.Content = string.Format("DR:{0:d}", drop); 
                }));
                
            }
        }

        private bool USB2060load()
        {
            int iErrCode;
           
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.OpenDevice(USBIO_2060, 1)))
            {
                //TxbErrorMsg.Text = "Failed to open USB-2060. ErrCode:[" + iErrCode.ToString() + "]";
                LbSplash.Content = "USB-IO не готов, нет связи с устройством";
                LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                HideControls();
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
                //TxbErrorMsg.Text = "Failed to read DO value. ErrCode:[" + iErrCode.ToString() + "]";
                LbSplash.Content = "USB-IO не готов, нет связи с устройством";
                LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                HideControls();
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
                //TxbErrorMsg.Text = "Failed to read DO value. ErrCode:[" + iErrCode.ToString() + "]";
                LbSplash.Content = "USB-IO не готов, нет связи с устройством";
                LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                HideControls();
                return false;
            }
        }
        private bool WriteDOValue(byte[] _byDOValue)
        {
            int iErrCode;
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.DO_WriteValue(_byDOValue)))
            {
                //TxbErrorMsg.Text = "Failed to write DO value. ErrCode:[" + iErrCode.ToString() + "]";
                LbSplash.Content = "USB-IO не готов, нет связи с устройством";
                LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                HideControls();
                return false;
            }
            else
                return true;
        }

        private void BtOpen_Click(object sender, RoutedEventArgs e)
        {
            //mode 1
            if (ARZ_ws_state != 1)
            {
                openBtCount += 1;
                double velocity = (axis_1.MotorMaxRevPerMin / axis_1.ReductionCx) * axis_1.DistancePerRevInMM / 60000;
                switch (openBtCount)
                {
                    case 0:
                        PageUpdate(0);
                        LbOpenCloseVelocityValue.Content = "0.00";
                        freqIn = 0.0;
                        statemode = 0;
                        app.logger.Info("Выбран режим открытия на скорости 0%");
                        break;
                    case 1:
                        PageUpdate(0);
                        PageUpdate(13);
                        LbOpenCloseVelocityValue.Content = (velocity * 0.3).ToString("#0.00");
                        freqIn = 50 * 0.3;
                        statemode = 1;
                        app.logger.Info("Выбран режим открытия на скорости 30%");
                        break;
                    case 2:
                        PageUpdate(0);
                        PageUpdate(16);
                        LbOpenCloseVelocityValue.Content = (velocity * 0.6).ToString("#0.00");
                        freqIn = 50 * 0.6;
                        statemode = 1;
                        app.logger.Info("Выбран режим открытия на скорости 60%");
                        break;
                    case 3:
                        PageUpdate(0);
                        PageUpdate(19);
                        openBtCount = -1;
                        LbOpenCloseVelocityValue.Content = velocity.ToString("#0.00");
                        freqIn = 50;
                        statemode = 1;
                        app.logger.Info("Выбран режим открытия на скорости 100%");
                        break;
                }
            }
        }

        private void BtClose_Click(object sender, RoutedEventArgs e)
        {
            //mode 2
            if (ARZ_ws_state != 2)
            {
                closeBtCount += 1;
                double velocity = (axis_1.MotorMaxRevPerMin / axis_1.ReductionCx) * axis_1.DistancePerRevInMM / 60000;
                switch (closeBtCount)
                {
                    case 0:
                        PageUpdate(0);
                        statemode = 0;
                        freqIn = 0.0;
                        LbOpenCloseVelocityValue.Content = "0.00";
                        app.logger.Info("Выбран режим закрытия на скорости 0%");
                        break;
                    case 1:
                        PageUpdate(0);
                        PageUpdate(23);
                        statemode = 2;
                        freqIn = 50 * 0.3;
                        LbOpenCloseVelocityValue.Content = (velocity * 0.3).ToString("#0.00");
                        app.logger.Info("Выбран режим закрытия на скорости 30%");
                        break;
                    case 2:
                        PageUpdate(0);
                        PageUpdate(26);
                        statemode = 2;
                        freqIn = 50 * 0.6;
                        LbOpenCloseVelocityValue.Content = (velocity * 0.6).ToString("#0.00");
                        app.logger.Info("Выбран режим закрытия на скорости 60%");
                        break;
                    case 3:
                        PageUpdate(0);
                        PageUpdate(29);
                        closeBtCount = -1;
                        statemode = 2;
                        freqIn = 50;
                        LbOpenCloseVelocityValue.Content = velocity.ToString("#0.00");
                        app.logger.Info("Выбран режим закрытия на скорости 100%");
                        break;
                }
            }

           
        }

        private void BtUp_Click(object sender, RoutedEventArgs e)
        {
            //mode 3
            if (UPDN_ws_state != 3)
            {
                upBtCount += 1;
                double velocity = (axis_1.MotorMaxRevPerMin / axis_1.ReductionCx) * axis_1.DistancePerRevInMM / 60000;
                switch (upBtCount)
                {
                    case 0:
                        PageUpdate(0);
                        statemode = 0;
                        freqIn = 0.0;
                        LbUpDownVelocityValue.Content = "0.00";
                        app.logger.Info("Выбран режим подъема на скорости 0%");
                        break;
                    case 1:
                        PageUpdate(0);
                        PageUpdate(33);
                        statemode = 3;
                        freqIn = 50 * 0.3;
                        LbUpDownVelocityValue.Content = (velocity * 0.3).ToString("#0.00");
                        app.logger.Info("Выбран режим подъема на скорости 30%");
                        break;
                    case 2:
                        PageUpdate(0);
                        PageUpdate(36);
                        statemode = 3;
                        freqIn = 50 * 0.6;
                        LbUpDownVelocityValue.Content = (velocity * 0.6).ToString("#0.00");
                        app.logger.Info("Выбран режим подъема на скорости 60%");
                        break;
                    case 3:
                        PageUpdate(0);
                        PageUpdate(39);
                        upBtCount = -1;
                        statemode = 3;
                        freqIn = 50;
                        LbUpDownVelocityValue.Content = velocity.ToString("#0.00");
                        app.logger.Info("Выбран режим подъема на скорости 100%");
                        break;
                }
            }
        }

        private void BtDown_Click(object sender, RoutedEventArgs e)
        {
            //mode 4
            if (UPDN_ws_state != 4)
            {
                downBtCount += 1;
                double velocity = (axis_1.MotorMaxRevPerMin / axis_1.ReductionCx) * axis_1.DistancePerRevInMM / 60000;
                switch (downBtCount)
                {
                    case 0:
                        PageUpdate(0);
                        statemode = 0;
                        freqIn = 0.0;
                        LbUpDownVelocityValue.Content = "0.00";
                        app.logger.Info("Выбран режим спуска на скорости 0%");
                        break;
                    case 1:
                        PageUpdate(0);
                        PageUpdate(43);
                        statemode = 4;
                        freqIn = 50 * 0.3;
                        LbUpDownVelocityValue.Content = (velocity * 0.3).ToString("#0.00");
                        app.logger.Info("Выбран режим спуска на скорости 30%");
                        break;
                    case 2:
                        PageUpdate(0);
                        PageUpdate(46);
                        statemode = 4;
                        freqIn = 50 * 0.6;
                        LbUpDownVelocityValue.Content = (velocity * 0.6).ToString("#0.00");
                        app.logger.Info("Выбран режим спуска на скорости 60%");
                        break;
                    case 3:
                        PageUpdate(0);
                        PageUpdate(49);
                        downBtCount = -1;
                        statemode = 4;
                        freqIn = 50;
                        LbUpDownVelocityValue.Content = velocity.ToString("#0.00");
                        app.logger.Info("Выбран режим спуска на скорости 100%");
                        break;
                }
            }
        }
        private void startMoving()
        {
            switch (statemode)
            {
                case 0:
                    // nothing todo
                    break;
                case 1: // run open button set
                   if (cm.IsConnected && axis_1.Remote)                                       
                   {
                        try
                        {
                            cm.client.WriteSymbol("Axis_1.Fwd", true, reloadInfo: false);
                            //cm.client.WriteSymbol("Axis_1.Rev", true, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Frequency_in", freqIn, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Acceleration", axis_1.Acceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Deceleration", axis_1.Deceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.EnableFreeRun", true, reloadInfo: false);
                            LbSplash.Content = "Открытие занавеса";
                            TxbErrorMsg.Text = "";
                            app.logger.Info("Открытие занавеса");
                        }
                        catch (Exception e)
                        {
                            LbSplash.Content = "Ошибка подключения: " + e.Message;
                        }
                    }
                    else
                    {
                        //TxbErrorMsg.Text = "Нет связи с ПЧ";
                        LbSplash.Content = "Нет связи с ПЧ";
                        LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                        HideControls();
                    }
                    break;
                case 2:// run close button set
                    if (cm.IsConnected && axis_1.Remote)
                    {
                        try
                        {
                            //cm.client.WriteSymbol("Axis_1.Fwd", true, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Rev", true, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Frequency_in", freqIn, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Acceleration", axis_1.Acceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Deceleration", axis_1.Deceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.EnableFreeRun", true, reloadInfo: false);
                            LbSplash.Content = "Закрытие занавеса";                      
                            TxbErrorMsg.Text = "";
                            app.logger.Info("Закрытие занавеса");
                        }
                        catch (Exception e)
                        {
                            LbSplash.Content = "Ошибка подключения: " + e.Message;
                        }
                    }
                    else
                    {
                        //TxbErrorMsg.Text = "Нет связи с ПЧ";
                        LbSplash.Content = "Нет связи с ПЧ";
                        LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                        HideControls();
                    }
                    break;
                case 3: // 
                    if (cm.IsConnected && axis_2.Remote)
                    {
                        try {
                            cm.client.WriteSymbol("Axis_2.Fwd", true, reloadInfo: false);
                           // cm.client.WriteSymbol("Axis_2.Rev", true, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Frequency_in", freqIn, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Acceleration", axis_2.Acceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Deceleration", axis_2.Deceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableFreeRun", true, reloadInfo: false);
                            LbSplash.Content = "Подъем занавеса";                    
                            TxbErrorMsg.Text = "";
                            app.logger.Info("Подъем занавеса");
                        }
                        catch (Exception e)
                        {
                            LbSplash.Content = "Ошибка подключения: " + e.Message;
                        }
                    }
                    else
                    {
                        //TxbErrorMsg.Text = "Нет связи с ПЧ";
                        LbSplash.Content = "Нет связи с ПЧ";
                        LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                        HideControls();
                    }
                    break;
                case 4:
                    if (cm.IsConnected && axis_2.Remote)
                    {
                        try {
                            cm.client.WriteSymbol("Axis_2.Rev", true, reloadInfo: false);
                            //cm.client.WriteSymbol("Axis_2.Fwd", true, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Frequency_in", freqIn, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Acceleration", axis_2.Acceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Deceleration", axis_2.Deceleration, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableFreeRun", true, reloadInfo: false);
                            LbSplash.Content = "Спуск занавеса";                  
                            TxbErrorMsg.Text = "";
                            app.logger.Info("Спуск занавеса");
                        }
                        catch (Exception e)
                        {
                            LbSplash.Content = "Ошибка подключения: " + e.Message;
                        }
                    }
                    else
                    {
                        //TxbErrorMsg.Text = "Нет связи с ПЧ";
                        LbSplash.Content = "Нет связи с ПЧ";
                        LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                        HideControls();
                    }
                    break;

            }
            LbSplash.Foreground = new SolidColorBrush(Colors.White);
            ShowControls();
        }
        private void stopMoving()
        {
            try
            {
                switch (statemode)
                {
                    case 0:
                        // nothing todo
                        break;
                    case 1: // stop open button set
                        if (cm.IsConnected && axis_1.Remote)                          
                        {                          
                            cm.client.WriteSymbol("Axis_1.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.EnableFreeRun", false, reloadInfo: false);
                            TxbErrorMsg.Text = "";
                            app.logger.Info("Остановка открытия занавеса");
                        }
                        else
                        {
                            //TxbErrorMsg.Text = "Нет связи с ПЧ";
                            LbSplash.Content = "Нет связи с ПЛК";
                            LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                            HideControls();
                        }
                        break;
                    case 2: // stop close button set
                        if (cm.IsConnected && axis_1.Remote)
                        {
                            cm.client.WriteSymbol("Axis_1.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_1.EnableFreeRun", false, reloadInfo: false);
                            TxbErrorMsg.Text = "";
                            app.logger.Info("Остановка закрытия занавеса");
                        }
                        else
                        {
                            //TxbErrorMsg.Text = "Нет связи с ПЧ";
                            LbSplash.Content = "Нет связи с ПЧ";
                            LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                            HideControls();
                        }
                        break;

                    case 3: // stop up button set
                        if (cm.IsConnected && axis_2.Remote)
                        {
                            cm.client.WriteSymbol("Axis_2.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableFreeRun", false, reloadInfo: false);
                            TxbErrorMsg.Text = "";
                            app.logger.Info("Остановка подъема занавеса");
                        }
                        else
                        {
                            //TxbErrorMsg.Text = "Нет связи с ПЧ";
                            LbSplash.Content = "Нет связи с ПЧ";
                            LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                            HideControls();
                        }
                        break;
                    case 4: // stop down button set
                        if (cm.IsConnected && axis_2.Remote)
                        {
                            cm.client.WriteSymbol("Axis_2.Fwd", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.Rev", false, reloadInfo: false);
                            cm.client.WriteSymbol("Axis_2.EnableFreeRun", false, reloadInfo: false);
                            TxbErrorMsg.Text = "";
                            app.logger.Info("Остановка спуска занавеса");
                        }
                        else
                        {
                            //TxbErrorMsg.Text = "Нет связи с ПЧ";
                            LbSplash.Content = "Нет связи с ПЧ";
                            LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                            HideControls();
                        }
                        break;
                }
                LbSplash.Content = axis_1.Remote ? "АРЗ готов к работе. " : "АРЗ не подключен. ";
                LbSplash.Content += axis_2.Remote ? "ПОЗ готов к работе" : "ПОЗ не подключен";
                LbSplash.Foreground = new SolidColorBrush(Colors.White);
                ShowControls();
            }
            catch (AdsException)
            {
                //TxbErrorMsg.Text = "Потеряна связь с ПЛК";
                LbSplash.Content = "Потеряна связь с ПЛК";
                LbSplash.Foreground = new SolidColorBrush(Colors.Red);
                HideControls();

            }        
            catch (Exception e)
            {
                LbSplash.Content = "Ошибка подключения: " + e.Message;
            }
}
        private void PageUpdate(int stateindex)
        {
            switch (stateindex)
            {
                case 0:
                    ImOpenLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_00.png"));
                    ImOpenRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_00.png"));
                    ImCloseLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_00.png"));
                    ImCloseRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_00.png"));
                    ImUpLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_00.png"));
                    ImUpRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_00.png"));
                    ImDownLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_00.png"));
                    ImDownRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_00.png"));
                    /*openBtCount = 0;
                    closeBtCount = 0;
                    upBtCount = 0;
                    downBtCount = 0;*/
                   
                    break;
                case 13:
                    ImOpenLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_30.png"));
                    ImOpenRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_30.png"));
                    closeBtCount = 0;
                    upBtCount = 0;
                    downBtCount = 0;
                    break;
                case 16:
                    ImOpenLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_60.png"));
                    ImOpenRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_60.png"));
                    closeBtCount = 0;
                    upBtCount = 0;
                    downBtCount = 0;
                    break;
                case 19:
                    ImOpenLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_90.png"));
                    ImOpenRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_90.png"));                    
                    closeBtCount = 0;
                    upBtCount = 0;
                    downBtCount = 0;
                    break;

                case 23:
                    ImCloseLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_30.png"));
                    ImCloseRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_30.png"));
                    openBtCount = 0;                    
                    upBtCount = 0;
                    downBtCount = 0;
                    break;
                case 26:
                    ImCloseLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_60.png"));
                    ImCloseRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_60.png"));
                    openBtCount = 0;
                    upBtCount = 0;
                    downBtCount = 0;
                    break;
                case 29:
                    ImCloseLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arRight_90.png"));
                    ImCloseRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arLeft_90.png"));
                    openBtCount = 0;
                    upBtCount = 0;
                    downBtCount = 0;
                    break;

                case 33:
                    ImUpLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_30.png"));
                    ImUpRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_30.png"));
                    openBtCount = 0;
                    closeBtCount = 0;                   
                    downBtCount = 0;
                    break;

                case 36:
                    ImUpLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_60.png"));
                    ImUpRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_60.png"));
                    openBtCount = 0;
                    closeBtCount = 0;
                    downBtCount = 0;
                    break;

                case 39:
                    ImUpLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_90.png"));
                    ImUpRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arUp_90.png"));
                    openBtCount = 0;
                    closeBtCount = 0;
                    downBtCount = 0;
                    break;

                case 43:
                    ImDownLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_30.png"));
                    ImDownRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_30.png"));
                    openBtCount = 0;
                    closeBtCount = 0;
                    upBtCount = 0;
                    
                    break;
                case 46:
                    ImDownLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_60.png"));
                    ImDownRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_60.png"));
                    openBtCount = 0;
                    closeBtCount = 0;
                    upBtCount = 0;
                    break;
                case 49:
                    ImDownLeft.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_90.png"));
                    ImDownRight.Source = new BitmapImage(new Uri("pack://application:,,,/images/arDown_90.png"));
                    openBtCount = 0;
                    closeBtCount = 0;
                    upBtCount = 0;
                    break;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            int iErrCode; 
            cm.Dispose();
            //dispatcherTimer.Stop();
            sw.Stop();
            if ((int)ICPDAS_USBIO.ERR_CODE.ERR_NO_ERR != (iErrCode = m_USBIO.CloseDevice()))
            {
                TxbErrorMsg.Text = "Failed to close USB-IO";
            }
            hpt.Dispose();   

        }
        
        private void HideControls()
        {
            ImOpenLeft.Visibility = Visibility.Hidden;
            ImOpenRight.Visibility = Visibility.Hidden;
            ImCloseLeft.Visibility = Visibility.Hidden;
            ImCloseRight.Visibility = Visibility.Hidden;
            ImUpLeft.Visibility = Visibility.Hidden;
            ImUpRight.Visibility = Visibility.Hidden;
            ImDownLeft.Visibility = Visibility.Hidden;
            ImDownRight.Visibility = Visibility.Hidden;
            Grpb_Bts.Visibility = Visibility.Hidden;
            BtOpen.Visibility = Visibility.Collapsed;
            BtClose.Visibility = Visibility.Collapsed;
            BtUp.Visibility = Visibility.Collapsed;
            BtDown.Visibility = Visibility.Collapsed;
            SP_Info_OpenClose.Visibility = Visibility.Hidden;
            SP_Info_UpDown.Visibility = Visibility.Hidden;

        }
        private void ShowControls()
        {
            ImOpenLeft.Visibility = Visibility.Visible;
            ImOpenRight.Visibility = Visibility.Visible;
            ImCloseLeft.Visibility = Visibility.Visible;
            ImCloseRight.Visibility = Visibility.Visible;
            ImUpLeft.Visibility = Visibility.Visible;
            ImUpRight.Visibility = Visibility.Visible;
            ImDownLeft.Visibility = Visibility.Visible;
            ImDownRight.Visibility = Visibility.Visible;
            Grpb_Bts.Visibility = Visibility.Visible;
            BtOpen.Visibility = Visibility.Visible;
            BtClose.Visibility = Visibility.Visible;
            BtUp.Visibility = Visibility.Visible;
            BtDown.Visibility = Visibility.Visible;
            SP_Info_OpenClose.Visibility = Visibility.Visible;
            SP_Info_UpDown.Visibility = Visibility.Visible;
        }

        private void BtAlarmInfoOK_Click(object sender, RoutedEventArgs e)
        {
            BrdInfo.Visibility = Visibility.Hidden;
        }
    }
}
