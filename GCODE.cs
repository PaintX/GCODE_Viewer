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
    class GCODE : MainWindow
    {
        public struct gcodeSource
        {
            public int linenumber;
            public List<string> command;
            public string line;
            public List<string> axiscmds;
            public double? x_pos;
            public double? y_pos;
            public double? z_pos;
            public double? i_pos;
            public double? j_pos;
            public double? speed;
        }

        class gcodeMaxMinData
        {
            public double x_max { get; set; }
            public double x_min { get; set; }
            public double y_max { get; set; }
            public double y_min { get; set; }
            public double z_max { get; set; }
            public double z_min { get; set; }
        }

        private Boolean inch = false;

        List<gcodeSource> gcodeListCommands = new List<gcodeSource>();

        public GCODE(String file)
        {
            gcodeListCommands.Clear();

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

            String line;
            UInt32 i = 0;
            

            //-- chargement des commandes
            while ((line = myFile.ReadLine()) != null)
            {
                gcodeSource gcodeLine = new gcodeSource();
                gcodeLine.linenumber = (int)i;
                gcodeLine.line = line.Trim();

                if (!line.StartsWith("(") && !line.StartsWith(";"))
                    gcodeLine = _parseGcodeLine(gcodeLine);

                gcodeListCommands.Add(gcodeLine);
                i++;
            }

            myFile.Close();
        }



        private gcodeSource _parseGcodeLine(gcodeSource gcodeS)
        {
            String[] tokens = gcodeS.line.Split(new char[] { ' ' });
            gcodeS.command = new List<string>();
            gcodeS.axiscmds = new List<string>();

            gcodeS.command.Clear();
            foreach (String token in tokens)
            {
                if ((token.StartsWith("G") || token.StartsWith("T") || token.StartsWith("M")))
                {
                    gcodeS.command.Add(token);
                }
                else
                {
                    gcodeS.axiscmds.Add(token);

                    if (token.StartsWith("X"))
                    {
                        gcodeS.x_pos = Convert.ToDouble(token.Substring(1).Replace(".", ","));
                    }
                    if (token.StartsWith("Y"))
                    {
                        gcodeS.y_pos = Convert.ToDouble(token.Substring(1).Replace(".", ","));
                    }
                    if (token.StartsWith("Z"))
                    {
                        gcodeS.z_pos = Convert.ToDouble(token.Substring(1).Replace(".", ","));
                    }
                    if (token.StartsWith("I"))
                    {
                        gcodeS.i_pos = Convert.ToDouble(token.Substring(1).Replace(".", ","));
                    }
                    if (token.StartsWith("J"))
                    {
                        gcodeS.j_pos = Convert.ToDouble(token.Substring(1).Replace(".", ","));
                    }
                    if (token.StartsWith("F"))
                    {
                        gcodeS.speed = Convert.ToDouble(token.Substring(1).Replace(".", ","));
                    }
                }
            }
            return gcodeS;
        }

List<LinesVisual3D> moves = new List<LinesVisual3D>();
        LinesVisual3D normalmoves = new LinesVisual3D();
        LinesVisual3D rapidmoves = new LinesVisual3D();

        LinesVisual3D Toolmoves = new LinesVisual3D();

        public List<LinesVisual3D> build_3D_Model(int lineMax)
        {
            Primitive p = new Primitive();        
            moves.Clear();

            if (gcodeListCommands.Count <= 0)
                return moves;

            normalmoves.Points.Clear();
            rapidmoves.Points.Clear();
            Toolmoves.Points.Clear();

            // translate gcode to 3d
            gcodeMaxMinData maxdata = new gcodeMaxMinData();
            gcodeSource old_positions = new gcodeSource();
            old_positions.linenumber = 0;
            old_positions.line = "G0 X0 Y0 Z0 F0 J0 I0";
            old_positions = _parseGcodeLine(old_positions);

            String positionCommand = "";
            foreach (gcodeSource positions in gcodeListCommands)
            {
                if (positions.command != null)
                {
                    foreach (String pcmd in positions.command)
                    {
                        if (pcmd.StartsWith("G20"))
                            inch = true;
                        if (pcmd.StartsWith("G21"))
                            inch = false;

                        positionCommand = pcmd;
                    }
                }

                if (positionCommand != "G0" && positionCommand != "G1" && positionCommand != "G2" && positionCommand != "G3")
                    continue;

                double factor = (inch ? 25.4 : 1); // mm or inch

                double x_pos_old = Convert.ToDouble(old_positions.x_pos);
                double y_pos_old = Convert.ToDouble(old_positions.y_pos);
                double z_pos_old = Convert.ToDouble(old_positions.z_pos);
                double x_pos = Convert.ToDouble((positions.x_pos.HasValue ? positions.x_pos * factor : old_positions.x_pos));
                double y_pos = Convert.ToDouble((positions.y_pos.HasValue ? positions.y_pos * factor : old_positions.y_pos));
                double z_pos = Convert.ToDouble((positions.z_pos.HasValue ? positions.z_pos * factor : old_positions.z_pos));
                double i_pos = Convert.ToDouble((positions.i_pos.HasValue ? positions.i_pos * factor : Double.NaN));
                double j_pos = Convert.ToDouble((positions.j_pos.HasValue ? positions.j_pos * factor : Double.NaN));

                // Save in maxmin data
                if (x_pos > maxdata.x_max)
                    maxdata.x_max = x_pos;
                if (x_pos < maxdata.x_min)
                    maxdata.x_min = x_pos;
                if (y_pos > maxdata.y_max)
                    maxdata.y_max = y_pos;
                if (y_pos < maxdata.y_min)
                    maxdata.y_min = y_pos;
                if (z_pos > maxdata.z_max)
                    maxdata.z_max = z_pos;
                if (z_pos < maxdata.z_min)
                    maxdata.z_min = z_pos;

                if (positionCommand == "G0")
                {
                    // Draw rapidmove as blue line
                    if (positions.linenumber < lineMax)
                        p.DrawLine(Toolmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                    else
                        p.DrawLine(rapidmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);         
                }
                if (positionCommand == "G1")
                {
                    if (positions.linenumber < lineMax)
                        p.DrawLine(Toolmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                    else
                        p.DrawLine(normalmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                    
                }
                if (positionCommand == "G2" || positionCommand == "G3") // G2 or G3 > draw an arc
                {
                    bool clockwise = false;
                    if (positionCommand == "G2")
                        clockwise = true;

                    p.DrawArc(normalmoves,x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos, j_pos, i_pos, false, clockwise);

                    if (positions.linenumber < lineMax)
                        p.DrawLine(Toolmoves, x_pos_old, y_pos_old, z_pos_old, x_pos, y_pos, z_pos);
                }
                
                if (positions.x_pos.HasValue == true)
                    old_positions.x_pos = x_pos;
                if (positions.y_pos.HasValue == true)
                    old_positions.y_pos = y_pos;
                if (positions.z_pos.HasValue == true)
                    old_positions.z_pos = z_pos;
            }

            rapidmoves.Thickness = 1;
            rapidmoves.Color = Colors.Blue;
            normalmoves.Thickness = 1;
            normalmoves.Color = Colors.Black;
            Toolmoves.Thickness = 1;
            Toolmoves.Color = Colors.Red;

            moves.Add(rapidmoves);
            moves.Add(normalmoves);
            moves.Add(Toolmoves);
            return moves;
        }
    }
}
