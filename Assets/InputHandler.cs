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

public struct GAParameters
{
    public int populationSize;
    public int chromosomeLength;
    public int numberOfGenerations;
    public float pCross;
    public float pMut;
    public int seed;
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

    }

    // Update is called once per frame
    void Update()
    {
        if(GUIMgr.inst.State == GAState.GARunning) {
            GraphMgr.inst.PlotGraph();
        }
    }

    public TMP_InputField PopulationSize;
    public TMP_InputField NumberOfGenerations;
    public TMP_InputField ChromosomeLength;
    public TMP_InputField Px;
    public TMP_InputField Pm;
    public TMP_InputField Seed;

    public Button Submit;


    public GAParameters parameters;
    public void OnSubmit()
    {
        parameters = new GAParameters();
        parameters.populationSize = int.Parse(PopulationSize.text);
        parameters.numberOfGenerations = int.Parse(NumberOfGenerations.text);
        parameters.chromosomeLength = int.Parse(ChromosomeLength.text);
        parameters.pCross = float.Parse(Px.text);
        parameters.pMut = float.Parse(Pm.text);
        parameters.seed = int.Parse(Seed.text);

        Debug.Log("GAParameters: " +  parameters.populationSize + ", " + 
            parameters.numberOfGenerations + ", " + parameters.chromosomeLength + ", " +
            parameters.pCross + ", " + parameters.pMut + ", " + parameters.seed);
        GUIMgr.inst.State = GAState.GARunning;
        StartJob();
//        GraphMgr.inst.SetAxisLimits(parameters.numberOfGenerations, 0, parameters.chromosomeLength);
    }
    //---------------------------------------------------------------------------------------

    void StartJob()
    {
        GAThread = new Thread(GAStarter);
        GAThread.Start();
        GUIMgr.inst.State = GAState.GARunning;
    }
    GA ga;
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
