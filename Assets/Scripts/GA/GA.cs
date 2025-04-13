using System;
using System.Collections.Generic;
using System.Threading;

public class ThreadSyncer {

    public volatile bool allWorkDone;
    public ManualResetEvent[] workReady;
    public ManualResetEvent[] workDone;

    public ThreadSyncer(int nThreads) {
        allWorkDone = false;

        workReady = new ManualResetEvent[nThreads];
        for(int i = 0; i < nThreads; i++) {
            workReady[i] = new ManualResetEvent(false);
        }

        workDone = new ManualResetEvent[nThreads];
        for(int i = 0; i < nThreads; i++) {
            workDone[i] = new ManualResetEvent(false);
        }
    }
}

[Serializable]
public class GA {
    public GAParameters gaParameters;
    public Population parents, children;
    public volatile bool isRunning = true;
    public ThreadSyncer threadSyncer;

    public List<Individual> evaluatableIndividuals = new List<Individual>();

    public GA(GAParameters gap)    {
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

        CreateThreads(Environment.ProcessorCount); // creates threadSyncer and threads

        parents = new Population(gaParameters, threadSyncer, evaluatableIndividuals);
        parents.Init(gaParameters.problem.evaluator);
        children = new Population(gaParameters, threadSyncer, evaluatableIndividuals);
        children.Init(gaParameters.problem.evaluator);

        parents.Evaluate();
        parents.Statistics();

        parents.Report(0);
        ReportMgr.inst.ReportRun(parents.gaPlotData);

        InputHandler.inst.ThreadLog("Finished Initializing GA");

    }

    public void Evolve()    {
        for(int i = 1; i < gaParameters.numberOfGenerations; i++) {
            GenerationStep(i);
            if(!isRunning) {
                InputHandler.inst.ThreadLog("Stopping GA Thread");
                break;
            }
        }
    }

    public void GenerationStep(int gen) {

        parents.CHCGeneration(children); //produces children
        //parents.CHCWithCataclysms(children, gen);

        if(gen % gaParameters.localOptInterval == 0)
            children.LocalOpt(0, gaParameters.populationSize);

        children.Statistics();
        children.Report(gen);
        ReportMgr.inst.ReportRun(children.gaPlotData);

        Population tmp = parents;
        parents = children;
        children = tmp;
    }

    public void LocalOptBest() {
        parents.evaluator.LocalOpt(parents.bestIndividual);
        ReportMgr.inst.SetBest(parents.bestIndividual);
    }

    public void Cleanup()    {
        threadSyncer.allWorkDone = true;
        WakeAll();
        foreach(Thread thread in threads) {
            thread.Join();
            InputHandler.inst.ThreadLog("Thread " + thread.Name + " joined");
        }
        InputHandler.inst.ThreadLog("Cleaning up");
        isRunning = false;
    }

    //-----------------------------Thread parallel evaluation----------------------------------------
    List<Thread> threads = new List<Thread>();
    void CreateThreads(int nProcs) {
        int nThreads = ComputeNThreads(nProcs, gaParameters.populationSize);

        threadSyncer = new ThreadSyncer(nThreads);

        for(int i = 0; i < nThreads; i++) {
            int threadIndex = i;
            Thread thread = new Thread(() => { EvaluatorThread(threadIndex, nThreads); } );
            thread.Name = "ET_" + threadIndex;
            threads.Add(thread);
            thread.Start();
        }

    }
    
    void WakeAll() {
        for(int i = 0; i < threadSyncer.workReady.Length; i++) {
            threadSyncer.workReady[i].Set();
        }
    }
    int ComputeNThreads(int nProcs, int popSize) {
        return 20;
    }
    void EvaluatorThread(int threadId, int nThreads) {
        int nEvals = gaParameters.populationSize / nThreads;
        int start = threadId * nEvals;
        int end = (threadId + 1) * nEvals;
        InputHandler.inst.ThreadLog("Starting Thread " + threadId + " start: " + start + ", end: " + end);
        ParallelEvaluator parallelEvaluator = new ParallelEvaluator(threadId, start, end, gaParameters, threadSyncer, evaluatableIndividuals);
        parallelEvaluator.Run();

    }

}
