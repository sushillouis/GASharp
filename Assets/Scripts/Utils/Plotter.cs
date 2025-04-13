using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public enum PlotType {
    TSP,
    GA,
    Cluster,
}


[Serializable]
public class Plotter : MonoBehaviour {

    public PlotType plotType;
    public Vector3 offset;
    public List<Vector3> points;
    public LineRenderer axes;
    public LineRenderer pointsRenderer;

    public const float Epsilon = 0.000001f;

    public float xMin, zMin, xMax, zMax;
    public float xInc, zInc;
    public float xCoordMin, xCoordMax, zCoordMin, zCoordMax;
    public int spacing;

    void Start() {
        points = new List<Vector3>();
        InitAxes();
    }

    public void InitAxes() {
        xMin = axes.GetPosition(0).x + spacing;
        xMax = axes.GetPosition(2).x - spacing;
        zMin = axes.GetPosition(0).z + spacing;
        zMax = axes.GetPosition(2).z - spacing;
    }

    public void InitLimits(Vector3 point) {
        xCoordMin = xCoordMax = point.x;
        zCoordMin = zCoordMax = point.z;
        xInc = 1;
        zInc = 1;
    }

    public void ResetPlotter() {
        points.Clear();
        pointsRenderer.positionCount = 0;
    }

    public void RecomputeLimits(Vector3 point) {
        if(point.x < xCoordMin)
            xCoordMin = point.x;
        if(point.x > xCoordMax)
            xCoordMax = point.x;
        if(point.z < zCoordMin)
            zCoordMin = point.z;
        if(point.z > zCoordMax)
            zCoordMax = point.z;

        float xDiff = (xCoordMax - xCoordMin);
        if(xDiff < Epsilon)
            xDiff = 1;
        xInc = (xMax - xMin) / xDiff;

        float zDiff = (zCoordMax - zCoordMin);
        if(zDiff < Epsilon)
            zDiff = 1;
        zInc = (zMax - zMin) / zDiff;
    }


    public void SetCommonLimits(float xLow, float xHigh, float zLow, float zHigh) {
        xCoordMin = xLow;
        xCoordMax = xHigh;
        zCoordMin = zLow;
        zCoordMax = zHigh;
    }

    public void AddPoint(Vector3 point) {
        lock(points) {
            if(points.Count > 0) {
                   RecomputeLimits(point);
            } else {
                InitLimits(point);
            } 
            points.Add(point);
        }
    }

    public void SetPoints(List<Vector3> inPoints) {
        lock(points) {
            points.Clear();
            InitLimits(inPoints[0]);
            foreach(Vector3 point in inPoints) {
                points.Add(point);
                RecomputeLimits(point);
            }
        }
    }

    public void ShareLimits(List<Plotter> plotters) {
        List<Vector3> sharedPoints = new List<Vector3>();
        foreach(Plotter plotter in plotters) {
            lock(plotter.points) {

                foreach(Vector3 point in plotter.points) {
                    RecomputeLimits(point);
                    sharedPoints.Add(point);
                }
            }
        }
        
    }

    public void SetRoute(int i, List<Vector3> inRoute, Vector3 depot) {
        if(inRoute.Count > 0) SetPoints(inRoute);
        lock(points) {
            points.Insert(0, depot);
        }
        AddPoint(depot);
    }

    public void PlotPoints() {
        lock(points) {
            if(points.Count > 0) {
                pointsRenderer.positionCount = (plotType == PlotType.GA ? points.Count : points.Count + 1);
                int i = 0;
                foreach(Vector3 point in points) {
                    pointsRenderer.SetPosition(i++, Convert(point));
                }
                if(plotType == PlotType.TSP)
                    pointsRenderer.SetPosition(i, Convert(points[0]));
            }
        }
    }

    public Vector3 Convert(Vector3 point) {
        float x = xMin + ((point.x - xCoordMin) * xInc);
        float z = zMin + ((point.z - zCoordMin) * zInc);
        return new Vector3(x, 0, z) + offset;
    }
//--------------------------------------------------------------------------------------------------------
}


