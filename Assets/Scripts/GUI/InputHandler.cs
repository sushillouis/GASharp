
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using System.Threading;
using System;
using System.Collections;


[Serializable]
public class GAParameters
{
    public int populationSize;
    public int bitChromLength;
    public int seqChromLength;
    public int numberOfGenerations;
    public float pCross;
    public float pMut;
    public float pmCat;
    public float pLocalOpt;
    public int seed;

    public CVRP2 evaluator;
    public CVRPData cvrpData;
    public CVRPRepresentationType representationType = CVRPRepresentationType.RouteCityHeuristics;

    public int localOptInterval;
    public int npInterval;
    
    // for debugging
    public bool isDebug;
    public int nBits, nVars;
    public float min, max;
}



public class InputHandler : MonoBehaviour
{
    public static InputHandler inst;
    private void Awake()
    {
        inst = this; 
    }
    private Thread GAThread;

    public CVRPData cvrpData;

    // Start is called before the first frame update
    void Start()
    {
        GUIMgr.inst.State = GAState.GAInput;
        parameters = new GAParameters();
        cvrpData = new CVRPData();
        parameters.cvrpData = cvrpData;
        List<string> problemNameStrings = cvrpData.GetProblemNames();

        FunctionDropdown.ClearOptions();
        FunctionDropdown.AddOptions(problemNameStrings);
        LocalOptButton.onClick.AddListener(OnLocalOpt);

        Debug.Log("Core count: " + SystemInfo.processorCount);
        Debug.Log("Core count: " + System.Environment.ProcessorCount);
        Debug.Log("Graphics device: " + SystemInfo.graphicsDeviceName);
    }


    public void OnFunctionDropdownChanged() {
        Debug.Log(FunctionDropdown.value);
        cvrpData.problemFilename = FunctionDropdown.options[FunctionDropdown.value].text;
    }

    // Update is called once per frame
    void Update()
    {
        if(GUIMgr.inst.State == GAState.GARunning) {
            GAPlotMgr.inst.Plot();
            GAPlotMgr.inst.PlotBestChrom();
            CVRPPlotMgr.inst.Plot();
        }
    }

    [Header("Input handling")]

    public TMP_InputField PopulationSize;
    public TMP_InputField NumberOfGenerations;

    public TMP_InputField nVarsIn;
    public TMP_InputField nBitsIn;
    public TMP_InputField MinIn;
    public TMP_InputField MaxIn;

    public TMP_InputField LocalOptInterval;
    public TMP_InputField NPInterval;
    public TMP_InputField ChromosomeLength;
    public TMP_InputField Px;
    public TMP_InputField Pm;
    public TMP_InputField pmCat;
    public TMP_InputField pmLocalOpt;
    public TMP_InputField Seed;
    public Toggle DebugToggle;
    public TMP_Dropdown FunctionDropdown;

    public Button SubmitButton;
    public Button LocalOptButton;


    public GAParameters parameters;
    public void OnSubmit() {
        GetParamsFromUI();
        SetupEvaluation();
        GUIMgr.inst.State = GAState.GARunning;
        //TestCluster();
        StartCoroutine(StartJobOnDataLoaded(0.1f));


    }

    public void GetParamsFromUI() {
        parameters.populationSize = int.Parse(PopulationSize.text);
        parameters.numberOfGenerations = int.Parse(NumberOfGenerations.text);

        parameters.localOptInterval = int.Parse(LocalOptInterval.text);
        parameters.pLocalOpt = float.Parse(pmLocalOpt.text);

        parameters.npInterval = int.Parse(NPInterval.text);

        parameters.pCross = float.Parse(Px.text);
        parameters.pMut = float.Parse(Pm.text);
        parameters.pmCat = float.Parse(pmCat.text);
        parameters.seed = int.Parse(Seed.text);
        parameters.isDebug = DebugToggle.isOn;

        Debug.Log("GAParameters: pop: " + parameters.populationSize + ", ngens: " +
            parameters.numberOfGenerations + ", chromlength: " + parameters.bitChromLength + ", min: " +
            parameters.min + ", max: " + parameters.max + ", nHeuristicBits: " + parameters.nBits + ", nVars: " +
            parameters.nVars + ", pCross: " +
            parameters.pCross + ", pMut: " + parameters.pMut + ", seed: " + parameters.seed);
    }

    void SetupEvaluation() {
        cvrpData.LoadData(FunctionDropdown.options[FunctionDropdown.value].text.Trim(), parameters.representationType);

        parameters.evaluator = new CVRP2(cvrpData);
    }

    IEnumerator StartJobOnDataLoaded(float checkInterval) {
        while(!cvrpData.isDataLoaded)
            yield return new WaitForSeconds(checkInterval);

        parameters.bitChromLength = parameters.evaluator.GetChromLength();
        Debug.Log("bitChromLength: " + parameters.bitChromLength);
        parameters.seqChromLength = cvrpData.nCustomers;
        Debug.Log("seqChromLength: " + parameters.seqChromLength);

        CVRPPlotMgr.inst.Init(parameters);
        GAPlotMgr.inst.Init();

        HandleRunPlatform();
    }
    //---------------------------------------------------------------------------------------

    public CoGA coga;
    void HandleRunPlatform() {

        if(Application.platform == RuntimePlatform.WebGLPlayer) {
            GACoroutineStarter();//WebGL does not allow threading
        } else {
            StartJob();
        }

    }

    public void GACoroutineStarter() {
        coga = new CoGA(parameters);
        ga = coga;
        coga.RunAsCoroutine(this);
        Debug.Log("CoGA started!");
    }

    void StartJob()    {
        GAThread = new Thread(GAStarter);
        GAThread.Start();
    }
    
    public GA ga;
    public void GAStarter()    {
        ga = new GA(parameters);
        ga.Run();
        Debug.Log("GA done: ");

    }

    private void OnDestroy() {
        if(ga != null) {
            ga.isRunning = false; //stop threads if running
            StopAllCoroutines();  //stop coroutines if running on webgl
            ga.Cleanup();
        }
        if(GAThread != null) GAThread.Join();
    }

    //---------------------------------------------------------------------------------------

    public string LogSemaphore = "1";
    public void ThreadLog(string msg)    {
        if(parameters.isDebug) {
            lock(LogSemaphore) {
                Debug.Log("GAThrd---> " + msg);

            }
        }
    }

    public void OnLocalOpt() {
        ga.LocalOptBest();
        //TestSlicing();
    }

    //---------------------------------------------------------------------------------------

    public void TestSlicing() {
        Individual ind = new Individual(parameters);
        ind.Init();
        //evaluator.TestSlicing(ind);
    }

    public List<Cluster> clusters = new List<Cluster>();
    public void TestCluster() {
        ClusterK clusterK = new ClusterK(parameters.cvrpData.customers);
        clusterK.Cluster(parameters.cvrpData.nVehicles);
        clusters = clusterK.myClusters;

        ClusterPlotMgr.inst.Init(parameters.cvrpData.customers, parameters.cvrpData.nVehicles);
        ClusterPlotMgr.inst.SetClusters(clusterK.myClusters);
    }
}
