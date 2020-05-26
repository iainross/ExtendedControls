﻿/*
 * Copyright © 2016 - 2020 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms.DataVisualization.Charting;
using System.Diagnostics;
using System.Configuration;

namespace ExtendedControls.Controls
{
    public partial class ExtAstroPlot : UserControl
    {
        List<List<double[]>> Points = new List<List<double[]>>();
        List<PointF[]> DataPoints = new List<PointF[]>();

        List<List<double[]>> Coords = new List<List<double[]>>();
        List<PointF[]> AxesAnchors = new List<PointF[]>();

        List<List<double[]>> Orbits = new List<List<double[]>>();
        List<PointF[]> OrbitsFrames = new List<PointF[]>();

        private double focalLength = 900;
        private double distance = 6;
        private int smallDotSize = 3;
        private int mediumDotSize = 6;
        private int largeDotSize = 9;
        private double[] cameraPosition = new double[3];

        // Mouse 
        private bool leftMousePressed = false;
        private PointF ptMouseClick;
        private int mouseMovementSens = 150;
        private double mouseWheelSens = 300;

        // Axes Widget
        private bool drawAxesWidget = true;
        private int axesWidgetThickness = 3;
        private int axesWidgetLength = 50;

        // Azymuth is the horizontal direction expressed as the angular distance between the direction of a fixed point (such as the observer's heading) and the direction of the object
        private double lastAzimuth, azimuth = 0.3;
        // Elevation is the angular distance of something (such as a celestial object) above the horizon
        private double lastElevation, elevation = 0.3;

        #region Properties

        [Description("Set the distance at which the camera stands from the plot")]
        public double Distance
        {
            get { return distance; }
            set { distance = (value >= 0.1) ? distance = value : distance; UpdateProjection(); }
        }

        [Description("Focal length of the camera")]
        public new double Focus
        {
            get { return focalLength; }
            set { focalLength = value; UpdateProjection(); }
        }

        [Description("Camera position")]
        public double[] Camera
        {
            get { return cameraPosition; }
            set { cameraPosition = value; UpdateProjection(); }
        }

        [Description("Horizontal direction of the camera expressed as an angular distance")]
        public double Azimuth
        {
            get { return azimuth; }
            set { azimuth = value; UpdateProjection(); }
        }

        [Description("Vertical direction of the camera expressed as an angular distance")]
        public double Elevation
        {
            get { return elevation; }
            set { elevation = value; UpdateProjection(); }
        }

        [Description("Diameter of the smaller dots")]
        public int SmallDotSize
        {
            get { return smallDotSize; }
            set { smallDotSize = value; UpdateProjection(); }
        }

        [Description("Diameter of the smaller dots")]
        public int MediumDotSize
        {
            get { return mediumDotSize; }
            set { mediumDotSize = value; UpdateProjection(); }
        }

        [Description("Diameter of the smaller dots")]
        public int LargeDotSize
        {
            get { return largeDotSize; }
            set { largeDotSize = value; UpdateProjection(); }
        }

        [Description("Toggle the axes widget display")]
        public bool AxesWidget
        {
            get { return drawAxesWidget; }
            set { drawAxesWidget = value; UpdateProjection(); }
        }

        [Description("Set the thickness of each axis in the axes widget")]
        public int AxesThickness
        {
            get { return axesWidgetThickness; }
            set { axesWidgetThickness = value; UpdateProjection(); }
        }

        [Description("Set the length of each axis in the axes widget")]
        public int AxesLength
        {
            get { return axesWidgetLength; }
            set { axesWidgetLength = value; UpdateProjection(); }
        }

        [Description("Set the sensitivity of the mouse movement")]
        public int MouseSensitivity_Movement
        {
            get { return mouseMovementSens; }
            set { mouseMovementSens = value; UpdateProjection(); }
        }

        [Description("Set the sensisitivy of the mouse wheel")]
        public double MouseSensitivity_Wheel
        {
            get { return mouseWheelSens; }
            set { mouseWheelSens = value; UpdateProjection(); }
        }
        #endregion

        public ExtAstroPlot()
        {
            InitializeComponent();
            ScatterPlotHelpers.MouseWheelHandler.Add(this, OnMouseWheel);            
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;    // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        Color[] colors = new Color[] { Color.LightBlue, Color.Aqua,  Color.Yellow, Color.Orange, Color.DarkOrange, Color.White, Color.DarkViolet, Color.Gray, Color.DarkGray};
                
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Pick the background color defined in the designer
            SolidBrush backColor = new SolidBrush(BackColor);
            SolidBrush axisAnchor = new SolidBrush(ForeColor);

            Pen AxisPen = new Pen(new SolidBrush(ForeColor));
            AxisPen.Width = 1;

            // axes center point            
            var center = new PointF(this.Width / 2, this.Height / 2);            
            
            Graphics g = this.CreateGraphics();

            // give some love to the renderint engine
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;            

            g.FillRectangle(backColor, new Rectangle(0, 0, this.Width, this.Height));

            // axes
            if (AxesAnchors != null)
            {
                for (int i = 0; i < AxesAnchors.Count; i++)
                {
                    for (int c = 0; c < AxesAnchors[i].Length; c++)
                    {
                        PointF p = AxesAnchors[i][c];
                        if (c == 0) // x axis
                        {
                            axisAnchor.Color = Color.Red;
                            AxisPen.Color = Color.Red;
                        }
                        if (c == 1) // y axys
                        {
                            axisAnchor.Color = Color.Green;
                            AxisPen.Color = Color.Green;
                        }
                        if (c == 2) // x axis
                        {
                            axisAnchor.Color = Color.Blue;
                            AxisPen.Color = Color.Blue;
                        }
                                                
                        var axisAnchorPoint = new PointF(p.X, p.Y);
                        g.DrawLine(AxisPen, center, axisAnchorPoint);
                    }
                }
            }

            // dots
            if (DataPoints != null)
            {
                for (int i = 0; i < DataPoints.Count; i++)
                {
                    foreach (PointF p in DataPoints[i])
                    {                        
                        g.FillEllipse(new SolidBrush(colors[i % colors.Length]), new RectangleF(p.X, p.Y, SmallDotSize, SmallDotSize));                        
                    }
                }
            }

            // orbits
            if (OrbitsFrames != null)
            {
                for (int i = 0; i < OrbitsFrames.Count; i++)
                {
                    foreach (PointF p in OrbitsFrames[i])
                    {
                        var orreryCenter = new Point(this.Width / 2, this.Height / 2);
                        var bodyOrbitBoundary = new Point((int)(orreryCenter.X + (orreryCenter.X - p.X)), (int)(orreryCenter.Y + (orreryCenter.Y - p.Y)));
                        var bodyOrbitMass = new Size((orreryCenter.X - bodyOrbitBoundary.X), (orreryCenter.Y - bodyOrbitBoundary.Y));

                        var orbitBoundary = new Point(bodyOrbitBoundary.X, bodyOrbitBoundary.Y);
                        var orbitSize = new Size(bodyOrbitMass.Width * 2, bodyOrbitMass.Height * 2);

                        // draw a fake central star, just for fun - REALLY, IT'S JUST TEMPORARY!
                        g.FillEllipse(new SolidBrush(Color.Yellow), new RectangleF(orreryCenter.X - LargeDotSize / 2, orreryCenter.Y - LargeDotSize / 2, LargeDotSize, LargeDotSize));

                        g.FillEllipse(new SolidBrush(colors[i % colors.Length]), new RectangleF(p.X - MediumDotSize / 2, p.Y - MediumDotSize / 2, MediumDotSize, MediumDotSize));
                        
                        //g.FillEllipse(new SolidBrush(colors[i % colors.Length]), new RectangleF(bodyOrbitBoundary.X, bodyOrbitBoundary.Y, 2, 2));                        

                        //g.DrawLine(AxisPen, bodyOrbitBoundary, p);

                        //g.DrawRectangle(AxisPen, new Rectangle(bodyOrbitBoundary, new Size(bodyOrbitMass.Width, bodyOrbitMass.Height)));
                        //g.DrawRectangle(AxisPen, new Rectangle(orreryCenter, new Size(bodyOrbitMass.Width, bodyOrbitMass.Height)));

                        //g.DrawEllipse(AxisPen, new Rectangle(bodyOrbitBoundary, orbitSize));
                        
                    }
                }
            }
        }
        
        public void AddPoint(double x, double y, double z, int series)
        {
            if (Points.Count - 1 < series)
            {
                Points.Add(new List<double[]>());
            }

            Points[series].Add(new double[] { x, y, z });

            foreach (List<double[]> ser in Points)
            {
                if (DataPoints.Count - 1 < series)
                    DataPoints.Add(ScatterPlotHelpers.Projection.ProjectVector(ser, this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation));
                else
                    DataPoints[series] = ScatterPlotHelpers.Projection.ProjectVector(ser, this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation);
            }

            this.Invalidate();
        }

        public void AddPoints(List<double[]> points)
        {
            List<double[]> _tmp = new List<double[]>(points);
            Points.Add(_tmp);
            DataPoints.Add(ScatterPlotHelpers.Projection.ProjectVector(Points[Points.Count - 1], this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation));                       
            UpdateProjection();
        }

        public void AddCoords(double x, double y, double z, int series)
        {
            if (Coords.Count - 1 < series)
            {
                Coords.Add(new List<double[]>());
            }

            Coords[series].Add(new double[] { x, y, z });

            foreach (List<double[]> ser in Coords)
            {
                if (AxesAnchors.Count - 1 < series)
                    AxesAnchors.Add(ScatterPlotHelpers.Projection.ProjectVector(ser, this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation));
                else
                    AxesAnchors[series] = ScatterPlotHelpers.Projection.ProjectVector(ser, this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation);
            }

            this.Invalidate();
        }

        public void AddAnchors(List<double[]> anchors)
        {
            List<double[]> _anchors = new List<double[]>(anchors);
            Coords.Add(_anchors);
            AxesAnchors.Add(ScatterPlotHelpers.Projection.ProjectVector(Coords[Coords.Count - 1], this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation));
            UpdateProjection();
        }

        public void AddEllipse(double x, double y, double z, int series)
        {
            if (Orbits.Count - 1 < series)
            {
                Orbits.Add(new List<double[]>());
            }

            Orbits[series].Add(new double[] { x, y, z });            

            foreach (List<double[]> ser in Points)
            {
                if (OrbitsFrames.Count - 1 < series)
                    OrbitsFrames.Add(ScatterPlotHelpers.Projection.ProjectVector(ser, this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation));
                else
                    OrbitsFrames[series] = ScatterPlotHelpers.Projection.ProjectVector(ser, this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation);
            }

            this.Invalidate();
        }

        public void AddEllipses(List<double[]> points)
        {
            List<double[]> _tmp = new List<double[]>(points);
            Orbits.Add(_tmp);
            OrbitsFrames.Add(ScatterPlotHelpers.Projection.ProjectVector(Orbits[Orbits.Count - 1], this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation));
            UpdateProjection();
        }

        private void UpdateProjection()
        {
            if (DataPoints == null)
                return;
            else
            {
                double x = distance * Math.Cos(elevation) * Math.Cos(azimuth);
                double y = distance * Math.Cos(elevation) * Math.Sin(azimuth);
                double z = distance * Math.Sin(elevation);
                cameraPosition = new double[3] { -y, z, -x };
                for (int i = 0; i < DataPoints.Count; i++)
                    DataPoints[i] = ScatterPlotHelpers.Projection.ProjectVector(Points[i], this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation);                
            }

            if (OrbitsFrames == null)
                return;
            else
            {
                double x = distance * Math.Cos(elevation) * Math.Cos(azimuth);
                double y = distance * Math.Cos(elevation) * Math.Sin(azimuth);
                double z = distance * Math.Sin(elevation);
                cameraPosition = new double[3] { -y, z, -x };
                for (int i = 0; i < OrbitsFrames.Count; i++)
                    OrbitsFrames[i] = ScatterPlotHelpers.Projection.ProjectVector(Orbits[i], this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation);
            }

            if (AxesAnchors == null)
                return;
            else
            {
                if (drawAxesWidget)
                {
                    double x = distance * Math.Cos(elevation) * Math.Cos(azimuth);
                    double y = distance * Math.Cos(elevation) * Math.Sin(azimuth);
                    double z = distance * Math.Sin(elevation);
                    cameraPosition = new double[3] { -y, z, -x };
                    for (int i = 0; i < AxesAnchors.Count; i++)
                    AxesAnchors[i] = ScatterPlotHelpers.Projection.ProjectVector(Coords[i], this.Width, this.Height, focalLength, cameraPosition, azimuth, elevation);                    
                }
            }

            this.Invalidate();            
        }

        public void Clear()
        {
            DataPoints.Clear();
            Points.Clear();            
        }               

        #region Interaction
        private void ExtScatterPlot_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                leftMousePressed = true;
                ptMouseClick = new PointF(e.X, e.Y);
                lastAzimuth = azimuth;
                lastElevation = elevation;                
            }                        
        }               

        private void ExtScatterPlot_MouseMove(object sender, MouseEventArgs e)
        {
            if (leftMousePressed)
            {
                azimuth = lastAzimuth - (ptMouseClick.X - e.X) / 150;
                elevation = lastElevation + (ptMouseClick.Y - e.Y) / 150;
                UpdateProjection();
            }
        }

        private void ExtScatterPlot_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                leftMousePressed = false;            
        }

        private void ExtScatterPlot_SizeChanged(object sender, EventArgs e)
        {
            if (DataPoints != null)
                UpdateProjection();
        }

        private void OnMouseWheel(MouseEventArgs e)
        {
            Distance += -e.Delta / MouseSensitivity_Wheel;
        }

        public void DrawAxes(int length)
        {
            if (drawAxesWidget)
            {
                List<double[]> Coords = new List<double[]>();

                Coords.Add(new double[] { length * 0.5, 0.0, 0.0, 0 });
                Coords.Add(new double[] { 0.0, -(length * 0.5), 0.0, 1 });
                Coords.Add(new double[] { 0.0, 0.0, length * 0.5, 2 });

                // draw the anchors points
                AddAnchors(Coords);

                Coords.Clear();
            }
        }
        #endregion
    }
}