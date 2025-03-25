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

    public void Init()    {
        InputHandler.inst.ThreadLog("Initializing GA");

        parents = new Population(gaParameters);
        parents.Init(gaParameters.cvrpEvaluator);
        children = new Population(gaParameters);
        children.Init(gaParameters.cvrpEvaluator);

        parents.Evaluate();
        parents.Statistics();

        parents.Report(0);
        InputHandler.inst.ThreadLog("Finished Initializing GA");

    }

    public void Evolve()
    {
        for(int i = 1; i < gaParameters.numberOfGenerations; i++) {
            GenerationStep(i);
        }
        //parents.Print();


    }

    public void GenerationStep(int gen) {
        //parents.Generation(children);
        parents.CHCGeneration(children);
        if(gen % gaParameters.localOptInterval == 0)
            children.LocalOpt(0, gaParameters.populationSize);
        children.Statistics();
        children.Report(gen);


        Population tmp = parents;
        parents = children;
        children = tmp;
    }

    public void LocalOptBest() {
        parents.evaluator.LinK3CVRP(parents.bestIndividual);
        CVRPPlotMgr.inst.SetBest(parents.bestIndividual);
        GAPlotMgr.inst.SetBest(parents.bestIndividual);
    }

    public void Cleanup()    {
        
        InputHandler.inst.ThreadLog("Cleaning up");
    }


}
