using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
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
using static WpfTwinCAT.MainWindow;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для TestPage.xaml
    /// </summary>
    public partial class TestPage : Page
    {
        private CommunicationManager cmr;
        private TC_Axis axis_1 = new TC_Axis();
        private TC_TR tr_1 = new TC_TR();
        private TC_Axis axis_2 = new TC_Axis();
        private TC_TR tr_2 = new TC_TR();
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private ManagementEventWatcher watcher = new ManagementEventWatcher();
        //private WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 or EventType = 3");
        private string driveName;
        private int eventType;
        public TestPage()
        {
            InitializeComponent();
            //  DispatcherTimer setup
            dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 180);
        }

        private void OnLoad(object sender, RoutedEventArgs e) // this event was added in HomePage.xaml header
        {
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.NavPanelBottom.Visibility = Visibility.Visible;
            parentWindow.bBack.Visibility = Visibility.Visible;
            parentWindow.brdAdmin.Visibility = Visibility.Hidden;
            parentWindow.PageTitle.Content = "Результат теста системы";
            cmr = new CommunicationManager(851);
            cmr.tryConnect();
            AxisVarLoad(axis_1, "Axis_1");
            TRVarLoad(tr_1, "TR_1");
            AxisVarLoad(axis_2, "Axis_2");
            TRVarLoad(tr_2, "TR_2");
            dispatcherTimer.Start();

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
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            //return to home page
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.MainFrame.NavigationService.Navigate(new HomePage());
        }
        private void AxisVarLoad(TC_Axis asix, string axisname)
        {
            //load axis variables            
            if (cmr.IsConnected)
            {
                asix.Name = axisname;
                TbxTestResults.Text += "\r\n" + asix.Name + " *****************************\r\n";
                int Alarm_Fault_Handle = 0;
                int Alarm_Status_Handle = 0;

                try
                {
                    Alarm_Fault_Handle = cmr.client.CreateVariableHandle(axisname + ".Alarm_Fault");
                    Alarm_Status_Handle = cmr.client.CreateVariableHandle(axisname + ".Alarm_Status");
                    asix.ADS_address = DecodeAmsAddr((AmsAddr)cmr.client.ReadSymbol(axisname + ".ADS_address", typeof(AmsAddr), false));
                    asix.Alarm_Fault = cmr.client.ReadAnyString(Alarm_Fault_Handle, 80, Encoding.Default);
                    asix.Alarm_Status = cmr.client.ReadAnyString(Alarm_Status_Handle, 80, Encoding.Default);
                    TbxTestResults.Text += asix.ADS_address + "\r\n" + 
                                        "Alarm Fault: " + asix.Alarm_Fault + "\r\n" + 
                                        "Alarm Status: " + asix.Alarm_Status + "\r\n";

                    cmr.client.DeleteVariableHandle(Alarm_Fault_Handle);
                    cmr.client.DeleteVariableHandle(Alarm_Status_Handle);

                    asix.Fault = (bool)cmr.client.ReadSymbol(axisname + ".Fault", typeof(bool), false);
                    asix.Warning = (bool)cmr.client.ReadSymbol(axisname + ".Warning", typeof(bool), false);
                    asix.Remote = (bool)cmr.client.ReadSymbol(axisname + ".Remote", typeof(bool), false);
                    asix.Connection_Error = (bool)cmr.client.ReadSymbol(axisname + ".Connection_Error", typeof(bool), false);
                    TbxTestResults.Text += "Fault: " + asix.Fault.ToString() + "\r\n" +
                                            "Warning: " + asix.Warning.ToString() + "\r\n" +
                                            "Remote: " + asix.Remote.ToString() + "\r\n" +
                                            "Connection Error: " + asix.Connection_Error.ToString() + "\r\n";
                    asix.Status = (UInt16)cmr.client.ReadSymbol(axisname + ".Status", typeof(UInt16), false);
                    asix.D081_FaultCause = (UInt16)cmr.client.ReadSymbol(axisname + ".D081_FaultCause", typeof(UInt16), false);
                    asix.D081_FaultInverterStatus = (UInt16)cmr.client.ReadSymbol(axisname + ".D081_FaultInverterStatus", typeof(UInt16), false);
                    asix.D005_MF_InputsMonitor = (UInt16)cmr.client.ReadSymbol(axisname + ".D005_MF_InputsMonitor", typeof(UInt16), false);
                    TbxTestResults.Text += "Status: " + asix.Status.ToString() + "\r\n" +
                                           "D081_FaultCause: " + asix.D081_FaultCause.ToString() + "\r\n" +
                                           "D081_FaultInverterStatus: " + asix.D081_FaultInverterStatus.ToString() + "\r\n" +
                                           "D005_MF_InputsMonitor: " + asix.D005_MF_InputsMonitor.ToString() + "\r\n";
                }
                catch (Exception e)
                {
                    TbxTestResults.Text += "Ошибка подключения: " + e.Message + "\r\n";
                }         

            }
        }
        private void TRVarLoad(TC_TR tr, string trname)
        {
            //load axis variables            
            if (cmr.IsConnected)
            {
                tr.Name = trname;
                TbxTestResults.Text += "\r\n" +  tr.Name + " *****************************\r\n";

                
                try
                {
                    tr.ADS_address = DecodeAmsAddr((AmsAddr)cmr.client.ReadSymbol(trname + ".ADS_address", typeof(AmsAddr), false));

                    TbxTestResults.Text += tr.ADS_address + "\r\n";                                        

                    tr.PositionValue = (UInt32)cmr.client.ReadSymbol(trname + ".PositionValue", typeof(UInt32), false);
                    tr.TimeStamp = (UInt32)cmr.client.ReadSymbol(trname + ".TimeStamp", typeof(UInt32), false);
                    tr.State = (UInt16)cmr.client.ReadSymbol(trname + ".State", typeof(UInt16), false);

                    TbxTestResults.Text += "States: " + tr.State.ToString() + "\r\n" +
                                           "TimeStamp: " + tr.TimeStamp.ToString() + "\r\n" +
                                           "PositionValue: " + tr.PositionValue.ToString() + "\r\n";
                }
                catch(Exception e)
                {
                    TbxTestResults.Text += "Ошибка подключения: " + e.Message + "\r\n";
                }

            }
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            cmr.Dispose();
            dispatcherTimer.Stop();
            watcher.Stop();
        }
        private string DecodeAmsAddr(AmsAddr structure)
        {
            string resAddr = "NetID: ";
            for (int i=0; i < 6; i++)
            {
                resAddr += i==5? (structure.netId[i].ToString()) : (structure.netId[i].ToString() + ".");
            }
            resAddr += ", port: " + structure.port.ToString();
            return resAddr;
        }

        private void BtWriteFile_Click(object sender, RoutedEventArgs e)
        {
            // write log file to USB disk
            //show instruction box
            if ((driveName != null) && (eventType == 2))
            {
                //usb media inserted
                TxbWriteFileInstruction.Text = "Записать log файлы на диск " + driveName + "? Файлы будут записаны в каталог <logs> USB носителя";
            }
            else
            {
                TxbWriteFileInstruction.Text = "Установите карту памяти в USB слот (если карта уже установлена, извлеките ее и подключите снова)";
            }
           
            BrdWriteFileInstruction.Visibility = Visibility.Visible;
            
        }

        private void BtWriteFileOK_Click(object sender, RoutedEventArgs e)
        {
            BrdWriteFileInstruction.Visibility = Visibility.Hidden;
            // write file to USB flash here
            if ((driveName != null) && (eventType == 2))
            {
                //observe the usb disc to find *.log files in the /logs/ dir
                string[] tracks;
                try
                {
                    string copyDir = driveName + "/logs/";
                    string sourceDir = Environment.CurrentDirectory + "/logs/";
                    tracks = Directory.GetFiles(sourceDir, "*.log");

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
                                //copy file to Environment.CurrentDirectory + "/logs/" directory
                                File.Copy(System.IO.Path.Combine(sourceDir, fName), System.IO.Path.Combine(copyDir, fName), true);
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
                            
                        }
                    }

                }
                catch (Exception err)
                {
                    Console.WriteLine("The process failed: {0}", err.ToString());
                }
                TxbInfo.Text = "Запись файлов успешно завершена";
                BrdInfo.Visibility = Visibility.Visible;
                
            }
        }

        private void BtWriteFileBack_Click(object sender, RoutedEventArgs e)
        {
            BrdWriteFileInstruction.Visibility = Visibility.Hidden;
        }

        private void BtInfoOK_Click(object sender, RoutedEventArgs e)
        {
            BrdInfo.Visibility = Visibility.Hidden;
        }

        private void BtInfoBack_Click(object sender, RoutedEventArgs e)
        {
            BrdInfo.Visibility = Visibility.Hidden;
        }
    }

    // TwinCAT2 Pack = 1, TwinCAT3 Pack = 0
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public class AmsAddr
    {
        //public AmsNetId netId = new AmsNetId();
        public byte[] netId = new byte[6];
        public ushort port;
    }
}