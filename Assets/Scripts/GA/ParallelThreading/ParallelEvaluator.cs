using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ParallelEvaluator {

    int start = -1;
    int end = -1;
    int threadId = -1;
    GAParameters gaParameters;
    ThreadSyncer threadSyncer;
    IEvaluator evaluator;
    List<Individual> evaluatableIndividuals;

    public ParallelEvaluator(int tid, int start, int end, GAParameters gaParameters, 
        ThreadSyncer threadSyncer, List<Individual> eis) {
        this.threadId = tid;
        this.start = start;
        this.end = end;
        this.gaParameters = gaParameters;
        this.threadSyncer = threadSyncer;
        this.evaluator = gaParameters.problem.evaluator;
        this.evaluatableIndividuals = eis;
    }

    public void Run() {
        InputHandler.inst.ThreadLog("Running threadId: " + threadId + " start: " + start + ", end: " + end);
        while(!threadSyncer.allWorkDone) {
            WaitForWork();
            if(threadSyncer.allWorkDone)
                break;
            DoWork();
            NotifyWorkCompletion();
        }
        InputHandler.inst.ThreadLog("Thread " + threadId + " done Running");
    }

    public void WaitForWork() {
        //InputHandler.inst.ThreadLog("Thread " + threadId + " waiting for work");
        threadSyncer.workReady[threadId].WaitOne();
        threadSyncer.workReady[threadId].Reset();
        //InputHandler.inst.ThreadLog("Thread " + threadId + " got work");
    }

    public void DoWork() {
        //InputHandler.inst.ThreadLog("Thread " + threadId + " DOING work from " + start + " to " + end);
        //Thread.Sleep(500); // Simulate work
        EvaluateMembers(start, end);
        //InputHandler.inst.ThreadLog("Thread " + threadId + " DONE work from " + start + " to " + end);

    }
    
    public void EvaluateMembers(int start, int end) {
        for(int i = start; i < end; i++) {
            evaluator.Evaluate(evaluatableIndividuals[i]);
            //InputHandler.inst.ThreadLog("Thread " + threadId + " evaluating member " + i);
        }
    }

    public void NotifyWorkCompletion() {
        //InputHandler.inst.ThreadLog("Thread " + threadId + " notifying completion");
        threadSyncer.workDone[threadId].Set();

    }
}
