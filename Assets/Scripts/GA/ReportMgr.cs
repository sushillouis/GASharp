using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MultiRunAverage {
    public int gen;
    public float min;
    public float avg;
    public float max;
    public float obj;
    public MultiRunAverage(int gen, float min, float avg, float max, float obj) {
        this.gen = gen;
        this.min = min;
        this.avg = avg;
        this.max = max;
        this.obj = obj;
    }

    public override string ToString() {
        return gen + ", " + min + ", " + avg + ", " + max + ", " + obj;
    }
}

[Serializable]
public class GAPlotData {
    public int gen;
    public float min, max, avg;
    public float objMax;
    public float bestSoFarObj;
    public float bestSoFar;
    public Individual bestIndividual;
    public GAPlotData(int gen, float min, float avg, float max, float objMax, Individual bestIndividual) {

        this.gen = gen;
        this.min = min;
        this.avg = avg;
        this.max = max;
        this.objMax = objMax;
        this.bestSoFarObj = objMax;
        this.bestSoFar = max;
        this.bestIndividual = new Individual(bestIndividual.parameters);
        this.bestIndividual.Init();
        this.bestIndividual.CopyFrom(bestIndividual);
    }
}

[Serializable]
public class ReportData {
    public int run;
    public List<GAPlotData> dataList;
}
public class ReportMgr : MonoBehaviour
{
    public static ReportMgr inst;
    private void Awake() {
        inst = this;
    }

    //public RectTransform MoveDirButtonPanel;
    public Button MoveDirButton;
    public TMP_Text RunNumberText;

    public GAPlotMgr gaPlotMgr;
    public CVRPPlotMgr cvrpPlotMgr;

    public List<GAPlotData> gaPlots = new List<GAPlotData>();
    public List<ReportData> multiRunData = new List<ReportData>();

    public PlotRunHandler plotRunHandler;
    public int currentRunNumber = -1;
    public int maxGenerations = -1;
    public int maxRuns = -1;
    public string BaseDirName = "Reports";
    public string problemName = "Unknown";

    // Start is called before the first frame update
    void Start() {
        gaPlots.Clear();
        MoveDirButton.onClick.AddListener(MoveToProblemNamedDirectory);
        MoveDirButton.gameObject.SetActive(false);
    }

    public void SetToRunNumber(int runNumber) {
        gaPlotMgr.ResetPlotters();
        cvrpPlotMgr.ResetPlotters();
        currentRunNumber = runNumber;
        RunNumberText.text = (runNumber + 1).ToString() + " of " + maxRuns.ToString();
    }

    public void Init(GAParameters gap) {
        gaPlots.Clear();
        gaPlotMgr.Init(gap);
        cvrpPlotMgr.Init(gap);
        multiRunData.Clear();
        for(int i = 0; i < gap.nRuns; i++) {
            ReportData data = new ReportData();
            data.run = i;
            data.dataList = new List<GAPlotData>();
            multiRunData.Add(data);
        }
        currentRunNumber = 0;
        maxRuns = gap.nRuns;
        maxGenerations = gap.numberOfGenerations;
        problemName = gap.problem.cvrpData.problemName;
        BaseDirName = gap.baseDirName;
    }

    public void ReportRun(GAPlotData data) {
        gaPlotMgr.AddStats(data.gen, data.avg, data.max);
        gaPlotMgr.SetBest(data.bestIndividual);
        cvrpPlotMgr.SetBest(data.bestIndividual);

        multiRunData[currentRunNumber].dataList.Add(data);
    }

    public void SetBest(Individual individual) {
        gaPlotMgr.SetBest(individual);
        cvrpPlotMgr.SetBest(individual);
    }

    // Update is called once per frame
    void Update()    {
        if(GUIMgr.inst.State == GAState.GARunning) {
            gaPlotMgr.Plot();
            cvrpPlotMgr.Plot();
            gaPlotMgr.PlotBestChrom();
        }
    }

    //---------------------------------------------------------------------------

    public void MultiRunAverages() {
        List<MultiRunAverage> multiRunAverages = new List<MultiRunAverage>();
        int nRuns = multiRunData.Count;
        float avgSum = 0; float maxSum = 0; float minSum = 0; float objSum = 0;
        GAPlotData gapd;
        for(int i = 0; i < maxGenerations; i++) {
            avgSum = 0; maxSum = 0; minSum = 0; objSum = 0;
            for(int j = 0; j < nRuns; j++) {
                gapd = multiRunData[j].dataList[i];
                avgSum += gapd.avg;
                maxSum += gapd.max;
                minSum += gapd.min;
                objSum += gapd.objMax;
                if(gapd.max > maxMax) {
                    maxMax = gapd.max;
                    bestObj = gapd.objMax;
                    bestRun = j;
                }

            }
            MultiRunAverage multiRunAverage = new MultiRunAverage(i, minSum/nRuns, avgSum/nRuns, maxSum/nRuns, objSum/nRuns);
            multiRunAverages.Add(multiRunAverage);
        }
        ComputeMetrics(multiRunAverages, maxMax);
        PrintAverages(multiRunAverages);
        MoveDirButton.gameObject.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);
    }

    void PrintAverages(List<MultiRunAverage> multiRunAverages) {
        using(StreamWriter sr = new StreamWriter(BaseDirName + "/MultiRunAverages.csv", true)) {
            sr.WriteLine($"Max, {maxMax}, Obj, {bestObj}, Reliability, {reliability}%, HowOften, {howOftenFound}, When, {whenFound}, bestRun, {bestRun}");
            sr.WriteLine("Generation, Min, Avg, Max, Obj");
            for(int i = 0;i < multiRunAverages.Count;i++) 
                sr.WriteLine(multiRunAverages[i]);
        }
    }
    [SerializeField] private float bestObj = 0;
    [SerializeField] private float maxMax = -1;
    [SerializeField] private float reliability = -1;
    [SerializeField] private float whenFound = -1;
    [SerializeField] private float howOftenFound = -1;
    [SerializeField] private float bestRun = -1;

    void ComputeMetrics(List<MultiRunAverage> mra, float maxMax) {
        int nRuns = multiRunData.Count;
        int count = 0;
        float sumWhenFound = 0;
        GAPlotData gapd;
        for(int i = 0; i < nRuns; i++) {
            for(int j = 0; j < maxGenerations; j++) {
                gapd = multiRunData[i].dataList[j];
                if(gapd.max >= maxMax) {
                    count++;
                    sumWhenFound += gapd.gen;
                    break;
                }
            }
        }
        howOftenFound = count;
        reliability = 100.0f * count/(float)nRuns;
        whenFound = sumWhenFound / count;

    }

    void MoveToProblemNamedDirectory() {
        string newDirName = BaseDirName + problemName.Trim();
        if(Directory.Exists(BaseDirName)) {
            if(!Directory.Exists(newDirName)) {
                Directory.Move(BaseDirName, newDirName);
            } else {
                Debug.LogError("Error. " + newDirName + " exists. Cannot create");
            }
        } else {
            Debug.LogError("Error. " + BaseDirName + " does not exist! Cannot move to " + newDirName);
        }
        MoveDirButton.gameObject.SetActive(false);
    }

}
