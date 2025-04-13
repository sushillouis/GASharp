
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class InputHandler : MonoBehaviour
{
    public static InputHandler inst;
    private void Awake()
    {
        inst = this; 
    }

    // Start is called before the first frame update
    void Start()    {
        GUIMgr.inst.State = GAState.GAInput;
        if(Application.platform == RuntimePlatform.WebGLPlayer) {
            NRunsPanel.gameObject.SetActive(false);
        }

        Debug.Log("Core count: " + SystemInfo.processorCount);
        Debug.Log("Core count: " + System.Environment.ProcessorCount);
        Debug.Log("Graphics device: " + SystemInfo.graphicsDeviceName);
    }

    public void Init(string problemFilenames) {
        string[] filenames = problemFilenames.Split(' ');
        FunctionDropdown.ClearOptions();
        FunctionDropdown.AddOptions(new List<string>(filenames));
    }

    public string problemFilename = "E-n22-k4.vrp";
    public string GetProblemFilename() {
        return problemFilename;
    }
    public void OnFunctionDropdownChanged() {
        Debug.Log(FunctionDropdown.value);
        problemFilename = FunctionDropdown.options[FunctionDropdown.value].text;
    }

    void Update()
    {
        /*
        if(GUIMgr.inst.State == GAState.GARunning) {
            GAPlotMgr.inst.Plot();
            GAPlotMgr.inst.PlotBestChrom();
            CVRPPlotMgr.inst.Plot();
        }
        */
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

    public TMP_InputField NRunsInputField;
    public RectTransform NRunsPanel;

    public void UpdateParameterFromUI(GAParameters parameters) {
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

        parameters.nRuns = int.Parse(NRunsInputField.text);

        Debug.Log("GAParameters: pop: " + parameters.populationSize + ", ngens: " +
            parameters.numberOfGenerations + ", chromlength: " + parameters.bitChromLength + 
            ", pCross: " + parameters.pCross + ", pMut: " + parameters.pMut + ", seed: " + parameters.seed);
    }

    //---------------------------------------------------------------------------------------

    public string LogSemaphore = "1";
    public void ThreadLog(string msg)    {
        if(DebugToggle.isOn) {
            lock(LogSemaphore) {
                Debug.Log("GAThrd---> " + msg);

            }
        }
    }

    //---------------------------------------------------------------------------------------

}

/*
public void OnSubmit() {
    Debug.Log("Submit button clicked");
    UpdateParameterFromUI();
    SetupEvaluation();
    GUIMgr.inst.State = GAState.GARunning;
    //TestCluster();
    StartCoroutine(StartJobOnDataLoaded(0.1f));


}



    public List<Cluster> clusters = new List<Cluster>();
    public void TestCluster() {
        ClusterK clusterK = new ClusterK(parameters.problem.cvrpData.customers);
        clusterK.Cluster(parameters.cvrpData.nVehicles);
        clusters = clusterK.myClusters;

        ClusterPlotMgr.inst.Init(parameters.cvrpData.customers, parameters.cvrpData.nVehicles);
        ClusterPlotMgr.inst.SetClusters(clusterK.myClusters);
    }

*/