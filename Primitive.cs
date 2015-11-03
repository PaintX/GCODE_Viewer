using System;
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
using System.Text.RegularExpressions;
using HelixToolkit.Wpf;

namespace ExampleBrowser
{
    class Primitive : MainWindow
    {
        public void DrawLine(LinesVisual3D lines, double x_start, double y_start, double z_start, double x_stop, double y_stop, double z_stop)
        {
            lines.Points.Add(new Point3D(x_start, y_start, z_start));
            lines.Points.Add(new Point3D(x_stop, y_stop, z_stop));
        }

        public void DrawLine(LinesVisual3D lines, Point3D start, Point3D end)
        {
            lines.Points.Add(start);
            lines.Points.Add(end);
        }

        public void DrawArc(LinesVisual3D lines, double x_start, double y_start, double z_start,
                     double x_stop, double y_stop, double z_stop,
                     double j_pos, double i_pos,
                     bool absoluteIJKMode, bool clockwise)
        {
            Point3D initial = new Point3D(x_start, y_start, z_start);
            Point3D nextpoint = new Point3D(x_stop, y_stop, z_stop);
            double k = Double.NaN;
            double radius = Double.NaN;

            Point3D center = updateCenterWithCommand(initial, nextpoint, j_pos, i_pos, k, radius, absoluteIJKMode, clockwise);
            List<Point3D> kreispunkte = generatePointsAlongArcBDring(initial, nextpoint, center, clockwise, 0, 15); // Dynamic resolution

            Point3D old_point = new Point3D();
            foreach (Point3D point in kreispunkte)
            {
                if (old_point.X != 0)
                {
                    DrawLine(lines, old_point, point);
                }
                old_point = point;
            }
        }

        static private Point3D updateCenterWithCommand(Point3D initial, Point3D nextPoint,
                        double j, double i, double k, double radius,
                        bool absoluteIJKMode, bool clockwise)
        {
            if (Double.IsNaN(i) && Double.IsNaN(j) && Double.IsNaN(k))
            {
                return convertRToCenter(initial, nextPoint, radius, absoluteIJKMode, clockwise);
            }

            Point3D newPoint = new Point3D();

            if (absoluteIJKMode)
            {
                if (!Double.IsNaN(i))
                {
                    newPoint.X = i;
                }
                if (!Double.IsNaN(j))
                {
                    newPoint.Y = j;
                }
                if (!Double.IsNaN(k))
                {
                    newPoint.Z = k;
                }
            }
            else
            {
                if (!Double.IsNaN(i))
                {
                    newPoint.X = initial.X + i;
                }
                if (!Double.IsNaN(j))
                {
                    newPoint.Y = initial.Y + j;
                }
                if (!Double.IsNaN(k))
                {
                    newPoint.Z = initial.Z + k;
                }
            }

            return newPoint;
        }

        /**
        * Generates the points along an arc including the start and end points.
        */
        private static List<Point3D> generatePointsAlongArcBDring(Point3D p1, Point3D p2, Point3D center, bool isCw, double R, int arcResolution)
        {
            double radius = R;
            double sweep;

            // Calculate radius if necessary.
            if (radius == 0)
            {
                radius = Math.Sqrt(Math.Pow(p1.X - center.X, 2.0) + Math.Pow(p1.Y - center.Y, 2.0));
            }

            // Calculate angles from center.
            double startAngle = getAngle(center, p1);
            double endAngle = getAngle(center, p2);

            // Fix semantics, if the angle ends at 0 it really should end at 360.
            if (endAngle == 0)
            {
                endAngle = Math.PI * 2;
            }

            // Calculate distance along arc.
            if (!isCw && endAngle < startAngle)
            {
                sweep = ((Math.PI * 2 - startAngle) + endAngle);
            }
            else if (isCw && endAngle > startAngle)
            {
                sweep = ((Math.PI * 2 - endAngle) + startAngle);
            }
            else
            {
                sweep = Math.Abs(endAngle - startAngle);
            }

            return generatePointsAlongArcBDring(p1, p2, center, isCw, radius, startAngle, endAngle, sweep, arcResolution);
        }

        /**
         * Generates the points along an arc including the start and end points.
         */
        private static List<Point3D> generatePointsAlongArcBDring(Point3D p1,
                Point3D p2, Point3D center, bool isCw, double radius,
                double startAngle, double endAngle, double sweep, int numPoints)
        {

            Point3D lineEnd = p2;
            List<Point3D> segments = new List<Point3D>();
            double angle;

            double zIncrement = (p2.Z - p1.Z) / numPoints;
            for (int i = 0; i < numPoints; i++)
            {
                if (isCw)
                {
                    angle = (startAngle - i * sweep / numPoints);
                }
                else
                {
                    angle = (startAngle + i * sweep / numPoints);
                }

                if (angle >= Math.PI * 2)
                {
                    angle = angle - Math.PI * 2;
                }

                lineEnd.X = Math.Cos(angle) * radius + center.X;
                lineEnd.Y = Math.Sin(angle) * radius + center.Y;
                lineEnd.Z += zIncrement;

                segments.Add(lineEnd);
            }

            segments.Add(p2);

            return segments;
        }

        /** 
         * Return the angle in radians when going from start to end.
         */
        private static double getAngle(Point3D start, Point3D end)
        {
            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;

            double angle = 0.0;

            if (deltaX != 0)
            { // prevent div by 0
                // it helps to know what quadrant you are in
                if (deltaX > 0 && deltaY >= 0)
                {  // 0 - 90
                    angle = Math.Atan(deltaY / deltaX);
                }
                else if (deltaX < 0 && deltaY >= 0)
                { // 90 to 180
                    angle = Math.PI - Math.Abs(Math.Atan(deltaY / deltaX));
                }
                else if (deltaX < 0 && deltaY < 0)
                { // 180 - 270
                    angle = Math.PI + Math.Abs(Math.Atan(deltaY / deltaX));
                }
                else if (deltaX > 0 && deltaY < 0)
                { // 270 - 360
                    angle = Math.PI * 2 - Math.Abs(Math.Atan(deltaY / deltaX));
                }
            }
            else
            {
                // 90 deg
                if (deltaY > 0)
                {
                    angle = Math.PI / 2.0;
                }
                // 270 deg
                else
                {
                    angle = Math.PI * 3.0 / 2.0;
                }
            }

            return angle;
        }

        // Try to create an arc :)
        private static Point3D convertRToCenter(Point3D start, Point3D end, double radius, bool absoluteIJK, bool clockwise)
        {
            double R = radius;
            Point3D center = new Point3D();

            // This math is copied from GRBL in gcode.c
            double x = end.X - start.X;
            double y = end.Y - start.Y;

            double h_x2_div_d = 4 * R * R - x * x - y * y;
            if (h_x2_div_d < 0) { Console.Write("Error computing arc radius."); }
            h_x2_div_d = (-Math.Sqrt(h_x2_div_d)) / Hypotenuse(x, y);

            if (clockwise == false)
            {
                h_x2_div_d = -h_x2_div_d;
            }

            // Special message from gcoder to software for which radius
            // should be used.
            if (R < 0)
            {
                h_x2_div_d = -h_x2_div_d;
                // TODO: Places that use this need to run ABS on radius.
                radius = -radius;
            }

            double offsetX = 0.5 * (x - (y * h_x2_div_d));
            double offsetY = 0.5 * (y + (x * h_x2_div_d));

            if (!absoluteIJK)
            {
                center.X = start.X + offsetX;
                center.Y = start.Y + offsetY;
            }
            else
            {
                center.X = offsetX;
                center.Y = offsetY;
            }

            return center;
        }

        private static double Hypotenuse(double a, double b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }
    }
}
