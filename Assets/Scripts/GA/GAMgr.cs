using System;
using System.Collections;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class CVRPProblem {
    public IEvaluator evaluator;
    public CVRPData cvrpData;
    public string problemFilenames = "E-n22-k4.vrp E-n30-k3.vrp F-n45-k4.vrp F-n72-k4.vrp F-n135-k7.vrp M-n101-k10.vrp X-n101-k25.vrp X-n167-k10.vrp X-n573-k30.vrp";
}

public interface IEvaluator {
    public void Initialize(Individual ind);
    public int GetChromLength();
    float Evaluate(Individual ind);
    float LocalOpt(Individual ind);
    void Decode(Individual ind);
}


[Serializable]
public class GAParameters {
    public int populationSize;
    public int bitChromLength;
    public int seqChromLength;
    public int numberOfGenerations;
    public float pCross;
    public float pMut;
    public float pmCat;
    public float pLocalOpt;
    public int seed;

    public string reportFilename = "report";
    public string baseDirName = "Reports";

    public int nRuns = 1;

    public CVRPProblem problem;

    public int localOptInterval;
    public int npInterval;

    // for debugging
    public bool isDebug;

    public GAParameters(string bdName) {
        populationSize = 100;
        numberOfGenerations = 200;
        bitChromLength = 10;
        seqChromLength = 10;
        pCross = 0.99f;
        pMut = 0.02f;
        pmCat = 0.1f;
        pLocalOpt = 0.1f;
        seed = 123456;
        reportFilename = bdName + "/report_0";
        baseDirName = bdName;
        nRuns = 1;
        localOptInterval = 10;
        npInterval = 20;
        isDebug = false;
    }

    public GAParameters(GAParameters gap) {
        populationSize = gap.populationSize;
        numberOfGenerations = gap.numberOfGenerations;
        bitChromLength = gap.bitChromLength;
        seqChromLength = gap.seqChromLength;
        pCross = gap.pCross;
        pMut = gap.pMut;
        pmCat = gap.pmCat;
        pLocalOpt = gap.pLocalOpt;
        seed = gap.seed;
        reportFilename = gap.reportFilename;
        baseDirName = gap.baseDirName;
        nRuns = gap.nRuns;
        localOptInterval = gap.localOptInterval;
        npInterval = gap.npInterval;
        isDebug = gap.isDebug;

        problem = gap.problem;

    }
}


public class GAMgr : MonoBehaviour
{
    public static GAMgr inst;
    private void Awake() {
        inst = this;
    }


    public Button RunButton;
    public Button LocalOptButton;

    public GAParameters gaParameters;

    private void Start() {
        RunButton.onClick.AddListener(OnRunButtonClick);
        LocalOptButton.onClick.AddListener(LocalOpt);
        Init();
        InitProblem();
        InputHandler.inst.Init(gaParameters.problem.cvrpData.problemFilenames);
    }

    void LocalOpt() {

    }

    public void OnRunButtonClick() {
        InputHandler.inst.UpdateParameterFromUI(gaParameters);
        SetupProblem(gaParameters);
        GUIMgr.inst.State = GAState.GARunning;
        StartCoroutine(RunAfterDataLoad());
    }

    public void SetupProblem(GAParameters gaParameters) {
        string problemFilename = InputHandler.inst.GetProblemFilename();
        gaParameters.problem.cvrpData.problemFilename = problemFilename;
        gaParameters.problem.cvrpData.LoadData(gaParameters.problem);

        gaParameters.problem.cvrpData.representationType = CVRPRepresentationType.RouteCityHeuristics;
        //Then we can set the evaluator
        gaParameters.problem.evaluator = new CVRP2(gaParameters.problem.cvrpData);
    }

    public void Init() {
        this.gaParameters = new GAParameters(ReportMgr.inst.BaseDirName);
        if(!Directory.Exists(ReportMgr.inst.BaseDirName))
            Directory.CreateDirectory(ReportMgr.inst.BaseDirName);
        else
            DeleteFiles();
    }

    void DeleteFiles() {
        string[] files = Directory.GetFiles(ReportMgr.inst.BaseDirName);
        foreach(string file in files) {
            try {
                File.Delete(file);
            } catch(IOException e) {
                Debug.LogError(e.Message);
            }
        }
    }
    public void InitProblem() {
        gaParameters.problem = new CVRPProblem();
        gaParameters.problem.cvrpData = new CVRPData();
    }

    public void InitPlotters() {
        ReportMgr.inst.Init(gaParameters);
    }

    public IEnumerator RunAfterDataLoad() {
        while(!gaParameters.problem.cvrpData.isDataLoaded)
            yield return new WaitForSeconds(0.1f);

        gaParameters.bitChromLength = gaParameters.problem.evaluator.GetChromLength();
        Debug.Log("bitChromLength: " + gaParameters.bitChromLength);
        gaParameters.seqChromLength = gaParameters.problem.cvrpData.nCustomers;
        Debug.Log("seqChromLength: " + gaParameters.seqChromLength);

        InitPlotters();
        HandleRunPlatform();
    }

    void HandleRunPlatform() {
        if(Application.platform == RuntimePlatform.WebGLPlayer) {
            GACoroutineStarter();
        } else {
            //RunGAThread();
            MultiRun();
        }
        Debug.Log("Running on: " + Application.platform.ToString());
    }

    [SerializeField] CoGA coga;
    public void GACoroutineStarter() {
        coga = new CoGA(gaParameters);
        coga.RunAsCoroutine(this);
        Debug.Log("CoGA started!");
    }

    [SerializeField] private MultiRunner mr;
    public void MultiRun() {
        mr = new MultiRunner(gaParameters);
        mr.InitMRD();
        StartCoroutine(mr.RunSequential(gaParameters));
    }


    public void OnDestroy() {
        mr.DestroyMRSequential();
        StopAllCoroutines();
    }

}


/*
Thread GAThread;
public void RunGAThread() {
    GAThread = new Thread(RunGA);
    GAThread.Start();
}
public GA ga;
public void RunGA() {
    ga = new GA(gaParameters);
    ga.Run();

}
*/