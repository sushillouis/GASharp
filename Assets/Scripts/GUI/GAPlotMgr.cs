using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class GAPlottersData {
    public int run;
    public Plotter avgPlotter;
    public Plotter maxPlotter;

    public string chromosomeString = "";

    public GAPlottersData(int run, Plotter avgPlotter, Plotter maxPlotter) {
        this.run = run;
        this.avgPlotter = avgPlotter;
        this.maxPlotter = maxPlotter;
    }

    public void Activate(bool shouldShow) {
        avgPlotter.gameObject.SetActive(shouldShow);
        maxPlotter.gameObject.SetActive(shouldShow);
    }
}


public class GAPlotMgr : MonoBehaviour{

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


    int maxGen = 0;
    public void Init(GAParameters gaParameters) {
        avgPlotter.InitAxes();
        maxPlotter.InitAxes();
        maxGen = gaParameters.numberOfGenerations;
    }


    float minLimit = float.MaxValue, maxLimit = float.MinValue;
    public void AddStats(int gen, float avg, float max) {
        avgPlotter.AddPoint(new Vector3(gen, 0, avg));
        maxPlotter.AddPoint(new Vector3(gen, 0, max));
        if(avg < minLimit)
            minLimit = avg;
        if(max > maxLimit)
            maxLimit = max;
        if(minLimit/maxLimit < 0.9f)
            minLimit = maxLimit * 0.9f;

        avgPlotter.SetCommonLimits(0, maxGen + 1, minLimit, maxLimit);
        maxPlotter.SetCommonLimits(0, maxGen + 1, minLimit, maxLimit);
    }

    public void ResetPlotters() {
        avgPlotter.ResetPlotter();
        maxPlotter.ResetPlotter();
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
