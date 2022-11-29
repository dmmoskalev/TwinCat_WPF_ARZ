using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;

using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NLog;

namespace WpfTwinCAT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public Logger logger = LogManager.GetCurrentClassLogger();
        public int COMM_TIMEOUT = 100;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow wnd = new MainWindow();
            
            if (e.Args.Length == 1)
            {
                wnd.PRODUCTION_MODE = e.Args[0] == "pm";
            }           
                
            wnd.Show();
        }
    }
    
    public class Utf8StreamWriter : StreamWriter
    {
        public Utf8StreamWriter(string path) : base(path) { }
        public override Encoding Encoding { get { return Encoding.UTF8; } }

    }
    [Serializable]
    public class Param
    {
        public string Name { get; set; }
        public string Descriptor { get; set; }
        public string Value { get; set; }
        public string Tag { get; set; }
        public string Help { get; set; }
        public string Address { get; set; }

        public Param() { }

        public Param(string _address, string _name, string _descriptor, string _value, string _tag, string _help)
        {
            Address = _address;
            Name = _name;
            Descriptor = _descriptor;
            Value = _value;
            Tag = _tag;
            Help = _help;
        }

    }
    [Serializable]
    public class Group
    {
        public string Name { get; set; }
        public string Index { get; set; }
        public Param[] ArrayOfParameters { get; set; }
     
        public Group() { }
        public Group(string _index, string _name, Param[] _arrayOfParam)
        {
            ArrayOfParameters = _arrayOfParam;
            Name = _name;
            Index = _index;
        }

    }
    [Serializable]
    public class Axis
    {
        public string Name { get; set; }
        public string Index { get; set; }
        public string XMLFileName = Environment.CurrentDirectory + "/axis_settings.xml";
        public Group[] ArrayOfGroupes { get; set; }
        public Axis() { }
        public Axis(string _index, string _name, Group[] _arrayOfGroup)
        {
            ArrayOfGroupes = _arrayOfGroup;
            Name = _name;
            Index = _index;
        }


    }
    public class Key
    {
        public string KeyValue { get; set; }
        public string XML_KEY_FileName = Environment.CurrentDirectory + "/app_settings.xml";
        public Key() { }
        public Key(string _keyvalue) 
        {
            KeyValue = _keyvalue;

        }

    }
    public class TC_Axis
    {//convertor class
        public string Name { get; set; }
        public string ADS_address { get; set; }
        public uint Status { get; set; } // has bool representatives
        #region Status bits description
        /*
        0 Forward operation in progress:
		        0: Stopped/during reverse operation
		        1: During forward operation
        1 Reverse operation in progress:
		        0: Stopped/during forward operation
		        1: During reverse operation
        3 Fault:
		        0: No error trip occurred for the unit or inverter
		        1: Error trip occurred for the unit or inverter
        7 Warning:
		        0: No warning occurred for the unit or inverter
		        1: Warning occurred for the unit or inverter
        9 Remote:
		        0: Local(Operations from EtherCAT are disabled)
		        1: Remote(Operations from EtherCAT are enabled)
        12 Frequency matching:
		        0: During acceleration/deceleration
		        1: Frequency matched
        15 Connection error between the Optional Unit and inverter:
		        0: Normal
		        1: Error(Cannot update data FOR the inverter. TO restore, turn the power OFF and then ON again.)
        */
        #endregion
        public bool Fault { get; set; }
        public bool During_Rev { get; set; }
        public bool During_Fwd { get; set; }
        public bool Freq_Match { get; set; }
        public bool Warning { get; set; }
        public bool Remote { get; set; }
        public bool Connection_Error { get; set; }

        public int D081_FaultCause { get; set; }
        public int D081_FaultInverterStatus { get; set; }
        public int D005_MF_InputsMonitor { get; set; } // has info about WorkingLimitNotAchieved flag
        public bool WorkingLimitNotAchieved { get; set; }
        public string Alarm_Fault { get; set; }
        public string Alarm_Status { get; set; }
       
        public bool EnableFreeRun { get; set; }
        public bool EnableAbsolute { get; set; }
        public bool EnableRelative { get; set; }

        public uint Command { get; set; } // is consist of FWD,REv,FaultReset bits
        /*	Command
	        Bit 0: Forward/stop
	        Bit 1: Reverse/stop
	        Bit 7: Fault reset 
        */
        public bool Fwd { get; set; }
        public bool Rev { get; set; }
        public bool Fault_Reset { get; set; }
        public double Frequency_out { get; set; }
        public double Frequency_in { get; set; } // has INT value*100 in FreqReference 0..5000
        public uint FreqReference { get; set; }
        public double Frequency_in_SlowMotion { get; set; } // about 5
        public double Acceleration { get; set; } // has INT value*100 in F002_1st_Acceleration
        public UInt32 F002_1st_Acceleration { get; set; }
        public double Deceleration { get; set; } // has INT value*100 in F003_1st_Deceleration
        public UInt32 F003_1st_Deceleration { get; set; }    
        
        public uint ScanPeriod { get; set; }
       
        public uint Output_Frequency_Monitor { get; set; }
        public uint Output_Current_Monitor { get; set; }
        public uint State { get; set; }
        public uint MotorMaxRevPerMin { get; set; } // maximum revolutions per minute on max frequency 50Hz - motor parametr
        public double ReductionCx { get; set; } //reduction coefficient in case the reductor is installed
        public double DistancePerRevInMM { get; set; } //distance passed during one revolution of drum driver in mm
        public double JogDistanceInMM { get; set; }



        public TC_Axis() { }

        public TC_Axis(string _name, string _address, uint _status, int _d081fc, int _d081fis, int _d005mf,
                string _alarmfault, string _alarmstatus, bool _fault, bool _warning, bool _remote,
                bool _connectionerror)
        {
            Name = _name;
            ADS_address = _address;
            Status = _status;
            D081_FaultCause = _d081fc;
            D081_FaultInverterStatus = _d081fis;
            D005_MF_InputsMonitor = _d005mf;
            Alarm_Fault = _alarmfault;
            Alarm_Status = _alarmstatus;
            Fault = _fault;
            Warning = _warning;
            Remote = _remote;
            Connection_Error = _connectionerror;
        }

        #region Axis fields description
        /*
            ADS_address AT %I*:AMSADDR;
	        Output_Frequency_Monitor AT %I*: UINT;
	        Output_Current_Monitor AT %I*: UINT;
	        //slave state PLC report
	        State AT %I*: UINT;
	        //invertor status
	        Status AT %I*: UINT;
	        //monitor status vars
	        D081_FaultCause AT %I*: INT;
	        D081_FaultInverterStatus AT %I*: INT;
	        D005_MF_InputsMonitor AT %I*: INT;
	        MotorMaxRevPerMin:UINT:=1350; // maximum revolutions per minute on max frequency 50Hz - motor parametr
	        ReductionCx:REAL:=20.0; //reduction coefficient in case the reductor is installed
	        DistancePerRevInMM:REAL:=680.0; //distance passed during one revolution of drum driver in mm
	        JogDistanceInMM:REAL :=50; // override lomit back jog distance (jump back) in mm
	
	        Alarm_Fault: STRING[256];
	        Alarm_Status: STRING[256];
	        //output command and parameters
	        Command AT %Q*: UINT;
	
	        // move monitor status
	        During_Fwd AT %Q*: BOOL;
	        During_Rev AT %Q*: BOOL;
	        Fault AT %Q*: BOOL;
	        Warning AT %Q*: BOOL;
	        Remote AT %Q*: BOOL;
	        Freq_Match AT %Q*: BOOL;
	        Connection_Error AT %Q*: BOOL;
	        Frequency_out AT %Q*: REAL;
	
	        //move controls
	
	        EnableFreeRun AT %Q*: BOOL;
	        EnableAbsolute AT %Q*: BOOL;
	        EnableRelative AT %Q*: BOOL;
	        Fwd AT %Q*:BOOL;
	        Rev AT %Q*:BOOL;
	        Frequency_in AT %Q*: REAL;
	        Frequency_in_SlowMotion AT %Q*: REAL:=5.0;
	        Acceleration AT %Q*: REAL;
	        Deceleration AT %Q*: REAL;
	        F002_1st_Acceleration AT %Q*: DINT;
	        F003_1st_Deceleration AT %Q* : DINT;
	        FreqReference AT %Q*: UINT;
	        Fault_Reset AT %Q*: BOOL;
	        WorkingLimitNotAchieved AT %Q*: BOOL;
	        ScanPeriod:UDINT :=10; // system PlcTask scan period in millisec*/
        #endregion
    }
    public class TC_TR
    {
        // encoder class
        public string Name { get; set; }
        public string ADS_address { get; set; }
        public uint PositionValue { get; set; }
        public uint TimeStamp { get; set; }
        public int State { get; set; }
        public double EncStepsPerOneMM { get; set; } //= 8192 steps per rev/680mm = 12.04
        public uint PositionTarget { get; set; } // new position target in mm
        public uint DistanceTarget { get; set; } // distance for relative moving in mm
        public bool DirectionRelative { get; set; } // true = fwd, false = backward
        public uint Alarm { get; set; }
        public uint UpLimitInSteps { get; set; } 
        public uint LowLimitInSteps { get; set; }
        public uint ZeroLevel { get; set; }  //zero means zero position of encoder(!). the value keeps distance (mm) from encoder zero mark to zero level hoist position
        public uint Resolution { get; set; } // resolution in steps per revolution
        public TC_TR() { }

        public TC_TR(string _name, string _address, uint _posvalue, uint _ts, int _state)
        {
            Name = _name;
            ADS_address = _address;
            PositionValue = _posvalue;
            TimeStamp = _ts;
            State = _state;
        }
        #region TR fields description
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
    
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public class MoveAbsolutInstruction
    {
        public UInt16 Index { get; set; }
        public float Acceleration { get; set; }
        public float Deceleration { get; set; }
        public float Frequency_in { get; set; }
        public UInt32 TargetDist { get; set; }
        
        public bool FWD { get; set; }

        public MoveAbsolutInstruction() { }
        public MoveAbsolutInstruction(UInt16 _index, float _a, float _d, float _f, UInt32 _td, bool _fwd)
        {
            Index = _index;
            Acceleration = _a;
            Deceleration = _d;
            Frequency_in = _f;
            TargetDist = _td;
            FWD = _fwd;
        }

    }
    public class MoveTrack
    {
        public ObservableCollection<MoveAbsolutInstruction[]> MoveSetCollection { get; set; }
        public MoveTrack() { }
        public MoveTrack(ObservableCollection<MoveAbsolutInstruction[]> _movesetcollection)
        {
            MoveSetCollection = _movesetcollection;
        }
    }
   
}
