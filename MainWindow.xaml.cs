using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Threading;

namespace ExampleBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        OpenFileDialog openFileDialog1 = new OpenFileDialog();

        public MainWindow()
        {
            InitializeComponent();

            AllocConsole();
        }

       /* private void _load_file(String file)
        {
            if (file.EndsWith(".nc"))
                _load_gcode_file(file);
        }*/

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            String Pfad = "";
            
            openFileDialog1.Filter = "gcode files (*.nc; *.gcode)|*.nc;*.gcode|All files (*.*)|*.*";
            Nullable<bool> result = openFileDialog1.ShowDialog();
/*
            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                Pfad = openFileDialog1.FileName;
            }

            _load_file(Pfad);*/
        }

        private void Menu_Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
