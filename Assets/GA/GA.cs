using System;
using System.Diagnostics;
using System.Threading;
using Unity.Collections;

[Serializable]
public class GA {
    public GAParameters gaParameters;
    public Population parents, children;
    public GA(GAParameters gap)
    {
        gaParameters = gap;
        GARandom r = new GARandom(gaParameters.seed);
    }
    public void Run() {
        Init();
        Evolve();
        Cleanup();

    }

    public void Init()
    {
        InputHandler.inst.ThreadLog("Initializing GA");

        parents = new Population(gaParameters);
        parents.Init();
        children = new Population(gaParameters);
        children.Init();

        parents.Evaluate();
        parents.Statistics();
        GraphMgr.inst.SetAxisLimits(gaParameters.numberOfGenerations, parents.avg, parents.max);
        parents.Report(0);
        InputHandler.inst.ThreadLog("Initialed GA");

    }

    public void Evolve()
    {
        for(int i = 1; i < gaParameters.numberOfGenerations; i++) {
            parents.Generation(children);
            //parents.CHCGeneration(children);
            children.Statistics();
            children.Report(i);


            Population tmp = parents;
            parents = children;
            children = tmp;

        }
        //parents.Print();
    }
    
    public void Cleanup()
    {
        InputHandler.inst.ThreadLog("Cleaning up");
    }


}
