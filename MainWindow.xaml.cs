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
using HelixToolkit.Wpf;

namespace ExampleBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class texteFile
        {
            public string line { get; set; }
            public string text { get; set; }
        }


        GCODE gcodeParser;
        //[DllImport("kernel32.dll", SetLastError = true)]
        //static extern bool AllocConsole();

        OpenFileDialog openFileDialog1 = new OpenFileDialog();
        List<LinesVisual3D> moves;


        public MainWindow()
        {
            InitializeComponent();

           // AllocConsole();
        }

        private void _load_file(String file)
        {
            // Read the file.
            System.IO.StreamReader myFile;
            try
            {
                myFile = new System.IO.StreamReader(file);
            }
            catch
            {
                return;
            }

            String str;

            UInt32 i = 1;
            //-- lecture du fichier
            while ((str = myFile.ReadLine()) != null)
            {
                TextBlock printTextBlock = new TextBlock();
                printTextBlock.Text = str;
                if ((i % 2) == 0)
                    printTextBlock.Background = Brushes.LightGray;
                else
                    printTextBlock.Background = Brushes.LightYellow;
                fileText.Items.Add(printTextBlock);
                i++;
            }
            myFile.Close();

            if (file.EndsWith(".nc"))
            {
                gcodeParser = new GCODE(file);

                moves = gcodeParser.build_3D_Model(0);
            }

            foreach (LinesVisual3D m in moves)
            {
                viewport.Children.Remove(m);
                viewport.Children.Add(m);
            }
            viewport.CameraController.AddRotateForce(0.001, 0.001); // emulate move camera 
        }

        private void lineSelected(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = (ListBox)sender;

            if (openFileDialog1.FileName.EndsWith(".nc"))
            {
                moves = gcodeParser.build_3D_Model(lb.SelectedIndex);
            }

            foreach (LinesVisual3D m in moves)
            {
                viewport.Children.Remove(m);
                viewport.Children.Add(m);
            } 
            
            viewport.CameraController.AddRotateForce(0.001, 0.001); // emulate move camera 
        }



        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {        
            openFileDialog1.Filter = "gcode files (*.nc; *.gcode)|*.nc;*.gcode|All files (*.*)|*.*";
            Nullable<bool> result = openFileDialog1.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                _load_file(openFileDialog1.FileName);
            }
        }

        private void Menu_Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }
    }
}
