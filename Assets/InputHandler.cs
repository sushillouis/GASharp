using Unity.Jobs;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Text;
using System;
using UnityEngine.Assertions.Must;


[Serializable]
public enum EvalFunction {
    OneMax = 0,
    X,
    DeJongF1,
    DeJongF2,
    DeJongF3,
    DeJongF4,
}

[Serializable]
public struct GAParameters
{
    public int populationSize;
    public int chromosomeLength;
    public int numberOfGenerations;
    public float pCross;
    public float pMut;
    public int seed;

    public EvalFunction GAFunction;

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

    // Start is called before the first frame update
    void Start()
    {
        GUIMgr.inst.State = GAState.GAInput;
        parameters = new GAParameters();
        List<string> functionNameStrings = new List<string>(Enum.GetNames(typeof(EvalFunction)));
        FunctionDropdown.ClearOptions();
        FunctionDropdown.AddOptions(functionNameStrings);

    }

    public void OnFunctionDropdownChanged() {
        Debug.Log(FunctionDropdown.value);
        parameters.GAFunction = (EvalFunction) FunctionDropdown.value;
        SetFunctionParams(parameters);
    }

    // Update is called once per frame
    void Update()
    {
        if(GUIMgr.inst.State == GAState.GARunning) {
            GraphMgr.inst.PlotGraph();
            GraphMgr.inst.PlotBestChrom();
        }
    }

    public TMP_InputField PopulationSize;
    public TMP_InputField NumberOfGenerations;

    public TMP_InputField nVarsIn;
    public TMP_InputField nBitsIn;
    public TMP_InputField MinIn;
    public TMP_InputField MaxIn;

    public TMP_InputField ChromosomeLength;
    public TMP_InputField Px;
    public TMP_InputField Pm;
    public TMP_InputField Seed;
    public Toggle DebugToggle;
    public TMP_Dropdown FunctionDropdown;

    public Button Submit;


    public GAParameters parameters;
    public void OnSubmit()
    {

        parameters.populationSize = int.Parse(PopulationSize.text);
        parameters.numberOfGenerations = int.Parse(NumberOfGenerations.text);

        parameters.GAFunction = (EvalFunction) FunctionDropdown.value;
        SetFunctionParams(parameters);

        parameters.chromosomeLength = parameters.nBits * parameters.nVars;

        parameters.pCross = float.Parse(Px.text);
        parameters.pMut = float.Parse(Pm.text);
        parameters.seed = int.Parse(Seed.text);
        parameters.isDebug = DebugToggle.isOn;

        Debug.Log("GAParameters: pop: " +  parameters.populationSize + ", ngens: " + 
            parameters.numberOfGenerations + ", chromlength: " + parameters.chromosomeLength + ", min: " +
            parameters.min + ", max: " + parameters.max + ", nBits: " + parameters.nBits + ", nVars: " + 
            parameters.nVars + ", pCross: " + 
            parameters.pCross + ", pMut: " + parameters.pMut + ", seed: " + parameters.seed);
        GUIMgr.inst.State = GAState.GARunning;
        StartJob();
//        GraphMgr.inst.SetAxisLimits(parameters.numberOfGenerations, 0, parameters.chromosomeLength);
    }

    void SetFunctionParams(GAParameters tmp) {
        switch(parameters.GAFunction) {
            case EvalFunction.OneMax:
                parameters.nBits = 50;
                parameters.nVars = 1;
                parameters.min = 0;
                parameters.max = 50;
                break;
            case EvalFunction.X:
                parameters.nBits = 10;
                parameters.nVars = 1;
                parameters.min = 0;
                parameters.max = 1023;
                break;
            case EvalFunction.DeJongF1:
                parameters.nBits = 10;
                parameters.nVars = 5;
                parameters.min = -5.12f;
                parameters.max = 5.11f;
                break;
            case EvalFunction.DeJongF2:
                parameters.nBits = 14;
                parameters.nVars = 2;
                parameters.min = -2.048f;
                parameters.max = 2.047f;
                break;
            case EvalFunction.DeJongF3:
                parameters.nBits = 10;
                parameters.nVars = 5;
                parameters.min = -5.12f;
                parameters.max = 5.11f;
                break;
            case EvalFunction.DeJongF4:
                parameters.nBits = 8;
                parameters.nVars = 30;
                parameters.min = -1.28f;
                parameters.max = 1.27f;
                break;
            default:
                Debug.Log("Unknown function");
                break;
        }
        SetInputFields(parameters);
    }

    void SetInputFields(GAParameters parms) {
        nVarsIn.text = parms.nVars.ToString();
        nBitsIn.text = parms.nBits.ToString();
        MinIn.text = parms.min.ToString(); 
        MaxIn.text = parms.max.ToString();

    }


//---------------------------------------------------------------------------------------

    void StartJob()
    {
        GAThread = new Thread(GAStarter);
        GAThread.Start();
        GUIMgr.inst.State = GAState.GARunning;
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
            Debug.Log("--->GA: " + msg);

        }
    }

}
