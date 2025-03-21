using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GAPlotMgrOld : MonoBehaviour
{
    public static GAPlotMgrOld inst;
    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        avgPoints = new List<Vector3>();
        maxPoints = new List<Vector3>();
        xAxisLimit = Axes.GetPosition(2).x;
        yDiffAxis = Axes.GetPosition(1).z - Axes.GetPosition(0).z;
        AvgFitnessRenderer.startWidth = width;
        AvgFitnessRenderer.endWidth = width;
        MaxFitnessRenderer.startWidth = width;
        MaxFitnessRenderer.endWidth = width;
    }

    float width = 1f;

    // Update is called once per frame
    void Update()
    {
        

    
    }

    public float yDiffAxis = 1;
    public float xAxisLimit = 0;
    public float xLimit = 0;
    public float xInc = 1f;
    public float zLimitMin = 0;
    public float zLimitMax = 0;
    public float zInc = 1f;

    public LineRenderer AvgFitnessRenderer;
    public LineRenderer Axes;
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
        zLimitMin = limitYMin;
        zLimitMax = limitYMax;
        float yDiffFitness = zLimitMax - zLimitMin;
        zInc = yDiffAxis / yDiffFitness;


    }
    public Vector3 offset = new Vector3(0, 1, 0);
    public List<Vector3> avgPoints;
    public List<Vector3> maxPoints;
    public void AddPoint(float gen, float avg, float max)
    {
        lock(avgPoints) {
            avgPoints.Add(new Vector3(gen, 0, avg));
            if(avg < zLimitMin) zLimitMin = avg;
            if(avg > zLimitMax) zLimitMax = avg;

        }
        lock(maxPoints) {
            maxPoints.Add(new Vector3(gen, 0, max));
            if(max > zLimitMax) zLimitMax = max;
        }
    }

    public void SetBestChromosome(Individual individual) {
        //chromosomeString = individual.ToString();
    }

    public void PlotBestChrom() {
        chromosomeText.text = chromosomeString;
    }

    public void PlotGraph()
    {

        MaxXLabel.text = xLimit.ToString();
        zInc = yDiffAxis / (zLimitMax - zLimitMin);
        MinYLabel.text = zLimitMin.ToString();
        MaxYLabel.text = zLimitMax.ToString();
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
        float z = (point.z - zLimitMin) * zInc;
        return new Vector3 (x, 0, z) + offset;
    }


}
