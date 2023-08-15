using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphMgr : MonoBehaviour
{
    public static GraphMgr inst;
    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        avgPoints = new List<Vector3>();
        xAxisLimit = xAxis.GetPosition(1).x;
        yDiffAxis = yAxis.GetPosition(1).y - yAxis.GetPosition(0).y;
        AvgPlotRenderer.startWidth = width;
        AvgPlotRenderer.endWidth = width;
        MaxFitness.startWidth = width;
        MaxFitness.endWidth = width;
    }

    float width = 0.2f;

    // Update is called once per frame
    void Update()
    {
        

    
    }

    public float yDiffAxis = 1;
    public float xAxisLimit = 0;
    public float xLimit = 0;
    public float xInc = 1f;
    public float yLimitMin = 0;
    public float yLimitMax = 0;
    public float yInc = 1f;

    public LineRenderer AvgPlotRenderer;
    public LineRenderer xAxis;
    public LineRenderer yAxis;
    public LineRenderer MaxFitness;

    public Text MinYLabel;
    public Text MaxYLabel;
    public Text MinXLabel;
    public Text MaxXLabel;

    public void SetAxisLimits(float limitX, float limitYMin, float limitYMax)
    {
        xLimit = limitX;
        xInc = xAxisLimit / xLimit;
        yLimitMin = limitYMin;
        yLimitMax = limitYMax;
        float yDiffFitness = yLimitMax - yLimitMin;
        yInc = yDiffAxis / yDiffFitness;


    }

    public List<Vector3> avgPoints;
    public List<Vector3> maxPoints;
    public void AddPoint(float gen, float avg, float max)
    {
        lock(avgPoints) {
            avgPoints.Add(new Vector3(gen, avg, 0));
            if(avg < yLimitMin) yLimitMin = avg;
            if(avg > yLimitMax) yLimitMax = avg;

        }
        lock(maxPoints) {
            maxPoints.Add(new Vector3(gen, max, 0));
            if(max > yLimitMax) yLimitMax = max;
        }
    }
   

    public void PlotGraph()
    {

        MaxXLabel.text = xLimit.ToString();
        yInc = yDiffAxis / (yLimitMax - yLimitMin);
        MinYLabel.text = yLimitMin.ToString();
        MaxYLabel.text = yLimitMax.ToString();
        int count = 0;

        lock(avgPoints) {
            AvgPlotRenderer.positionCount = avgPoints.Count;
            foreach(Vector3 point in avgPoints) {
                AvgPlotRenderer.SetPosition(count++, Recompute(point));
            }
        }

        count = 0;
        lock(maxPoints) {
            MaxFitness.positionCount = maxPoints.Count;
            foreach(Vector3 point in maxPoints) {
                MaxFitness.SetPosition(count++, Recompute(point));
            }
        }
    }

    public Vector3 Recompute(Vector3 point)
    {
        float x = point.x * xInc;
        float y = (point.y - yLimitMin) * yInc;
        return new Vector3 (x, y, 0);
    }
}
