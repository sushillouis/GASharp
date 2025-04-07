using System;
using System.Threading;
using Unity.Collections;

[Serializable]
public class GA {
    public GAParameters gaParameters;
    public Population parents, children;
    public volatile bool isRunning = true;
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
        parents.Init(gaParameters.evaluator);
        children = new Population(gaParameters);
        children.Init(gaParameters.evaluator);

        parents.Evaluate();
        parents.Statistics();

        parents.Report(0);
        InputHandler.inst.ThreadLog("Finished Initializing GA");

    }

    public void Evolve()
    {
        for(int i = 1; i < gaParameters.numberOfGenerations; i++) {
            GenerationStep(i);
            if(!isRunning) {
                InputHandler.inst.ThreadLog("Stopping GA Thread");
                break;
            }
        }
        //parents.evaluator.LocalOpt(parents.bestIndividual);

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
        parents.evaluator.LocalOpt(parents.bestIndividual);

        CVRPPlotMgr.inst.SetBest(parents.bestIndividual);
        GAPlotMgr.inst.SetBest(parents.bestIndividual);
    }

    public void Cleanup()    {
        
        InputHandler.inst.ThreadLog("Cleaning up");
    }


}
