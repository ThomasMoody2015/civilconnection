﻿// Copyright (c) 2016 Autodesk, Inc. All rights reserved.
// Author: paolo.serra@autodesk.com
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
using Autodesk.AECC.Interop.Land;
using Autodesk.AECC.Interop.Roadway;
using Autodesk.AECC.Interop.UiRoadway;
using Autodesk.AutoCAD.Interop.Common;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace CivilConnection
{
    /// <summary>
    /// Collection of utilities.
    /// </summary>
    [IsVisibleInDynamoLibrary(false)]
    public class Utils
    {
        #region PRIVATE PROPERTIES


        #endregion

        #region PUBLIC PROPERTIES


        #endregion

        #region CONSTRUCTOR


        #endregion

        #region PRIVATE METHODS


        #endregion

        #region PUBLIC METHODS


        /// <summary>
        /// Feets to mm.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static double FeetToMm(double d)
        {
            return d * 304.8;
        }


        /// <summary>
        /// Mms to feet.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static double MmToFeet(double d)
        {
            return d / 304.8;
        }


        /// <summary>
        /// Feets to m.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static double FeetToM(double d)
        {
            return d * 0.3048;
        }


        /// <summary>
        /// ms to feet.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static double MToFeet(double d)
        {
            return d / 0.3048;
        }


        /// <summary>
        /// Degs to RAD.
        /// </summary>
        /// <param name="angle">The angle.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static double DegToRad(double angle)
        {
            return angle / 180 * Math.PI;
        }


        /// <summary>
        /// RADs to deg.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static double RadToDeg(double d)
        {
            return d * 180 / Math.PI;
        }


        /// <summary>
        /// Adds the layer.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="layerName">Name of the layer.</param>
        [IsVisibleInDynamoLibrary(false)]
        public static void AddLayer(AeccRoadwayDocument doc, string layerName)
        {
            Utils.Log(string.Format("Utils.AddLayer {0} started...", layerName));

            AcadDatabase db = doc as AcadDatabase;

            bool found = false;

            foreach (AcadLayer l in db.Layers)
            {
                if (l.Name == layerName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                db.Layers.Add(layerName);
            }

            Utils.Log(string.Format("Utils.AddLayer completed.", ""));
        }


        /// <summary>
        /// Freezes the layers.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="layer">the name of the layer.</param>
        [IsVisibleInDynamoLibrary(false)]
        public static void FreezeLayers(AeccRoadwayDocument doc, string layer)
        {
            Utils.Log(string.Format("Utils.FreezeLayers started...", ""));

            AcadDatabase db = doc as AcadDatabase;
            IList<AcadLayer> dynLayers = db.Layers.Cast<AcadLayer>().Where(l => l.Name.Equals(layer)).ToList();

            foreach (AcadLayer l in dynLayers)
            {
                l.Freeze = true;
            }

            Utils.Log(string.Format("Utils.FreezeLayers completed.", ""));
        }


        /// <summary>
        /// Adds the text.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="text">The text.</param>
        /// <param name="point">The point.</param>
        /// <param name="height">The height.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="cs">The cs.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddText(AeccRoadwayDocument doc, string text, Point point, double height, string layer, CoordinateSystem cs)
        {
            Utils.Log(string.Format("Utils.AddText started...", ""));

            // TODO: different orientation of curves from Dynamo to AutoCAD
            AddLayer(doc, layer);

            AcadDatabase db = doc as AcadDatabase;
            AcadModelSpace ms = db.ModelSpace;
            var vlist = new double[] { point.X, point.Y, point.Z };
            AcadText a = ms.AddText(text, vlist, height);
            a.Layer = layer;

            RotateByVector(doc, a.Handle, cs.XAxis);
            var b = a.Copy();

            Rotate3DByPlane(doc, b.ObjectID, cs.ZXPlane);

            Utils.Log(string.Format("Utils.AddText completed.", ""));

            return text;
        }


        /// <summary>
        /// Adds the arc by arc.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="arc">The arc.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddArcByArc(AeccRoadwayDocument doc, Arc arc, string layer)
        {
            Utils.Log(string.Format("Utils.AddArcByArc started...", ""));

            // TODO: different orientation of curves from Dynamo to AutoCAD
            //string layer = "DYN-Shapes";
            AddLayer(doc, layer);
            double rotation = 0;

            Point center = arc.CenterPoint;
            Plane curvePlane = Plane.ByOriginNormal(center, arc.Normal);
            Vector direction = Vector.ZAxis();

            if (Math.Abs(Math.Abs(arc.Normal.Dot(Vector.ZAxis())) - 1) > 0.0001)
            {
                Plane horizontal = Plane.ByOriginNormal(center, Vector.ZAxis());
                Circle c1 = Circle.ByPlaneRadius(curvePlane);
                Circle c2 = Circle.ByPlaneRadius(horizontal);
                var result = c1.Intersect(c2);
                IList<Point> points = new List<Point>();
                foreach (Geometry g in result)
                {
                    points.Add(g as Point);
                }
                Line intersection = Line.ByBestFitThroughPoints(points);

                direction = intersection.Direction.Normalized();

                rotation = DegToRad(Vector.ZAxis().AngleAboutAxis(arc.Normal, direction));
                horizontal.Dispose();
                c1.Dispose();
                c2.Dispose();
            }

            double radius = arc.Radius;
            double start = arc.StartAngle;
            double end = start + arc.SweepAngle;

            AcadDatabase db = doc as AcadDatabase;
            AcadModelSpace ms = db.ModelSpace;
            var vlist = new double[] { center.X, center.Y, center.Z };
            AcadArc a = ms.AddArc(vlist, radius, DegToRad(start), DegToRad(end));
            a.Layer = layer;

            center = center.Add(direction);

            var p1 = new double[] { center.X, center.Y, center.Z };

            a.Rotate3D(vlist, p1, rotation);

            center.Dispose();
            direction.Dispose();

            curvePlane.Dispose();

            Utils.Log(string.Format("Utils.AddArcByArc completed.", ""));

            return a.Handle;
        }


        /// <summary>
        /// Adds the point to the document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="point">The point.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddPointByPoint(AeccRoadwayDocument doc, Point point, string layer)
        {
            Utils.Log(string.Format("Utils.AddPointByPoint started...", ""));

            AddLayer(doc, layer);

            AcadDatabase db = doc as AcadDatabase;
            AcadModelSpace ms = db.ModelSpace;
            var coordinates = new double[] { point.X, point.Y, point.Z };
            var p = ms.AddPoint(coordinates);
            p.Layer = layer;

            Utils.Log(string.Format("Utils.AddPointByPoint completed.", ""));

            return p.Handle;
        }


        /// <summary>
        /// Adds the land point by point.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="point">The point.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddCivilPointByPoint(AeccRoadwayDocument doc, Point point)
        {
            Utils.Log(string.Format("Utils.AddCivilPointByPoint started...", ""));

            var points = doc.Points;
            var coordinates = new double[] { point.X, point.Y, point.Z };
            var p = points.Add(coordinates);

            Utils.Log(string.Format("Utils.AddCivilPointByPoint completed.", ""));

            return p.Handle;
        }


        /// <summary>
        /// Adds the point group by point.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="points">The points.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddPointGroupByPoint(AeccRoadwayDocument doc, Point[] points, string name)
        {
            Utils.Log(string.Format("Utils.AddPointGroupByPoint started...", ""));

            AeccPointGroups groups = null;
            AeccPointGroup group = null;

            groups = doc.PointGroups;

            if (groups != null)
            {
                if (groups.Count > 0)
                {
                    foreach (AeccPointGroup g in groups)
                    {
                        if (g.Name == name)
                        {
                            group = g;
                            break;
                        }
                    }
                }

                if (group == null)
                {
                    group = groups.Add(name);
                }

                var docPoints = doc.Points;

                IList<string> numbers = new List<string>();

                for (int i = 0; i < points.Length; ++i)
                {
                    var coordinates = new double[] { points[i].X, points[i].Y, points[i].Z };
                    var p = docPoints.Add(coordinates);

                    numbers.Add(p.Number.ToString());
                }

                string formula = group.QueryBuilder.IncludeNumbers;

                if ("" == formula)
                {
                    formula = numbers[0] + "-" + numbers[numbers.Count - 1];
                }
                else
                {
                    formula += ", " + numbers[0] + "-" + numbers[numbers.Count - 1];
                }

                group.QueryBuilder.IncludeNumbers = formula;

                Utils.Log(string.Format("Utils.AddPointGroupByPoint completed.", ""));

                return group.Handle;
            }

            Utils.Log(string.Format("Utils.AddPointGroupByPoint completed.", ""));

            return "";
        }


        /// <summary>
        /// Returns the point groups in Civil 3D as Dynamo point lists.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static Dictionary<string, IList<Point>> GetPointGroups(AeccRoadwayDocument doc)
        {
            Utils.Log(string.Format("Utils.GetPointGroups started...", ""));

            AeccPointGroups groups = null;

            Dictionary<string, IList<Point>> output = new Dictionary<string, IList<Point>>();

            groups = doc.PointGroups;

            if (groups != null)
            {
                Utils.Log("Processing Point Groups...");

                if (groups.Count > 0)
                {
                    foreach (AeccPointGroup g in groups)
                    {
                        Utils.Log(string.Format("Processing Point Group {0}...", g.Name));

                        IList<Point> group = new List<Point>();

                        foreach (int i in g.Points)
                        {
                            AeccPoint p = doc.Points.Item(i - 1);

                            Utils.Log(string.Format("Processing Point {0}...", i));

                            Point pt = Point.ByCoordinates(p.Easting, p.Northing, p.Elevation);

                            Utils.Log(string.Format("{0} acquired.", pt));

                            group.Add(pt);
                        }

                        if (group.Count > 0)
                        {
                            output.Add(g.Name, group);

                            Utils.Log(string.Format("Processing Point Group {0} completed.", g.Name));
                        }
                    }
                }
            }

            Utils.Log(string.Format("Utils.GetPointGroups completed.", ""));

            return output;
        }

        // TODO: investigate why it's not here
        // Retrieving the COM class factory for component with
        // CLSID {E93200C2-0078-4186-8DF9-3D5372B7DC57} failed due to the following error:
        // 8007007e The specified module could not be found

        // FIXIT: USE REFELECTION INSTEAD

        /// <summary>
        /// Adds a TIN surface by points.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="points">The points.</param>
        /// <param name="name">The name.</param>
        /// <param name="layer">The name of the layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddTINSurfaceByPoints(AeccRoadwayDocument doc, Point[] points, string name, string layer)
        {
            Utils.Log(string.Format("Utils.AddTINSurfaceByPoints started...", ""));

            string handle = "eNull";

            try
            {
                AddLayer(doc, layer);

                AeccPointGroup group = doc.HandleToObject(AddPointGroupByPoint(doc, points, name)) as AeccPointGroup;

                AeccSurfaces surfaces = doc.Surfaces;

                Type surfacesType = surfaces.GetType();

                if (surfacesType != null)
                {
                    //AeccTinCreationData data = new AeccTinCreationData()
                    //{
                    //    BaseLayer = layer,
                    //    Description = "Created by Autodesk CivilConnection",
                    //    Layer = layer,
                    //    Name = name,
                    //    Style =  doc.SurfaceStyles.Cast<AeccSurfaceStyle>().First().Name
                    //};

                    dynamic surf = surfacesType.InvokeMember("AddTinSurface",
                        BindingFlags.InvokeMethod,
                        System.Type.DefaultBinder,
                        surfaces,
                        new object[] { new AeccTinCreationData() });

                    handle = "eDebug - AeccTinCreationData";

                    // AeccTinSurface surface = surfaces.AddTinSurface(data);

                    // surf.PointGroups.Add(group);

                    handle = surf.Handle;
                }
            }
            catch (Exception ex)
            {
                Utils.Log(string.Format("ERROR: Utils.AddTINSurfaceByPoints {0}", ex.Message));

                throw ex;
            }

            Utils.Log(string.Format("Utils.AddTINSurfaceByPoints completed.", ""));

            return handle;
        }


        /// <summary>
        /// Adds a polyline by points.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="points">The points.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddPolylineByPoints(AeccRoadwayDocument doc, IList<Point> points, string layer)
        {
            Utils.Log(string.Format("Utils.AddPolylineByPoints started...", ""));

            //string layer = "DYN-Shapes";
            AddLayer(doc, layer);

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            double[] vlist = new double[3 * points.Count];

            for (int i = 0; i < points.Count; ++i)
            {
                vlist[3 * i] = points[i].X;
                vlist[3 * i + 1] = points[i].Y;
                vlist[3 * i + 2] = points[i].Z;
            }

            var pl = ms.Add3DPoly(vlist);
            pl.Layer = layer;
            pl.Closed = true;

            Utils.Log(string.Format("Utils.AddPolylineByPoints completed.", ""));

            return pl.Handle;
        }


        /// <summary>
        /// Adds a circle entity in Civil 3D by circle.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="c">The c.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddCircleByCircle(AeccRoadwayDocument doc, Circle c, string layer)
        {
            Utils.Log(string.Format("Utils.AddCircleByCircle started...", ""));

            //string layer = "DYN-Shapes";
            AddLayer(doc, layer);

            Point center = c.CenterPoint;

            double radius = c.Radius;

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            double[] vlist = new double[] { center.X, center.Y, center.Z };

            var circle = ms.AddCircle(vlist, radius);
            circle.Layer = layer;

            if (Math.Abs(Math.Abs(c.Normal.Dot(Vector.ZAxis())) - 1) > 0.001)
            {
                Rotate3DByCurveNormal(doc, circle.Handle, c);
            }

            Utils.Log(string.Format("Utils.AddCircleByCircle completed.", ""));

            return circle.Handle;
        }


        /// <summary>
        /// Adds a light weigth polyline by points.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="points">The points.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddLWPolylineByPoints(AeccRoadwayDocument doc, IList<Point> points, string layer)
        {
            Utils.Log(string.Format("Utils.AddLWPolylineByPoints started...", ""));

            //string layer = "DYN-Shapes";
            AddLayer(doc, layer);

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            double[] vlist = new double[2 * points.Count];

            for (int i = 0; i < points.Count; ++i)
            {
                vlist[2 * i] = points[i].X;
                vlist[2 * i + 1] = points[i].Y;
            }

            var pl = ms.AddLightWeightPolyline(vlist);
            pl.Layer = layer;

            Utils.Log(string.Format("Utils.AddLWPolylineByPoints completed.", ""));

            return pl.Handle;
        }


        /// <summary>
        /// Adds a light weight polyline by poly curve.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="polycurve">The polycurve.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddLWPolylineByPolyCurve(AeccRoadwayDocument doc, PolyCurve polycurve, string layer)
        {
            Utils.Log(string.Format("Utils.AddLWPolylineByPolyCurve started...", ""));

            var totalTransform = RevitUtils.DocumentTotalTransform();

            polycurve = polycurve.Transform(totalTransform.Inverse()) as PolyCurve;

            IList<Point> points = polycurve.Curves().Select<Curve, Point>(c => c.StartPoint).ToList();

            points.Add(polycurve.EndPoint);

            Utils.Log(string.Format("Utils.AddLWPolylineByPolyCurve completed.", ""));

            totalTransform.Dispose();

            return AddLWPolylineByPoints(doc, points, layer);
        }


        /// <summary>
        /// Rotates in 3D by curve normal.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="handle">The handle.</param>
        /// <param name="dynCurve">The dyn curve.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Rotate3DByCurveNormal(AeccRoadwayDocument doc, string handle, Curve dynCurve)
        {
            Utils.Log(string.Format("Utils.Rotate3DByCurveNormal started...", ""));

            dynamic curve = doc.HandleToObject(handle);

            CoordinateSystem cs = dynCurve.ContextCoordinateSystem;

            double[,] transform = new double[4, 4];

            transform[0, 0] = cs.XAxis.X / cs.XScaleFactor;
            transform[0, 1] = cs.YAxis.X / cs.XScaleFactor;
            transform[0, 2] = cs.ZAxis.X / cs.XScaleFactor;
            transform[0, 3] = cs.Origin.X;

            transform[1, 0] = cs.XAxis.Y / cs.YScaleFactor;
            transform[1, 1] = cs.YAxis.Y / cs.YScaleFactor;
            transform[1, 2] = cs.ZAxis.Y / cs.YScaleFactor;
            transform[1, 3] = cs.Origin.Y;

            transform[2, 0] = cs.XAxis.Z / cs.ZScaleFactor;
            transform[2, 1] = cs.YAxis.Z / cs.ZScaleFactor;
            transform[2, 2] = cs.ZAxis.Z / cs.ZScaleFactor;
            transform[2, 3] = cs.Origin.Z / cs.ZScaleFactor;

            transform[3, 0] = 0;
            transform[3, 1] = 0;
            transform[3, 2] = 0;
            transform[3, 3] = 1;

            curve.TransformBy(transform);

            Utils.Log(string.Format("Utils.Rotate3DByCurveNormal completed.", ""));

            return handle;
        }


        /// <summary>
        /// Rotates the by vector.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="handle">The handle.</param>
        /// <param name="vector">The vector.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string RotateByVector(AeccRoadwayDocument doc, string handle, Vector vector)
        {
            Utils.Log(string.Format("Utils.RotateByVector started...", ""));

            Point p1 = null;
            Vector v = null;

            dynamic curve = doc.HandleToObject(handle);

            v = Vector.ByCoordinates(vector.X, vector.Y, 0);
            p1 = Point.ByCoordinates(curve.InsertionPoint[0], curve.InsertionPoint[1], curve.InsertionPoint[2]);

            double[] a1 = new double[] { p1.X, p1.Y, p1.Z };

            double rotation = Vector.XAxis().AngleAboutAxis(v, Vector.ZAxis());

            curve.Rotate(a1, DegToRad(rotation));

            p1.Dispose();
            v.Dispose();

            Utils.Log(string.Format("Utils.RotateByVector completed.", ""));

            return handle;
        }


        /// <summary>
        /// Rotate3s the d by plane.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="handle">The handle.</param>
        /// <param name="plane">The plane.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string Rotate3DByPlane(AeccRoadwayDocument doc, string handle, Plane plane)
        {
            Utils.Log(string.Format("Utils.Rotate3DByPlane started...", ""));

            Point p1 = null;
            Point p2 = null;
            Vector v = null;

            dynamic curve = doc.HandleToObject(handle);

            v = Vector.ByCoordinates(curve.Normal[0], curve.Normal[1], curve.Normal[2]);
            p1 = Point.ByCoordinates(curve.InsertionPoint[0], curve.InsertionPoint[1], curve.InsertionPoint[2]);
            p2 = p1.Translate(plane.Normal.Cross(v)) as Point;

            double[] a1 = new double[] { p1.X, p1.Y, p1.Z };
            double[] a2 = new double[] { p2.X, p2.Y, p2.Z };

            double rotation = Math.Acos(v.Dot(plane.Normal));

            curve.Rotate3D(a1, a2, DegToRad(180) - rotation);

            p1.Dispose();
            p2.Dispose();
            v.Dispose();

            Utils.Log(string.Format("Utils.Rotate3DByPlane completed.", ""));

            return handle;
        }


        /// <summary>
        /// Adds the polyline by curve.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddPolylineByCurve(AeccRoadwayDocument doc, Curve curve, string layer)
        {
            Utils.Log(string.Format("Utils.AddPolylineByCurve started...", ""));

            IList<string> temp = new List<string>();

            //curve = curve.Transform(RevitUtils.DocumentTotalTransform().Inverse()) as Curve;

            if (curve.ToString().Contains("Line"))
            {
                temp.Add(AddPolylineByPoints(doc, new List<Point>() { curve.StartPoint, curve.EndPoint }, layer));
            }
            else if (curve.ToString().Contains("Circle"))
            {
                Circle circle = curve as Circle;

                temp.Add(AddCircleByCircle(doc, circle, layer));

                Rotate3DByCurveNormal(doc, temp.Last(), circle);
            }
            else if (curve.ToString().Contains("Arc"))
            {
                Arc arc = curve as Arc;

                temp.Add(AddArcByArc(doc, arc, layer));

                //Rotate3DByCurveNormal(doc, temp.Last(), arc);
            }
            else if (curve.ToString().Contains("PolyCurve") ||
                curve.ToString().Contains("Rectangle") ||
                curve.ToString().Contains("Polygon"))
            {
                PolyCurve polycurve = curve as PolyCurve;

                //IList<Point> points = new List<Point>();

                //foreach (Curve crv in polycurve.Curves())
                //{
                //    points.Add(crv.StartPoint);
                //}

                //if (curve.IsClosed)
                //{
                //    points.Add(points.First());
                //}

                //temp.Add(AddPolylineByPoints(doc, points));

                Acad3DPolyline pl = doc.HandleToObject(AddPolylineByPoints(doc, new List<Point>() { polycurve.CurveAtIndex(0).StartPoint, polycurve.CurveAtIndex(0).EndPoint }, layer));

                if (polycurve.IsClosed)
                {
                    for (int i = 1; i < polycurve.NumberOfCurves - 1; ++i)
                    {
                        Point end = polycurve.CurveAtIndex(i).EndPoint;

                        pl.AppendVertex(new double[] { end.X, end.Y, end.Z });
                    }

                    pl.Closed = true;
                }
                else
                {
                    for (int i = 1; i < polycurve.NumberOfCurves; ++i)
                    {
                        Point end = polycurve.CurveAtIndex(i).EndPoint;

                        pl.AppendVertex(new double[] { end.X, end.Y, end.Z });
                    }

                    pl.Closed = false;
                }

                temp.Add(pl.Handle);

            }
            else
            {
                try
                {
                    var geos = curve.Explode();

                    if (geos.Length > 0)
                    {
                        var curves = geos.Cast<Curve>().ToList();
                        temp.Add(AddPolylineByCurves(doc, curves, layer));
                    }
                }
                catch (Exception ex)
                {
                    Utils.Log(string.Format("ERROR: Utils.AddPolylineByCurve {0}", ex.Message));

                    temp.Add(AddPolylineByPoints(doc, new List<Point>() { curve.StartPoint, curve.EndPoint }, layer));
                }
            }

            Utils.Log(string.Format("Utils.AddPolylineByCurve completed.", ""));

            // TODO: handle nurbs curves
            return temp[0];

        }


        /// <summary>
        /// Adds the polyline by curves.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="curves">The curves.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddPolylineByCurves(AeccRoadwayDocument doc, IList<Curve> curves, string layer)
        {
            Utils.Log(string.Format("Utils.AddPolylineByCurves started...", ""));

            IList<Point> points = new List<Point>();

            foreach (Curve crv in curves)
            {
                points.Add(crv.StartPoint);
            }

            if (curves.First().StartPoint.DistanceTo(curves.Last().EndPoint) < 0.001)
            {
                points.Add(points.First());
            }

            Utils.Log(string.Format("Utils.AddPolylineByCurves completed.", ""));

            return AddPolylineByPoints(doc, points, layer);
        }


        /// <summary>
        /// Adds the extruded solid by points.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="points">The points.</param>
        /// <param name="height">The height.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddExtrudedSolidByPoints(AeccRoadwayDocument doc, IList<Point> points, double height, string layer)
        {
            Utils.Log(string.Format("Utils.AddExtrudedSolidByPoints started...", ""));

            //string layer = "DYN-Solids";
            AddLayer(doc, layer);

            Acad3DSolid solid = null;

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            Acad3DPolyline pl = doc.HandleToObject(AddPolylineByPoints(doc, points, layer)) as Acad3DPolyline;

            if (pl.Closed)
            {
                var collection = pl.Explode();

                AcadEntity[] obj = new AcadEntity[collection.Length];

                for (int i = 0; i < collection.Length; ++i)
                {
                    obj[i] = collection[i] as AcadEntity;
                }

                var region = ms.AddRegion(obj)[0];
                region.Layer = layer;

                pl.Delete();

                foreach (var l in obj)
                {
                    l.Delete();
                }

                solid = ms.AddExtrudedSolid(region, height, 0);
                solid.Layer = layer;

                region.Delete();
            }

            Utils.Log(string.Format("Utils.AddExtrudedSolidByPoints completed.", ""));

            return solid.Handle;
        }


        /// <summary>
        /// Adds the region by patch.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddRegionByPatch(AeccRoadwayDocument doc, Curve curve, string layer)
        {
            Utils.Log(string.Format("Utils.AddRegionByPatch started...", ""));

            //string layer = "DYN-Solids";
            AddLayer(doc, layer);

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            IList<Acad3DPolyline> polylines = new List<Acad3DPolyline>();

            string id = AddPolylineByCurve(doc, curve, layer);

            Acad3DPolyline pl = doc.HandleToObject(id) as Acad3DPolyline;

            var collection = pl.Explode();

            AcadEntity[] obj = new AcadEntity[collection.Length];

            for (int i = 0; i < collection.Length; ++i)
            {
                obj[i] = collection[i] as AcadEntity;
            }

            var region = ms.AddRegion(obj)[0];
            region.Layer = layer;

            pl.Delete();

            foreach (var l in obj)
            {
                l.Delete();
            }

            Utils.Log(string.Format("Utils.AddRegionByPatch completed.", ""));

            return region.Handle;
        }


        /// <summary>
        /// Adds the extruded solid by patch.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="height">The height.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddExtrudedSolidByPatch(AeccRoadwayDocument doc, Curve curve, double height, string layer)
        {
            Utils.Log(string.Format("Utils.AddExtrudedSolidByPatch started...", ""));

            //string layer = "DYN-Solids";
            AddLayer(doc, layer);

            Acad3DSolid solid = null;

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            var r = doc.HandleToObject(AddRegionByPatch(doc, curve, layer));
            r.Layer = layer;

            solid = ms.AddExtrudedSolid(r, height, 0);
            solid.Layer = layer;

            r.Delete();

            Utils.Log(string.Format("Utils.AddExtrudedSolidByPatch completed.", ""));

            return solid.Handle;
        }


        /// <summary>
        /// Adds the extruded solid by curves.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="curves">The curves.</param>
        /// <param name="height">The height.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string AddExtrudedSolidByCurves(AeccRoadwayDocument doc, IList<Curve> curves, double height, string layer)
        {
            Utils.Log(string.Format("Utils.AddExtrudedSolidByCurves started...", ""));

            //string layer = "DYN-Solids";
            AddLayer(doc, layer);

            Acad3DSolid solid = null;

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            IList<Acad3DPolyline> polylines = new List<Acad3DPolyline>();

            string id = AddPolylineByCurves(doc, curves, layer);

            if (id != null)
            {
                Acad3DPolyline pl = doc.HandleToObject(id) as Acad3DPolyline;
                if (pl.Closed)
                {
                    polylines.Add(pl);
                }
            }

            foreach (Acad3DPolyline pl in polylines)
            {
                var collection = pl.Explode();

                AcadEntity[] obj = new AcadEntity[collection.Length];

                for (int i = 0; i < collection.Length; ++i)
                {
                    obj[i] = collection[i] as AcadEntity;
                }

                var region = ms.AddRegion(obj)[0];
                region.Layer = layer;

                pl.Delete();

                foreach (var l in obj)
                {
                    l.Delete();
                }

                solid = ms.AddExtrudedSolid(region, height, 0);
                solid.Layer = layer;

                region.Delete();
            }

            Utils.Log(string.Format("Utils.AddExtrudedSolidByCurves completed.", ""));

            return solid.Handle;
        }


        /// <summary>
        /// Cuts the solids by patch.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="closedCurve">The closed curve.</param>
        /// <param name="height">The height.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static bool CutSolidsByPatch(AeccRoadwayDocument doc, Curve closedCurve, double height, string layer)
        {
            Utils.Log(string.Format("Utils.CutSolidsByPatch started...", ""));

            bool result = false;

            IList<Acad3DSolid> cSolids = new List<Acad3DSolid>();

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            foreach (AcadEntity s in ms)
            {
                if (s.EntityName.Contains("Solid"))
                {
                    if (!s.Layer.Equals(layer))
                    {
                        cSolids.Add(s as Acad3DSolid);
                    }
                }
            }

            var solid = doc.HandleToObject(AddExtrudedSolidByPatch(doc, closedCurve, height, layer));

            var operation = AcBooleanType.acSubtraction;

            if (cSolids.Count > 0)
            {
                foreach (Acad3DSolid cs in cSolids)
                {
                    bool interference = false;

                    Acad3DSolid interf = cs.CheckInterference(solid, true, out interference);

                    if (interference)
                    {
                        cs.Boolean(operation, interf);
                        result = true;
                    }
                }
            }

            FreezeLayers(doc, layer);

            Utils.Log(string.Format("Utils.CutSolidsByPatch completed.", ""));

            return result;
        }

        /// <summary>
        /// Cuts the solids by curves.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="closedCurves">The closed curves.</param>
        /// <param name="height">The height.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static bool CutSolidsByCurves(AeccRoadwayDocument doc, IList<Curve> closedCurves, double height, string layer)
        {
            Utils.Log(string.Format("Utils.CutSolidsByCurves started...", ""));

            bool result = false;

            IList<Acad3DSolid> cSolids = new List<Acad3DSolid>();

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            foreach (AcadEntity s in ms)
            {
                if (s.EntityName.Contains("Solid") && !s.Layer.Equals(layer))
                {
                    cSolids.Add((Acad3DSolid)s);
                }
            }

            var solid = doc.HandleToObject(AddExtrudedSolidByCurves(doc, closedCurves, height, layer));

            var operation = AcBooleanType.acSubtraction;

            foreach (Acad3DSolid cs in cSolids)
            {
                bool interference = false;

                Acad3DSolid interf = cs.CheckInterference(solid, true, out interference);

                if (interference)
                {
                    cs.Boolean(operation, interf);
                    result = true;
                }
            }

            FreezeLayers(doc, layer);

            Utils.Log(string.Format("Utils.CutSolidsByCurves completed.", ""));

            return result;
        }


        /// <summary>
        /// Cuts the solids by geometry.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="geometry">The geometry.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static bool CutSolidsByGeometry(AeccRoadwayDocument doc, Geometry[] geometry, string layer)
        {
            Utils.Log(string.Format("Utils.CutSolidsByGeometry started...", ""));

            bool result = false;

            var handles = ImportGeometry(doc, geometry, layer);

            IList<Acad3DSolid> cSolids = new List<Acad3DSolid>();

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            foreach (AcadEntity s in ms)
            {
                if (s.EntityName.Contains("Solid") && s.Layer != layer)
                {
                    cSolids.Add((Acad3DSolid)s);
                }
            }

            var operation = AcBooleanType.acSubtraction;

            foreach (var handle in handles)
            {
                var solid = doc.HandleToObject(handle);

                foreach (Acad3DSolid cs in cSolids)
                {
                    bool interference = false;

                    Acad3DSolid interf = cs.CheckInterference(solid, true, out interference);

                    if (interference)
                    {
                        cs.Boolean(operation, interf);
                        result = true;
                    }
                }
            }

            FreezeLayers(doc, layer);

            Utils.Log(string.Format("Utils.CutSolidsByGeometry completed.", ""));

            return result;
        }


        /// <summary>
        /// Slices the solids by plane.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="plane">The plane..</param>     
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static bool SliceSolidsByPlane(AeccRoadwayDocument doc, Plane plane)
        {
            Utils.Log(string.Format("Utils.SliceSolidsByPlane started...", ""));

            bool result = false;

            IList<Acad3DSolid> cSolids = new List<Acad3DSolid>();

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            foreach (AcadEntity s in ms)
            {
                if (s.EntityName.Contains("Solid"))
                {
                    cSolids.Add((Acad3DSolid)s);
                }
            }

            Point a = plane.Origin;
            Point b = a.Add(plane.XAxis);
            Point c = a.Add(plane.YAxis);

            Acad3DSolid solid = null;

            foreach (Acad3DSolid cs in cSolids)
            {
                try
                {
                    solid = cs.SliceSolid(new double[] { a.X, a.Y, a.Z },
                                  new double[] { b.X, b.Y, b.Z },
                                  new double[] { c.X, c.Y, c.Z },
                                  true); // If set to true keeps both parts of the solid

                    result = true;
                }
                catch (Exception ex)
                {
                    Utils.Log(string.Format("ERROR: Utils.SliceSolidsByPlane {0}", ex.Message));

                    result = false;
                }
            }

            Utils.Log(string.Format("Utils.SliceSolidsByPlane completed.", ""));

            return result;
        }


        /// <summary>
        /// Imports the geometry.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="geometry">The geometry.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IList<string> ImportGeometry(AeccRoadwayDocument doc, Geometry[] geometry, string layer)
        {
            Utils.Log(string.Format("Utils.ImportGeometry started...", ""));

            AddLayer(doc, layer);

            IList<string> currentHandles = new List<string>();
            IList<string> newHandles = new List<string>();

            AcadDatabase db = doc as AcadDatabase;

            AcadModelSpace ms = db.ModelSpace;

            foreach (AcadEntity s in ms)
            {
                if (s.EntityName.Contains("Solid") || s.EntityName.Contains("Surface"))
                {
                    currentHandles.Add(s.Handle);
                }
            }

            IList<Geometry> solids = new List<Geometry>();

            foreach (Geometry g in geometry)
            {
                if (g is Solid)
                {
                    solids.Add(g);
                }

                else if (g is Arc)
                {
                    Arc arc = g as Arc;

                    newHandles.Add(AddArcByArc(doc, arc, layer));
                }
                else if (g is Curve)
                {
                    Curve c = g as Curve;

                    newHandles.Add(AddPolylineByCurve(doc, c, layer));
                }
                else if (g is Point)
                {
                    Point p = g as Point;
                    newHandles.Add(AddPointByPoint(doc, p, layer));
                }
            }

            if (solids.Count > 0)
            {
                var solidsArray = solids.ToArray();

                string path = Path.Combine(Path.GetTempPath(), "CivilConnection.sat");

                Geometry.ExportToSAT(solidsArray, path);

                doc.Import(path, new double[] { 0, 0, 0 }, 1);

                foreach (AcadEntity s in ms)
                {
                    if (s.EntityName.Contains("Solid") || s.EntityName.Contains("Surface"))
                    {
                        if (!currentHandles.Contains(s.Handle))
                        {
                            s.Layer = layer;
                            newHandles.Add(s.Handle);
                        }
                    }
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                //File.Delete(path); 
            }

            Utils.Log(string.Format("Utils.ImportGeometry completed.", ""));

            return newHandles;
        }


        //TODO: this node is not working when the geometry extraction from one of the solids returns a null or raise an exception

        /// <summary>
        /// Imports the element.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="element">The element.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="layer">The layer.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string ImportElement(AeccRoadwayDocument doc, Revit.Elements.Element element, string parameter, string layer)
        {
            Utils.Log(string.Format("Utils.ImportElement started...", ""));

            var totalTransform = RevitUtils.DocumentTotalTransform();

            var totalTransformInverse = totalTransform.Inverse();

            string result = "";
            string handles = "";
            Acad3DSolid existent = null;
            bool update = false;

            try
            {
                handles = Convert.ToString(element.GetParameterValueByName(parameter));

                foreach (var id in handles.Split(new string[] { "," }, StringSplitOptions.None))
                {
                    var temp = doc.HandleToObject(id) as Acad3DSolid;

                    if (temp != null)
                    {
                        update = true;

                        if (null != existent)
                        {
                            existent.Boolean(AcBooleanType.acUnion, temp);
                        }
                        else
                        {
                            existent = temp;
                        }
                    }
                }
            }
            catch
            { }

            try
            {
                IList<Solid> temp = element.Solids.ToList();
                IList<Solid> solids = new List<Solid>();

                Solid s = temp[0] as Solid;
                s = s.Transform(totalTransformInverse) as Solid;

                temp.RemoveAt(0);

                if (temp.Count > 0)
                {
                    foreach (Geometry g in temp)
                    {
                        Solid gs = null;

                        try
                        {
                            gs = g as Solid;
                            gs = gs.Transform(totalTransformInverse) as Solid;
                            s = Solid.ByUnion(new Solid[] { s, gs });
                        }
                        catch
                        {
                            if (null != gs)
                            {
                                solids.Add(gs);
                            }
                            continue;
                        }
                    }
                }

                solids.Add(s);

                IList<string> handlesList = new List<string>();

                //Solid geometry = Solid.ByUnion(solids.ToArray()).Transform(RevitUtils.DocumentTotalTransform().Inverse()) as Solid;

                Acad3DSolid newSolid = null;

                foreach (Solid i in solids)
                {
                    var ids = ImportGeometry(doc, new Geometry[] { i }, layer);

                    if (ids.Count > 0)
                    {
                        handlesList.Add(ids[0]);

                        Acad3DSolid tempSolid = doc.HandleToObject(ids[0]) as Acad3DSolid;

                        if (newSolid != null)
                        {
                            newSolid.Boolean(AcBooleanType.acUnion, tempSolid);
                        }
                        else
                        {
                            newSolid = tempSolid;
                        }
                    }
                }

                if (newSolid != null)
                {
                    result = newSolid.Handle;
                }
                else
                {
                    result = string.Join(",", handlesList);
                }

                s.Dispose();
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            if (!update || existent == null)
            {
                handles = result;
            }
            else
            {
                handles = Convert.ToString(existent.Handle);
            }

            if (update)
            {
                try
                {

                    doc.SendCommand(string.Format("-ReplaceSolid \"{0}\"\n\"{1}\"\n\n", handles, result));
                }
                catch (Exception ex)
                {
                    Utils.Log(string.Format("ERROR: Utils.ImportElement {0}", ex.Message));

                    MessageBox.Show(string.Format("PythonScript Failed\n\n{0}", ex.Message));
                }
            }

            element.SetParameterByName(parameter, handles);

            totalTransform.Dispose();

            totalTransformInverse.Dispose();

            Utils.Log(string.Format("Utils.ImportElement completed.", ""));

            return handles;
        }


        /// <summary>
        /// Dumps the land XML.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static string DumpLandXML(AeccRoadwayDocument doc)
        {
            Utils.Log(string.Format("Utils.DumpLandXML started...", ""));

            string landxml = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(doc.Name) + ".xml");

            if (!File.Exists(landxml))  // 1.1.0
            {
                doc.SendCommand("-aecclandxmlout\n" + landxml + "\n");  // 1.1.0
                SessionVariables.IsLandXMLExported = true;  // 1.1.0
            }
            else if (File.Exists(landxml) && !SessionVariables.IsLandXMLExported)
            {
                File.Delete(landxml);
                doc.SendCommand("-aecclandxmlout\n" + landxml + "\n");  // 1.1.0
                SessionVariables.IsLandXMLExported = true;  // 1.1.0
            }

            // asynchronous task

            while (!File.Exists(landxml))  // 1.1.0
            {
                // HACK: wait until the file is ready
#pragma warning disable CS0219 // The variable 'i' is assigned but its value is never used
                int i = 0;
#pragma warning restore CS0219 // The variable 'i' is assigned but its value is never used
            }

            Utils.Log(string.Format("Utils.DumpLandXML completed.", ""));

            return landxml;
        }


        /// <summary>
        /// Gets the XML document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Error in Loading XML</exception>
        [IsVisibleInDynamoLibrary(false)]
        public static XmlDocument GetXmlDocument(AeccRoadwayDocument doc)
        {
            Utils.Log(string.Format("Utils.GetXmlDocument started...", ""));

            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                string landxml = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(doc.Name) + ".xml");

                if (!File.Exists(landxml))
                {
                    SessionVariables.IsLandXMLExported = false;  // 1.1.0

                    DumpLandXML(doc);  // 1.1.0

                    //  throw new Exception("Export the LandXML corridor data from the Civil 3D document.\nSave the file in the %Temp% folder\nwith the same name of the Civil 3D document.");  // 1.1.0
                }

                xmlDoc.Load(landxml);
            }
            catch (Exception ex)
            {
                var message = string.Format("ERROR: Utils.GetXmlDocument {0} {1}", "Error in Loading XML", ex.Message);

                Utils.Log(message);

                throw new Exception(message);
            }

            Utils.Log(string.Format("Utils.GetXmlDocument completed.", ""));

            return xmlDoc;
        }


        /// <summary>
        /// Gets the XML namespace manager.
        /// </summary>
        /// <param name="xmlDoc">The XML document.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static XmlNamespaceManager GetXmlNamespaceManager(XmlDocument xmlDoc)
        {
            Utils.Log(string.Format("Utils.GetXmlNamespaceManager started...", ""));

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);

            if (xmlDoc.DocumentElement.NamespaceURI == "http://www.landxml.org/schema/LandXML-1.2")
            {
                nsmgr.AddNamespace("lx", "http://www.landxml.org/schema/LandXML-1.2");
            }
            else if (xmlDoc.DocumentElement.NamespaceURI == "http://www.landxml.org/schema/LandXML-1.1")
            {
                nsmgr.AddNamespace("lx", "http://www.landxml.org/schema/LandXML-1.1");
            }
            else
            {
                nsmgr.AddNamespace("lx", "http://www.landxml.org/schema/LandXML-1.0");
            }

            Utils.Log(string.Format("Utils.GetXmlNamespaceManager completed.", ""));

            return nsmgr;
        }


        /// <summary>
        /// Function that writes an entry to the log file
        /// </summary>
        /// <param name="message"></param>
        [IsVisibleInDynamoLibrary(false)]
        public static void Log(string message)
        {
            string path = Path.Combine(Path.GetTempPath(), "CivilConnection_temp.log");

            using (StreamWriter sw = new StreamWriter(path, true))
            {
                sw.WriteLine(string.Format("[{0}] {1}", DateTime.Now, message));
            }
        }

        /// <summary>
        /// Finalizes the Log file.
        /// </summary>
        [IsVisibleInDynamoLibrary(false)]
        public static void InitializeLog()
        {
            string path = Path.Combine(Path.GetTempPath(), "CivilConnection_temp.log");

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Gets the corridor subassemblies shapes from LandXML.
        /// </summary>
        /// <param name="corridor">The corridor.</param>
        /// <param name="dumpXML">If True exports a LandXML in the Temp folder.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IList<IList<IList<IList<IList<Point>>>>> GetCorridorSubAssembliesFromLandXML(Corridor corridor, bool dumpXML = false)
        {
            Utils.Log(string.Format("Utils.GetCorridorSubAssembliesFromLandXML started...", ""));

            IList<IList<IList<IList<IList<Point>>>>> corrPoints = new List<IList<IList<IList<IList<Point>>>>>();

            SessionVariables.IsLandXMLExported = !dumpXML;  // 1.1.0

            AeccRoadwayDocument doc = corridor._document;

            if (dumpXML)
            {
                DumpLandXML(doc);
                Log(string.Format("Create LandXML", ""));
            }

            // XmlDocument xmlDoc = GetXmlDocument(doc, corridor.Name);
            XmlDocument xmlDoc = GetXmlDocument(doc);

            Log(string.Format("Create XML document", ""));

            XmlNamespaceManager nsmgr = GetXmlNamespaceManager(xmlDoc);

            Log(string.Format("LandXML namespace ok", ""));

            CoordinateSystem cs = CoordinateSystem.Identity();

            foreach (Baseline b in corridor.Baselines)
            {
                IList<IList<IList<IList<Point>>>> baseline = new List<IList<IList<IList<Point>>>>();

                Log(string.Format("Processing Baseline {0}...", b.Index));

                foreach (AeccBaselineRegion blr in b._baseline.BaselineRegions)  // 1.1.0
                {
                    double start = blr.StartStation;
                    double end = blr.EndStation;

                    IList<IList<IList<Point>>> baselineRegion = new List<IList<IList<Point>>>();

                    Log(string.Format("Processing Baseline Region {0} - {1}...", start, end));

                    string[] separator = new string[] { " " };

                    string alName = b.Alignment.Name.Replace(' ', '_');   // this replacement happens when exporting to LandXML from Civil 3D

                    Log(string.Format("Processing Alignment {0}...", alName));

                    foreach (XmlNode alignmentXml in xmlDoc.SelectNodes(string.Format("//lx:Alignment[@name = '{0}']", alName), nsmgr))
                    {
                        Log(string.Format("Alignment {0} found!", alName));

                        foreach (XmlNode assembly in alignmentXml.SelectNodes(".//lx:CrossSect", nsmgr))
                        {
                            IList<IList<Point>> assPoints = new List<IList<Point>>();

                            double station = Convert.ToDouble(assembly.Attributes["sta"].Value, System.Globalization.CultureInfo.InvariantCulture);

                            Log(string.Format("Processing Station {0}...", station));

                            if (Math.Abs(station - start) < 0.001)
                            {
                                station = start;
                            }
                            if (Math.Abs(station - end) < 0.001)
                            {
                                station = end;
                            }

                            if (station >= start && station <= end)
                            {
                                cs = b.CoordinateSystemByStation(station);

                                foreach (XmlNode subassembly in assembly.SelectNodes("lx:DesignCrossSectSurf", nsmgr))
                                {
                                    IList<Point> subPoints = new List<Point>();

                                    Log(string.Format("Processing Subassembly {0} points...", subassembly.ChildNodes.Count));

                                    if (subassembly.ChildNodes.Count > 2)  // 20180810 - Changed to skip Links in the processing
                                    {
                                        foreach (XmlNode calcPoint in subassembly.SelectNodes("lx:CrossSectPnt", nsmgr))
                                        {
                                            string[] coords = calcPoint.InnerText.Split(separator, StringSplitOptions.None);

                                            subPoints.Add(Point.ByCoordinates(Convert.ToDouble(coords[0], System.Globalization.CultureInfo.InvariantCulture), 0, Convert.ToDouble(coords[1], System.Globalization.CultureInfo.InvariantCulture)).Transform(cs) as Point);

                                            Log(string.Format("Processing Coordinates...", ""));
                                        }

                                        var temp = Point.PruneDuplicates(subPoints).ToList();

                                        // discard links
                                        if (temp.Count > 2)
                                        {
                                            assPoints.Add(temp);

                                            Log(string.Format("Subassembly Points added!", ""));
                                        }
                                    }
                                }
                            }

                            if (assPoints.Count > 0)
                            {
                                baselineRegion.Add(assPoints);

                                Log(string.Format("Assembly Points added!", ""));
                            }
                        }
                    }

                    // 20180810 - Changed it throws some errors needs investigation
                    // baselineRegion = baselineRegion.OrderBy(p => b.GetArrayStationOffsetElevationByPoint(p[0][0])[0]).ToList();

                    baseline.Add(baselineRegion);

                    Log(string.Format("Region Points added!", ""));
                }

                corrPoints.Add(baseline);

                Log(string.Format("Baseline Points added!", ""));
            }

            cs.Dispose();

            Utils.Log(string.Format("Utils.GetCorridorSubAssembliesFromLandXML completed.", ""));

            return corrPoints;
        }

        /// <summary>
        /// Gets the corridor points by code from land XML.
        /// </summary>
        /// <param name="corridor">The corridor.</param>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IList<IList<IList<IList<IList<Point>>>>> GetCorridorPointsByCodeFromLandXML(Corridor corridor, string code)
        {
            Utils.Log(string.Format("Utils.GetCorridorPointsByCodeFromLandXML started...", ""));

            Log(string.Format("Code: {0}", code));

            IList<IList<IList<IList<IList<Point>>>>> corrPoints = new List<IList<IList<IList<IList<Point>>>>>();

            AeccRoadwayDocument doc = corridor._document;

            // DumpLandXML(doc); Commented on 1.1.0

            Log(string.Format("Create LandXML", ""));

            //XmlDocument xmlDoc = GetXmlDocument(doc, corridor.Name);
            XmlDocument xmlDoc = GetXmlDocument(doc);

            XmlNamespaceManager nsmgr = GetXmlNamespaceManager(xmlDoc);

            CoordinateSystem cs = CoordinateSystem.Identity();

            foreach (Baseline b in corridor.Baselines)
            {
                Log(string.Format("Processing Baseline {0}...", b.Index));

                IList<IList<IList<IList<Point>>>> baseline = new List<IList<IList<IList<Point>>>>();

                foreach (AeccBaselineRegion blr in b._baseline.BaselineRegions)  // 1.1.0
                {
                    double start = blr.StartStation;
                    double end = blr.EndStation;

                    Log(string.Format("Processing Baseline Region {0} - {1}...", start, end));

                    IList<IList<IList<Point>>> baselineRegion = new List<IList<IList<Point>>>();

                    string[] separator = new string[] { " " };

                    string alName = b.Alignment.Name.Replace(' ', '_');   // this replacement happens when exporting to LandXML form Civil 3D

                    foreach (XmlNode alignmentXml in xmlDoc.SelectNodes(string.Format("//lx:Alignment[@name = '{0}']", alName), nsmgr))
                    {
                        Log(string.Format("Processing Alignment {0}...", alName));

                        foreach (XmlNode assembly in alignmentXml.SelectNodes(".//lx:CrossSect", nsmgr))
                        {
                            IList<IList<Point>> assPoints = new List<IList<Point>>();

                            double station = Convert.ToDouble(assembly.Attributes["sta"].Value, System.Globalization.CultureInfo.InvariantCulture);

                            Log(string.Format("Processing Station {0}...", station));

                            if (Math.Abs(station - start) < 0.001)
                            {
                                station = start;
                            }
                            if (Math.Abs(station - end) < 0.001)
                            {
                                station = end;
                            }

                            if (station >= start && station <= end)
                            {
                                cs = b.CoordinateSystemByStation(station);

                                IList<Point> left = new List<Point>();

                                foreach (XmlNode calcPoint in assembly.SelectNodes(string.Format(".//lx:CrossSectPnt[@code = '{0}']", code), nsmgr))
                                {


                                    string[] coords = calcPoint.InnerText.Split(separator, StringSplitOptions.None);

                                    left.Add(Point.ByCoordinates(Convert.ToDouble(coords[0], System.Globalization.CultureInfo.InvariantCulture), 0, Convert.ToDouble(coords[1], System.Globalization.CultureInfo.InvariantCulture)).Transform(cs) as Point);
                                }

                                Log(string.Format("Processed {0} Calculated Points...", Point.PruneDuplicates(left).Length));

                                assPoints.Add(Point.PruneDuplicates(left));
                            }

                            if (assPoints.Count > 0)
                            {
                                baselineRegion.Add(assPoints);
                            }
                        }
                    }

                    try // 1.1.0
                    {
                        baselineRegion = baselineRegion.OrderBy(p => b.GetArrayStationOffsetElevationByPoint(p[0][0])[0]).ToList();
                    }
                    catch (Exception ex)
                    {
                        Log(string.Format("Error occured: {0}", ex.Message));
                    }

                    baseline.Add(baselineRegion);

                    Log(string.Format("Region Points added!", ""));
                }

                corrPoints.Add(baseline);

                Log(string.Format("Baseline Points added!", ""));
            }

            cs.Dispose();

            Utils.Log(string.Format("Utils.GetCorridorPointsByCodeFromLandXML completed.", ""));

            return corrPoints;
        }

        /// <summary>
        /// Gets the feature lines by code from land XML.
        /// </summary>
        /// <param name="corridor">The corridor.</param>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IList<IList<IList<Featureline>>> GetFeatureLinesByCodeFromLandXML(Corridor corridor, string code)
        {
            Utils.Log(string.Format("Utils.GetFeatureLinesByCodeFromLandXML started...", ""));

            AeccRoadwayDocument doc = corridor._document;

            //XmlDocument xmlDoc = GetXmlDocument(doc, corridor.Name);
            XmlDocument xmlDoc = GetXmlDocument(doc);

            XmlNamespaceManager nsmgr = GetXmlNamespaceManager(xmlDoc);

            CoordinateSystem cs = CoordinateSystem.Identity();

            string[] separator = new string[] { " " };

            IList<IList<IList<Featureline>>> corridorFeaturelines = new List<IList<IList<Featureline>>>();

            foreach (Baseline b in corridor.Baselines)
            {
                IList<IList<Featureline>> baselineColl = new List<IList<Featureline>>();

                string alName = b.Alignment.Name.Replace(' ', '_');   // this replacement happens when exporting to LandXML form Civil 3D

                foreach (XmlNode alignmentXml in xmlDoc.SelectNodes(string.Format("//lx:Alignment[@name = '{0}']", alName), nsmgr))
                {
                    foreach (BaselineRegion blr in b.GetBaselineRegions())
                    {
                        IList<Featureline> featurelines = new List<Featureline>();

                        IList<Point> right = new List<Point>();

                        IList<Point> left = new List<Point>();

                        IList<Point> none = new List<Point>();

                        foreach (XmlNode assembly in alignmentXml.SelectNodes(".//lx:CrossSect", nsmgr))
                        {
                            double station = Convert.ToDouble(assembly.Attributes["sta"].Value, System.Globalization.CultureInfo.InvariantCulture);

                            if (Math.Abs(station - blr.Start) < 0.001)
                            {
                                station = blr.Start;
                            }
                            if (Math.Abs(station - blr.End) < 0.001)
                            {
                                station = blr.End;
                            }

                            if (station >= blr.Start && station <= blr.End)
                            {
                                cs = b.CoordinateSystemByStation(station);

                                foreach (XmlNode subassembly in assembly.SelectNodes("lx:DesignCrossSectSurf[@side = 'left' and @closedArea]", nsmgr))
                                {
                                    //if (subassembly.ChildNodes.Count > 2)
                                    //{
                                    foreach (XmlNode calcPoint in subassembly.SelectNodes(string.Format("lx:CrossSectPnt[@code = '{0}']", code), nsmgr))
                                    {
                                        string[] coords = calcPoint.InnerText.Split(separator, StringSplitOptions.None);

                                        left.Add(Point.ByCoordinates(Convert.ToDouble(coords[0], System.Globalization.CultureInfo.InvariantCulture), 0, Convert.ToDouble(coords[1], System.Globalization.CultureInfo.InvariantCulture)).Transform(cs) as Point);
                                        break;
                                    }
                                    //}
                                }

                                foreach (XmlNode subassembly in assembly.SelectNodes("lx:DesignCrossSectSurf[@side = 'right' and @closedArea]", nsmgr))
                                {
                                    //if (subassembly.ChildNodes.Count > 2)
                                    //{
                                    foreach (XmlNode calcPoint in subassembly.SelectNodes(string.Format("lx:CrossSectPnt[@code = '{0}']", code), nsmgr))
                                    {
                                        string[] coords = calcPoint.InnerText.Split(separator, StringSplitOptions.None);

                                        right.Add(Point.ByCoordinates(Convert.ToDouble(coords[0], System.Globalization.CultureInfo.InvariantCulture), 0, Convert.ToDouble(coords[1], System.Globalization.CultureInfo.InvariantCulture)).Transform(cs) as Point);
                                        break;
                                    }
                                    //}
                                }

                                foreach (XmlNode calcPoint in assembly.SelectNodes(string.Format(".//lx:CrossSectPnt[@code = '{0}']", code), nsmgr))
                                {
                                    string[] coords = calcPoint.InnerText.Split(separator, StringSplitOptions.None);

                                    none.Add(Point.ByCoordinates(Convert.ToDouble(coords[0], System.Globalization.CultureInfo.InvariantCulture), 0, Convert.ToDouble(coords[1], System.Globalization.CultureInfo.InvariantCulture)).Transform(cs) as Point);
                                }
                            }
                        }

                        if (left.Count > 1)
                        {
                            left = Point.PruneDuplicates(left);
                            left = left.OrderBy(p => b.GetArrayStationOffsetElevationByPoint(p)[0]).ToList();
                            if (left.Count > 1)
                            {
                                featurelines.Add(new Featureline(b, PolyCurve.ByPoints(left), code, Featureline.SideType.Left));
                            }
                        }

                        if (right.Count > 1)
                        {
                            right = Point.PruneDuplicates(right);
                            right = right.OrderBy(p => b.GetArrayStationOffsetElevationByPoint(p)[0]).ToList();
                            if (right.Count > 1)
                            {
                                featurelines.Add(new Featureline(b, PolyCurve.ByPoints(right), code, Featureline.SideType.Right));
                            }
                        }

                        if (none.Count > 1)
                        {
                            none = Point.PruneDuplicates(none);
                            none = none.OrderBy(p => b.GetArrayStationOffsetElevationByPoint(p)[0]).ToList();
                            var pc = PolyCurve.ByPoints(none);
                            var offset = b.GetArrayStationOffsetElevationByPoint(pc.PointAtParameter(0.5))[1];
                            var side = Featureline.SideType.Right;

                            if (offset < 0)
                            {
                                side = Featureline.SideType.Left;
                            }

                            if (none.Count > 1)
                            {
                                featurelines.Add(new Featureline(b, pc, code, side));
                            }
                        }

                        baselineColl.Add(featurelines);
                    }
                }

                corridorFeaturelines.Add(baselineColl);
            }

            cs.Dispose();

            Utils.Log(string.Format("Utils.GetFeatureLinesByCodeFromLandXML completed.", ""));

            return corridorFeaturelines;
        }

        /// <summary>
        /// Gets the featurelines from land XML.
        /// </summary>
        /// <param name="corridor">The corridor.</param>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static IList<IList<IList<Featureline>>> GetFeaturelinesFromLandXML(Corridor corridor)
        {
            Utils.Log(string.Format("Utils.GetFeaturelinesFromLandXML started...", ""));

            AeccRoadwayDocument doc = corridor._document;

            // DumpLandXML(doc);

            Log(string.Format("Create LandXML", ""));

            //XmlDocument xmlDoc = GetXmlDocument(doc, corridor.Name);
            XmlDocument xmlDoc = GetXmlDocument(doc);

            XmlNamespaceManager nsmgr = GetXmlNamespaceManager(xmlDoc);

            CoordinateSystem cs = CoordinateSystem.Identity();

            string[] separator = new string[] { " " };

            IList<IList<IList<Featureline>>> corridorFeaturelines = new List<IList<IList<Featureline>>>();

            // TODO: what happens to the corridor in the LandXML if you have more than one baseline with different profiles?
            foreach (Baseline b in corridor.Baselines)
            {
                Log(string.Format("Processing Baseline {0}...", b.Index));

                IList<IList<Featureline>> baselineFeaturelines = new List<IList<Featureline>>();

                string alName = b.Alignment.Name.Replace(' ', '_');   // this replacement happens when exporting to LandXML form Civil 3D

                foreach (XmlNode alignmentXml in xmlDoc.SelectNodes(string.Format("//lx:Alignment[@name = '{0}']", alName), nsmgr))
                {
                    Log(string.Format("Processing Alignment {0}...", alName));

                    foreach (AeccBaselineRegion blr in b._baseline.BaselineRegions)  // 1.1.0
                    {
                        double start = blr.StartStation;
                        double end = blr.EndStation;

                        Log(string.Format("Processing Baseline Region {0} - {1}...", start, end));

                        IList<Featureline> featurelines = new List<Featureline>();

                        foreach (var code in corridor.GetCodes())
                        {
                            Log(string.Format("Code: {0}", code));

                            IList<Point> right = new List<Point>();

                            IList<Point> left = new List<Point>();

                            IList<Point> none = new List<Point>();

                            foreach (XmlNode assembly in alignmentXml.SelectNodes(".//lx:CrossSect", nsmgr))
                            {
                                double station = Convert.ToDouble(assembly.Attributes["sta"].Value, System.Globalization.CultureInfo.InvariantCulture);

                                Log(string.Format("Processing Station {0}...", station));

                                if (Math.Abs(station - start) < 0.001)
                                {
                                    station = start;
                                }
                                if (Math.Abs(station - end) < 0.001)
                                {
                                    station = end;
                                }

                                if (station >= start && station <= end)
                                {
                                    cs = b.CoordinateSystemByStation(station);

                                    foreach (XmlNode subassembly in assembly.SelectNodes("lx:DesignCrossSectSurf[@side = 'left' and @closedArea]", nsmgr))
                                    {
                                        Log(string.Format("Processing Subassembly {0} points...", subassembly.ChildNodes.Count));

                                        //if (subassembly.ChildNodes.Count > 2)
                                        //{
                                        foreach (XmlNode calcPoint in subassembly.SelectNodes(string.Format("lx:CrossSectPnt[@code = '{0}']", code), nsmgr))
                                        {
                                            string[] coords = calcPoint.InnerText.Split(separator, StringSplitOptions.None);

                                            left.Add(Point.ByCoordinates(Convert.ToDouble(coords[0], System.Globalization.CultureInfo.InvariantCulture), 0, Convert.ToDouble(coords[1], System.Globalization.CultureInfo.InvariantCulture)).Transform(cs) as Point);

                                            Log(string.Format("Processing Coordinates Left...", ""));
                                        }
                                        //}
                                    }

                                    foreach (XmlNode subassembly in assembly.SelectNodes("lx:DesignCrossSectSurf[@side = 'right' and @closedArea]", nsmgr))
                                    {
                                        //if (subassembly.ChildNodes.Count > 2)
                                        //{
                                        foreach (XmlNode calcPoint in subassembly.SelectNodes(string.Format("lx:CrossSectPnt[@code = '{0}']", code), nsmgr))
                                        {
                                            string[] coords = calcPoint.InnerText.Split(separator, StringSplitOptions.None);

                                            right.Add(Point.ByCoordinates(Convert.ToDouble(coords[0], System.Globalization.CultureInfo.InvariantCulture), 0, Convert.ToDouble(coords[1], System.Globalization.CultureInfo.InvariantCulture)).Transform(cs) as Point);

                                            Log(string.Format("Processing Coordinates Right...", ""));
                                        }
                                        //}
                                    }

                                    foreach (XmlNode calcPoint in assembly.SelectNodes(string.Format(".//lx:CrossSectPnt[@code = '{0}']", code), nsmgr))
                                    {
                                        string[] coords = calcPoint.InnerText.Split(separator, StringSplitOptions.None);

                                        none.Add(Point.ByCoordinates(Convert.ToDouble(coords[0], System.Globalization.CultureInfo.InvariantCulture), 0, Convert.ToDouble(coords[1], System.Globalization.CultureInfo.InvariantCulture)).Transform(cs) as Point);

                                        Log(string.Format("Processing Coordinates Centered...", ""));
                                    }
                                }
                            }

                            if (left.Count > 1)
                            {

                                left = left.OrderBy(p => b.GetArrayStationOffsetElevationByPoint(p)[0]).ToList();
                                if (left.Count > 1)
                                {
                                    featurelines.Add(new Featureline(b, PolyCurve.ByPoints(left), code, Featureline.SideType.Left));

                                    Log(string.Format("Left Featureline Created!", ""));
                                }
                            }

                            if (right.Count > 1)
                            {
                                right = Point.PruneDuplicates(right);
                                right = right.OrderBy(p => b.GetArrayStationOffsetElevationByPoint(p)[0]).ToList();
                                if (right.Count > 1)
                                {
                                    featurelines.Add(new Featureline(b, PolyCurve.ByPoints(right), code, Featureline.SideType.Right));

                                    Log(string.Format("Right Featureline Created!", ""));
                                }
                            }

                            if (none.Count > 1)
                            {
                                none = Point.PruneDuplicates(none);
                                none = none.OrderBy(p => b.GetArrayStationOffsetElevationByPoint(p)[0]).ToList();

                                var pc = PolyCurve.ByPoints(none);
                                var offset = b.GetArrayStationOffsetElevationByPoint(pc.PointAtParameter(0.5))[1];
                                var side = Featureline.SideType.Right;

                                if (offset < 0)
                                {
                                    side = Featureline.SideType.Left;
                                }

                                if (none.Count > 1)
                                {
                                    featurelines.Add(new Featureline(b, pc, code, side));

                                    Log(string.Format("Featureline Created!", ""));
                                }
                            }
                        }

                        baselineFeaturelines.Add(featurelines);

                        Log(string.Format("Region Featurelines added!", ""));
                    }
                }

                corridorFeaturelines.Add(baselineFeaturelines);

                Log(string.Format("Baseline Featurelines added!", ""));
            }

            cs.Dispose();

            Utils.Log(string.Format("Utils.GetFeaturelinesFromLandXML completed.", ""));

            return corridorFeaturelines;
        }

        /// <summary>
        /// Gets the featurelies from the corridor organized by Corridor-Baseline-Code-Region
        /// </summary>
        /// <param name="corridor">The corridor.</param>
        /// <returns></returns>
        public static IList<IList<IList<Featureline>>> GetFeaturelines(Corridor corridor)  // 20190125
        {
            Utils.Log(string.Format("Utils.GetFeaturelines started...", ""));

            IList<IList<IList<Featureline>>> corridorFeaturelines = new List<IList<IList<Featureline>>>();

            IList<string> codes = corridor.GetCodes();

            foreach (Baseline b in corridor.Baselines)
            {
                foreach (string code in codes)
                {
                    corridorFeaturelines.Add(b.GetFeaturelinesByCode(code));
                }
            }

            Utils.Log(string.Format("Utils.GetFeaturelines completed.", ""));

            return corridorFeaturelines;
        }

        // TODO : Create a set of nodes to process directly LandXML files to extract:
        // Surfaces
        // Alignments
        // Corridors
        // Pipe Networks

        #endregion
    }
}