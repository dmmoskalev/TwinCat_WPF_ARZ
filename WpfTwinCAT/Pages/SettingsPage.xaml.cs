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
using WpfTwinCAT.Pages;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Threading;

namespace WpfTwinCAT.Pages
{
    /// <summary>
    /// Логика взаимодействия для SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        public SettingsPage()
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
            parentWindow.PageTitle.Content = "Список настроек системы";
            // read settings from xml 
            LoadSettings();
            dispatcherTimer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            //return to home page
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.MainFrame.NavigationService.Navigate(new HomePage());
        }
        private void ValueEditCall(string sourcePrefix)
        {
            if (!(FindName("Lb_value_" + sourcePrefix) is Label lb_caller))
            {
                throw new Exception("Can't find resource Lb_value_" + sourcePrefix);
            }
           if (!(FindName("Txt_" + sourcePrefix) is TextBlock txt_caller))
            {
                throw new Exception("Can't find resource Lb_value_" + sourcePrefix);
            }
            
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.ValueToEdit = (string)lb_caller.Content;
            parentWindow.SourceName = lb_caller.Name;
            parentWindow.TextTagBuffer = (string)txt_caller.Tag;
            parentWindow.TextHelpBuffer = txt_caller.Text;
            parentWindow.MainFrame.NavigationService.Navigate(new ValueEditPage());
                
            
        }
       
        private void Bt_0_click(object sender, RoutedEventArgs e)
        {
            Button senderBtn = sender as Button;
            // get index of the edited parameter
            MainWindow parentWindow = Window.GetWindow(this) as MainWindow;
            parentWindow.Prefix = senderBtn.Name.Substring((senderBtn.Name.Length-6),6);
            ValueEditCall(parentWindow.Prefix);
        }
        private void LoadSettings()
        {
            Axis axis1 = new Axis();
            Axis axis2 = new Axis();
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
                foreach(Axis a in ArrayOfAxes)
                {
                    axn += 1;
                    //if (axn == 2) { group = 9; ind = 9; }; //page shift
                    if (!(FindName("TbxSet_" + string.Format("{0:d2}", axn)) is TextBlock txtSet))
                    {
                        throw new Exception("Can't find resource TbxSet_" + string.Format("{0:d2}", axn));
                    }
                    else
                    {
                        txtSet.Text = a.Name;
                    }
                    
                    foreach (Group g in a.ArrayOfGroupes)
                    {
                        group += 1;
                        ind = 0;
                        if (group == 2) { group = 0; }; //page shift
                        adr = string.Format("{0:d2}", axn) + string.Format("{0:d2}", group);
                        if (!(FindName("LB_group_descriptor_" + adr) is Label group_descriptor))
                        {
                            throw new Exception("Can't find resource Lb_group_descriptor_" + adr);
                        }
                        else
                        {
                            group_descriptor.Content = g.Name;

                        }
                       
                        foreach (Param p in g.ArrayOfParameters)
                        {
                            
                            //if (ind == 6) { ind = 0; }; //page shift
                            // build the address of parameter
                            adr = string.Format("{0:d2}", axn) + string.Format("{0:d2}", group) + string.Format("{0:d2}", ind);

                            if (!(FindName("Lb_descriptor_" + adr) is Label descriptor))
                            {
                                throw new Exception("Can't find resource Lb_descriptor_" + adr);
                            }
                            else
                            {
                                descriptor.Content = p.Descriptor;
                            }

                            if (!(FindName("Lb_value_" + adr) is Label value_lb))
                            {
                                throw new Exception("Can't find resource Lb_value_" + adr);
                            }
                            else
                            {
                               value_lb.Content = p.Value;
                            }
                            if (!(FindName("Txt_" + adr) is TextBlock helptag))
                            {
                                throw new Exception("Can't find resource Txt_" + adr);
                            }
                            else
                            {
                                helptag.Tag = p.Tag;
                                helptag.Text = p.Help;
                            }
                            ind += 1;
                        }
                    }
                }

            }
            else { throw new Exception("File " + axis1.XMLFileName + " is not founded"); }

        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
        }

        private void Bt_calibration_click(object sender, RoutedEventArgs e)
        {
            // calibration dialog
        }
    }
}
