#pragma checksum "..\..\..\..\Pages\TestPage.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "BC8CA6DBB10B0E17001749D0E7752EB119CFC287"
//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using WpfTwinCAT.Pages;


namespace WpfTwinCAT.Pages {
    
    
    /// <summary>
    /// TestPage
    /// </summary>
    public partial class TestPage : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 106 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TbxTestResults;
        
        #line default
        #line hidden
        
        
        #line 112 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtWriteFile;
        
        #line default
        #line hidden
        
        
        #line 119 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border BrdWriteFileInstruction;
        
        #line default
        #line hidden
        
        
        #line 123 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtCloseWriteFileInstuction;
        
        #line default
        #line hidden
        
        
        #line 128 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TxbWriteFileInstruction;
        
        #line default
        #line hidden
        
        
        #line 131 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtWriteFileInstructionOK;
        
        #line default
        #line hidden
        
        
        #line 136 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border BrdInfo;
        
        #line default
        #line hidden
        
        
        #line 140 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtCloseInfo;
        
        #line default
        #line hidden
        
        
        #line 145 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock TxbInfo;
        
        #line default
        #line hidden
        
        
        #line 148 "..\..\..\..\Pages\TestPage.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button BtInfoOK;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "5.0.15.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/WpfTwinCAT;V1.0.0.0;component/pages/testpage.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Pages\TestPage.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "5.0.15.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 9 "..\..\..\..\Pages\TestPage.xaml"
            ((WpfTwinCAT.Pages.TestPage)(target)).Loaded += new System.Windows.RoutedEventHandler(this.OnLoad);
            
            #line default
            #line hidden
            
            #line 10 "..\..\..\..\Pages\TestPage.xaml"
            ((WpfTwinCAT.Pages.TestPage)(target)).Unloaded += new System.Windows.RoutedEventHandler(this.OnUnloaded);
            
            #line default
            #line hidden
            return;
            case 2:
            this.TbxTestResults = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.BtWriteFile = ((System.Windows.Controls.Button)(target));
            
            #line 112 "..\..\..\..\Pages\TestPage.xaml"
            this.BtWriteFile.Click += new System.Windows.RoutedEventHandler(this.BtWriteFile_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.BrdWriteFileInstruction = ((System.Windows.Controls.Border)(target));
            return;
            case 5:
            this.BtCloseWriteFileInstuction = ((System.Windows.Controls.Button)(target));
            
            #line 123 "..\..\..\..\Pages\TestPage.xaml"
            this.BtCloseWriteFileInstuction.Click += new System.Windows.RoutedEventHandler(this.BtWriteFileBack_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.TxbWriteFileInstruction = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 7:
            this.BtWriteFileInstructionOK = ((System.Windows.Controls.Button)(target));
            
            #line 131 "..\..\..\..\Pages\TestPage.xaml"
            this.BtWriteFileInstructionOK.Click += new System.Windows.RoutedEventHandler(this.BtWriteFileOK_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.BrdInfo = ((System.Windows.Controls.Border)(target));
            return;
            case 9:
            this.BtCloseInfo = ((System.Windows.Controls.Button)(target));
            
            #line 140 "..\..\..\..\Pages\TestPage.xaml"
            this.BtCloseInfo.Click += new System.Windows.RoutedEventHandler(this.BtInfoBack_Click);
            
            #line default
            #line hidden
            return;
            case 10:
            this.TxbInfo = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 11:
            this.BtInfoOK = ((System.Windows.Controls.Button)(target));
            
            #line 148 "..\..\..\..\Pages\TestPage.xaml"
            this.BtInfoOK.Click += new System.Windows.RoutedEventHandler(this.BtInfoOK_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

