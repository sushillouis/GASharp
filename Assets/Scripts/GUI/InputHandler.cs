
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using System.Threading;
using System;
using System.Collections;


[Serializable]
public struct GAParameters
{
    public int populationSize;
    public int chromosomeLength;
    public int numberOfGenerations;
    public float pCross;
    public float pMut;
    public int seed;

    public CVRPEvaluator cvrpEvaluator;

    public int localOptInterval;
    
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
    private int GAResult;
    
    public CVRPEvaluator cvrpEvaluator;

    // Start is called before the first frame update
    void Start()
    {
        GUIMgr.inst.State = GAState.GAInput;
        parameters = new GAParameters();
        cvrpEvaluator = new CVRPEvaluator();
        parameters.cvrpEvaluator = cvrpEvaluator;
        List<string> problemNameStrings = cvrpEvaluator.LocalGetAvailableProblems();
        cvrpEvaluator.problemFilename = problemNameStrings[0];

        FunctionDropdown.ClearOptions();
        FunctionDropdown.AddOptions(problemNameStrings);
        LocalOpt.onClick.AddListener(OnLocalOpt);

    }


    public void OnFunctionDropdownChanged() {
        Debug.Log(FunctionDropdown.value);
        cvrpEvaluator.problemFilename = FunctionDropdown.options[FunctionDropdown.value].text;

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
    public TMP_InputField ChromosomeLength;
    public TMP_InputField Px;
    public TMP_InputField Pm;
    public TMP_InputField Seed;
    public Toggle DebugToggle;
    public TMP_Dropdown FunctionDropdown;

    public Button Submit;
    public Button LocalOpt;


    public GAParameters parameters;
    public void OnSubmit() {
        GetParamsFromUI();
        SetParams();
        GUIMgr.inst.State = GAState.GARunning;

        StartCoroutine(StartJobOnDataLoaded(1));


    }

    public void GetParamsFromUI() {
        parameters.populationSize = int.Parse(PopulationSize.text);
        parameters.numberOfGenerations = int.Parse(NumberOfGenerations.text);

        parameters.localOptInterval = int.Parse(LocalOptInterval.text);
        //parameters.chromosomeLength = cvrpEvaluator.nCustomers;

        parameters.pCross = float.Parse(Px.text);
        parameters.pMut = float.Parse(Pm.text);
        parameters.seed = int.Parse(Seed.text);
        parameters.isDebug = DebugToggle.isOn;

        Debug.Log("GAParameters: pop: " + parameters.populationSize + ", ngens: " +
            parameters.numberOfGenerations + ", chromlength: " + parameters.chromosomeLength + ", min: " +
            parameters.min + ", max: " + parameters.max + ", nBits: " + parameters.nBits + ", nVars: " +
            parameters.nVars + ", pCross: " +
            parameters.pCross + ", pMut: " + parameters.pMut + ", seed: " + parameters.seed);
    }

    void SetParams() {

        cvrpEvaluator.problemFilename = FunctionDropdown.options[FunctionDropdown.value].text.Trim();
        cvrpEvaluator.ReadLocal(cvrpEvaluator.problemFilename);
        cvrpEvaluator.Init();
        parameters.chromosomeLength = cvrpEvaluator.nCustomers;
        Debug.Log("chromosomeLength: " + parameters.chromosomeLength);
        CVRPPlotMgr.inst.Init(parameters);
        //CVRPPlotMgr.inst.TestPlot();

        GAPlotMgr.inst.Init();
    }

    IEnumerator StartJobOnDataLoaded(float checkInterval) {

        yield return null;
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

    void StartJob()
    {
        GAThread = new Thread(GAStarter);
        GAThread.Start();
        //GUIMgr.inst.State = GAState.GARunning;
    }
    
    public GA ga;
    public void GAStarter()
    {
        ga = new GA(parameters);
        ga.Run();
        Debug.Log("GA done: ");

    }

    private void OnDestroy()
    {
        if(GAThread != null) GAThread.Join();
    }

    //---------------------------------------------------------------------------------------

    public string LogSemaphore = "1";
    public void ThreadLog(string msg)
    {
        lock(LogSemaphore) {
            Debug.Log("GAThrd---> " + msg);

        }
    }

    public void OnLocalOpt() {
        ga.LocalOptBest();
        TestSlicing();
    }

    //---------------------------------------------------------------------------------------

    public void TestSlicing() {
        Individual ind = new Individual(parameters);
        ind.Init();
        cvrpEvaluator.TestSlicing(ind);

    }
}
