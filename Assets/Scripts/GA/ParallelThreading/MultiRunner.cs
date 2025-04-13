using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[Serializable]
public class ThreadId {
    public Thread thread;
    public int threadId;
    public ThreadId(Thread thread, int threadId) {
        this.thread = thread;
        this.threadId = threadId;
    }
}


[Serializable]
public class GAId {
    public GA ga;
    public int threadId;
    public GAId(GA ga, int threadId) {
        this.ga = ga;
        this.threadId = threadId;
    }
}

[Serializable]
public class MultiRunData {
    public int runNumber;
    public int seed;
    public string reportFilename;

    public MultiRunData(int rn, int inSeed, string bdName) {
        runNumber = rn;
        seed = inSeed;
        reportFilename = bdName + "/report_" + rn.ToString() + ".csv";
    }
}

[Serializable]
public class MultiRunner {

    public MultiRunner(GAParameters gap) {
        this.gaParameters = gap;
    }
    int[] gSeeds =
    { // 36 with help from gemini, 1st seed is our standard seed from single runner
        123456, 7, 987, 6543210, 1, 987654321,
        25, 10203040, 333, 77889900, 8, 54321,
        67890123, 4, 994431100, 159, 753951, 2, 8,
        6420931, 44, 111222333, 934567890, 3, 50000000,
        777, 2248800, 12, 3456789, 7763, 847293,
    };


    public List<MultiRunData> multiRunData = new List<MultiRunData>();
    public GAParameters gaParameters;
    public int actualNRuns = 1;
    public void InitMRD() {
        multiRunData.Clear();
        actualNRuns = Mathf.Min(gaParameters.nRuns, gSeeds.Length);
        for(int i = 0; i < gSeeds.Length; i++) {
            MultiRunData mrd = new MultiRunData(i, gSeeds[i], gaParameters.baseDirName);
            multiRunData.Add(mrd);
        }
    }


    List<ThreadId> threadIds = new List<ThreadId>();
    public void MultiRunParallel(GAMgr gam) {
        InitMRD();
        threadIds.Clear();
        gaList.Clear();
        for(int i = 0; i < actualNRuns; i++) {
            int runNumber = i;
            Thread thread = new Thread(() => {
                RunGA(runNumber);
            });
            thread.Name = "Run_" + runNumber.ToString();
            ThreadId threadId = new ThreadId(thread, runNumber);
            threadIds.Add(threadId);
            thread.Start();
        }
        gam.StartCoroutine(WaitForGAThreads());
    }

    public List<GAId> gaList = new List<GAId>();
    void RunGA(int runNumber) {
        GAParameters newGAParameters = NewGAParameters(gaParameters, runNumber);

        GA ga = new GA(newGAParameters);
        ga.isRunning = true;
        ga.Run();

        GAId gaId = new GAId(ga, runNumber);

        gaList.Add(gaId);
        //Run calls gA.Cleanup() to clean up all other threads when ga finishes evolving
    }
    
    public GAParameters NewGAParameters(GAParameters gap, int runNumber) {
        gaParameters = new GAParameters(gap);
        MultiRunData mrd = multiRunData[runNumber];
        gaParameters.seed = mrd.seed;
        gaParameters.reportFilename = mrd.reportFilename;

        gaParameters.problem = new CVRPProblem();
        gaParameters.problem.cvrpData = gap.problem.cvrpData;
        gaParameters.problem.evaluator = new CVRP2(gaParameters.problem.cvrpData);


        return gaParameters;

    }

    IEnumerator WaitForGAThreads() {
        int count = 0;
        while(count < gaList.Count) {
            foreach(GAId gaid in gaList) {
                if(!gaid.ga.isRunning) {
                    Debug.Log("GA thread " + gaid.ga.gaParameters.reportFilename + " is not running");
                    Thread thread = threadIds.Find(x => x.threadId == gaid.threadId).thread;
                    thread.Join();
                    count++;
                }
            }
            gaList.RemoveAll(x => x.ga.isRunning == false);
            yield return new WaitForSeconds(0.1f);
        }
    }

    //------------------------------------------------------------------------------

    private bool shouldBreak = false;
    public IEnumerator RunSequential(GAParameters gap) {
        for(int i = 0; i < gap.nRuns; i++) {
            ReportMgr.inst.SetToRunNumber(i);

            gap.reportFilename = multiRunData[i].reportFilename;
            gap.seed = multiRunData[i].seed;

            Thread gaThread = new Thread(() => {RunGA(gap);});
            gaThread.Start();
            while(gaThread.IsAlive & !shouldBreak)
                yield return new WaitForSeconds(0.1f);
            if(shouldBreak) break;
        }
        ReportMgr.inst.MultiRunAverages();
        yield return null;
    }

    [SerializeField] private GA ga;
    void RunGA(GAParameters gap) {
        ga = new GA(gap);
        ga.Run();
    }

    //------------------------------------------------------------------------------

    public void OnDestroy() {
        foreach(GAId gaid in gaList) {
            gaid.ga.isRunning = false; // ga.cleanup already sets ga.isRunning to false
        }
        Thread.Sleep(100);
        foreach(GAId gaid in gaList) {
            gaid.ga.Cleanup();
        }
    }

    public void DestroyMRSequential() {
        ga.isRunning = false;
        shouldBreak = true;
    }

}
