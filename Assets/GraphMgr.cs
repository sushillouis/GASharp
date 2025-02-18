using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        maxPoints = new List<Vector3>();
        xAxisLimit = xAxis.GetPosition(1).x;
        yDiffAxis = yAxis.GetPosition(1).y - yAxis.GetPosition(0).y;
        AvgFitnessRenderer.startWidth = width;
        AvgFitnessRenderer.endWidth = width;
        MaxFitnessRenderer.startWidth = width;
        MaxFitnessRenderer.endWidth = width;
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

    public LineRenderer AvgFitnessRenderer;
    public LineRenderer xAxis;
    public LineRenderer yAxis;
    public LineRenderer MaxFitnessRenderer;

    public Text MinYLabel;
    public Text MaxYLabel;
    public Text MinXLabel;
    public Text MaxXLabel;
    public Text chromosomeText;
    public string chromosomeString = "";

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

    public void SetBestChromosome(Individual individual) {
        chromosomeString = individual.ToString();
    }

    public void PlotBestChrom() {
        chromosomeText.text = chromosomeString;
    }

    public void PlotGraph()
    {

        MaxXLabel.text = xLimit.ToString();
        yInc = yDiffAxis / (yLimitMax - yLimitMin);
        MinYLabel.text = yLimitMin.ToString();
        MaxYLabel.text = yLimitMax.ToString();
        int count = 0;

        lock(avgPoints) {
            AvgFitnessRenderer.positionCount = avgPoints.Count;
            foreach(Vector3 point in avgPoints) {
                AvgFitnessRenderer.SetPosition(count++, Recompute(point));
            }
        }

        count = 0;
        lock(maxPoints) {
            MaxFitnessRenderer.positionCount = maxPoints.Count;
            foreach(Vector3 point in maxPoints) {
                MaxFitnessRenderer.SetPosition(count++, Recompute(point));
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
