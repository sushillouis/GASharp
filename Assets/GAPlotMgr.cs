using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GAPlotMgr : MonoBehaviour
{
    public static GAPlotMgr inst;
    private void Awake() {
        inst = this;
    }
    public Plotter avgPlotter;
    public Plotter maxPlotter;


    public Text MinZLabel;
    public Text MaxZLabel;
    public Text MinXLabel;
    public Text MaxXLabel;
    public Text chromosomeText;
    public string chromosomeString = "";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init() {
        avgPlotter.InitAxes();
        maxPlotter.InitAxes();
    }

    float minLimit = float.MaxValue, maxLimit = float.MinValue;
    public void AddStats(int gen, float avg, float max) {
        avgPlotter.AddPoint(new Vector3(gen, 0, avg));
        maxPlotter.AddPoint(new Vector3(gen, 0, max));
        if(avg < minLimit)
            minLimit = avg;
        if(max > maxLimit)
            maxLimit = max;
        avgPlotter.SetCommonLimits(0, gen + 1, minLimit, maxLimit);
        maxPlotter.SetCommonLimits(0, gen + 1, minLimit, maxLimit);
    }

    public void Plot() {
        UpdateLimitLabels();
        avgPlotter.PlotPoints();
        maxPlotter.PlotPoints();
    }

    public void UpdateLimitLabels() {
        MinXLabel.text = maxPlotter.xCoordMin.ToString("0");
        MaxXLabel.text = maxPlotter.xCoordMax.ToString("0");

        MinZLabel.text = avgPlotter.zCoordMin.ToString("0.0");
        MaxZLabel.text = maxPlotter.zCoordMax.ToString("0.0");
    }


    public void SetBest(Individual individual) {
        chromosomeString = individual.ToString();
    }

    public void PlotBestChrom() {
        chromosomeText.text = chromosomeString;
    }


}
