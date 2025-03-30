
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
    public int seed;

    public MetaCVRPEvaluator metaCVRPEvaluator;

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
    
    public MetaCVRPEvaluator MetaCVRPEvaluator;

    // Start is called before the first frame update
    void Start()
    {
        GUIMgr.inst.State = GAState.GAInput;
        parameters = new GAParameters();
        MetaCVRPEvaluator = new MetaCVRPEvaluator();
        parameters.metaCVRPEvaluator = MetaCVRPEvaluator;
        List<string> problemNameStrings = MetaCVRPEvaluator.LocalGetAvailableProblems();
        MetaCVRPEvaluator.problemFilename = problemNameStrings[0];

        FunctionDropdown.ClearOptions();
        FunctionDropdown.AddOptions(problemNameStrings);
        LocalOpt.onClick.AddListener(OnLocalOpt);
    }


    public void OnFunctionDropdownChanged() {
        Debug.Log(FunctionDropdown.value);
        MetaCVRPEvaluator.problemFilename = FunctionDropdown.options[FunctionDropdown.value].text;

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

    void SetParams() {

        MetaCVRPEvaluator.problemFilename = FunctionDropdown.options[FunctionDropdown.value].text.Trim();
        MetaCVRPEvaluator.ReadLocal(MetaCVRPEvaluator.problemFilename);
        MetaCVRPEvaluator.Init(); //sets nBits below
        parameters.bitChromLength = MetaCVRPEvaluator.nCustomers * MetaCVRPEvaluator.nBits;
        Debug.Log("bitChromLength: " + parameters.bitChromLength);
        parameters.seqChromLength = MetaCVRPEvaluator.nCustomers;
        Debug.Log("seqChromLength: " + parameters.seqChromLength);

        CVRPPlotMgr.inst.Init(parameters);
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
        //TestSlicing();
    }

    //---------------------------------------------------------------------------------------

    public void TestSlicing() {
        Individual ind = new Individual(parameters);
        ind.Init();
        MetaCVRPEvaluator.TestSlicing(ind);

    }
}
