using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using WpfClinicalElectron;

[assembly: ESAPIScript(IsWriteable =true)]

namespace VMS.TPS
{
    public class Script
    {
        public void Execute(ScriptContext context, Window Mainwindow)
        {
            context.Patient.BeginModifications();

            // Add existing WPF control to the script window.
            MainControl mainControl = new MainControl();
           
            Mainwindow.Content = mainControl;
            Mainwindow.SizeToContent = SizeToContent.WidthAndHeight;
            Mainwindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Mainwindow.Title = "Clinical Electron Setup";

            mainControl.patient = context.Patient;
        }
    }
}
