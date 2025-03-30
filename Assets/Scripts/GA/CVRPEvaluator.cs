using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class Customer {
    public Vector3 pos;
    public float demand;
    public int cid;
}

[Serializable]
public class CVRPEvaluator 
{
    public string problemName;
    public string problemDescription;
    public string problemURL;
    public string filelistFilename = "CVRPList.html";
    public string problemFilename;
    public List<string> problems = new List<string>();

    [Header("Data")]
    public int nCustomers;
    public int nVehicles;
    public int vehicleCapacity;
    public Customer[] customers;
    public List<Customer> depots = new List<Customer>();
    public List<int> depotIds = new List<int>();
    public float[,] distances;
    public float[] depotDistances;

    public int maxLKIterations;
    public int Penalty = 1000;
    public float cMax = 1000000;

    public List<string> LocalGetAvailableProblems() {
        StreamReader sr = new StreamReader(filelistFilename);
        string contents = sr.ReadToEnd();
        sr.Close();
        string[] lines = contents.Trim().Split('\n');
        problems = lines.ToList<string>();
        return problems;
    }

    public virtual void Init() {

        distances = new float[nCustomers, nCustomers];
        depotDistances = new float[nCustomers];
        ComputeDistances();
        maxLKIterations = (nCustomers * nCustomers);
    }

    void ComputeDistances() {
        for(int i = 0; i < nCustomers; i++) {
            depotDistances[i] = Mathf.RoundToInt(Vector3.Distance(depots[0].pos, customers[i].pos));
        }
        for(int i = 0; i < nCustomers; i++) {
            for(int j = i + 1; j < nCustomers; j++) {
                distances[i,j] = Mathf.RoundToInt(Vector3.Distance(customers[i].pos, customers[j].pos));
                distances[j,i] = distances[i, j];
            }
        }
        for(int i = 0; i < nCustomers; i++) {
            distances[i, i] = 0;
        }
    }
    //-------------------------------------------------------------------------------
    public string[] ReadFromWeb(string problemURL) {
        return null;
    }

    public bool coordsSection = false;
    public bool demandSection = false;
    public bool depotSection = false;

    public void SetupProblemData(string[] lines) {
        foreach(string line in lines) {
            if(line.Contains("NAME")) {
                problemName = line.Substring(7);
                string[] items = line.Trim().Split('-');
                nVehicles = int.Parse(items[2].Trim().Substring(1));
                continue;
            }
            if(line.Contains("COMMENT")) {
                problemDescription = line.Substring(9);
                continue;
            }
            if(line.Contains("DIMENSION"))
                nCustomers = int.Parse(line.Substring(12));
            if(line.Contains("CAPACITY")) {
                vehicleCapacity = int.Parse(line.Substring(10).Trim());
                customers = new Customer[nCustomers];
            }
            if(line.Contains("NODE_COORD")) {
                coordsSection = true;
                demandSection = false;
                depotSection = false;
                continue;
            }
            if(line.Contains("DEMAND_SECTION")) {
                demandSection = true;
                coordsSection = false;
                depotSection = false;
                continue;
            }
            if(line.Contains("DEPOT_SECTION")) {
                depotSection = true;
                coordsSection = false;
                demandSection = false;
                continue;
            }
            if(coordsSection && !demandSection && !depotSection) {
                //Debug.Log("line: " + line);
                string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); //three items
                //Debug.Log("Items: " + string.Join(", ", items));
                if(items.Length < 3)
                    items = line.Split('\t');
                Customer c = new Customer();
                c.cid = int.Parse(items[0].Trim());
                c.pos = new Vector3(float.Parse(items[1].Trim()), 0, float.Parse(items[2].Trim()));
                customers[c.cid - 1] = c;

            }
            if(demandSection && !coordsSection && !depotSection) {
                string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); //three items                string[] items = line.Split(" "); //two items
                if(items.Length < 2)
                    items = line.Split('\t');
                int cid = int.Parse(items[0].Trim());
                customers[cid - 1].demand = float.Parse(items[1].Trim());
            }
            if(depotSection && !coordsSection && !demandSection) {
                int did = int.Parse(line.Trim());
                if(did > 0) {
                    depotIds.Add(did);
                } else {
                    depotSection = false;
                }
            }
        }
        GetDepots();
        TotalDemand();
    }

    void GetDepots() {
        List<Customer> cList = customers.ToList();
        depots = new List<Customer>();
        foreach(int cit in depotIds) {
            if(cList.Exists(x => x.cid == cit)) {
                Customer customer = cList.Find(x => x.cid == cit);
                cList.Remove(customer);
                depots.Add(customer);
            }
        }

        customers = cList.ToArray();
        nCustomers = cList.Count;
    }

    public float totalCustomerDemand = 0;
    void TotalDemand() {
        totalCustomerDemand = 0;
        for(int i = 0; i < nCustomers; i++) {
            totalCustomerDemand += customers[i].demand;
        }
    }

    public void ReadLocal(string problemFilename) { 
        StreamReader sr = new StreamReader(problemFilename);
        string allContent = sr.ReadToEnd();
        sr.Close();
        string[] lines = allContent.Split('\n');
        SetupProblemData(lines);
    }
    //-------------------------------------------------------------------------------
    public float Evaluate(Individual ind) {
        DecodeToRoutes(ind);
        return EvaluateRoutes(ind);
    }

    public float EvaluateRoutes(Individual ind) {
        float sum = 0;
        float maxTourLength = 0;
        foreach(Route route in ind.routes) {
            int[] tourArray = route.tour.ToArray();
            route.tourLength = 0;
            route.tourLength += depotDistances[tourArray[0]];
            for(int i = 1; i < tourArray.Length; i++) {
                route.tourLength += distances[tourArray[i - 1], tourArray[i]];
            }
            route.tourLength += depotDistances[tourArray[tourArray.Length - 1]];
            sum += route.tourLength;
            if(route.tourLength > maxTourLength) {
                maxTourLength = route.tourLength;
            }
        }

        if(ind.unserved.Count > 0) { // penalize unserved customers
            int[] unservedArray = ind.unserved.ToArray();
            sum += depotDistances[unservedArray[0]];
            for(int i = 1; i < unservedArray.Length; i++) {
                sum += distances[unservedArray[i - 1], unservedArray[i]];
            }
            sum += depotDistances[unservedArray[unservedArray.Length - 1]];
            sum += Penalty;
        }

        ind.objectiveFunction = sum;
        ind.fitness = cMax - ind.objectiveFunction;
        return ind.fitness;
    }


    public virtual float LocalOpt(Individual ind) {
        return LK2CVRP(ind);
    }


    public virtual void DecodeToRoutes(Individual ind) {
        //InputHandler.inst.ThreadLog("CVRPDecoding: \n");
        ind.routes.Clear();
        ind.unserved.Clear();
        ind.unservedDemand = 0;

        Route currentRoute = new Route();
        currentRoute.vid = 0; 
        currentRoute.demand = 0;
        currentRoute.tour.Clear();
        ind.routes.Add(currentRoute);
        

        for(int i = 0; i < ind.parameters.seqChromLength; i++) {
            int ci = ind.seqChrom[i];
            float demand = customers[ci].demand;
            if(currentRoute.demand + demand <= vehicleCapacity) {
                currentRoute.demand += demand;
                currentRoute.tour.Add(ci);
            } else if(currentRoute.vid < nVehicles - 1) { // vehicles left
                int vid = currentRoute.vid + 1;
                currentRoute = CreateAndAddNewRoute(vid, ci, demand);
                ind.routes.Add(currentRoute);
            } else {
                ind.unservedDemand = 0;
                ind.unserved.Clear();
                for(int j = i; j < ind.parameters.seqChromLength; j++) {
                    ind.unserved.Add(ind.seqChrom[j]);
                    ind.unservedDemand += customers[ind.seqChrom[j]].demand;
                }
                break; // cannot add any customers, no vehicles left
            }
        }
    }

    Route CreateAndAddNewRoute(int vid, int ci, float demand) {
        Route route = new Route();
        route.vid = vid;
        route.demand = demand;
        route.tour.Clear();
        route.tour.Add(ci);
        return route;
    }

    public float LK2CVRP(Individual ind) {
        float gain = 0;
        float maxGain = 0;
        bool hasImproved = true;
        int count = 0;
        int mi = 0, mj = -1;
        float oldFit, oldObj;
        //InputHandler.inst.ThreadLog("Best: \n" + ind.ToString());
        oldFit = ind.fitness;
        oldObj = ind.objectiveFunction;
        while(hasImproved && count++ < maxLKIterations) {
            hasImproved = false;
            gain = 0;
            for(int i = 0; i < ind.parameters.seqChromLength - 1; i++) {
                for(int j = i + 1; j < ind.parameters.seqChromLength; j++) {
                    Array.Reverse(ind.seqChrom, i, j - i + 1); // reverse the portion
                    float newFit = this.Evaluate(ind);
                    Array.Reverse(ind.seqChrom, i, j - i + 1); // reverse the portion
                    gain = newFit - oldFit;
                    if(gain > maxGain) {
                        maxGain = gain;
                        hasImproved = true;
                        mi = i;
                        mj = j;
                        //InputHandler.inst.ThreadLog($"Improvement: count: {count}, i: {i}, j: {j}, gain: {gain}");
                        //InputHandler.inst.ThreadLog($"newFit: {newFit}, newObj: {ind.objectiveFunction}, maxGain = {maxGain}");
                    }
                }
            }

            if(hasImproved) {
                Array.Reverse(ind.seqChrom, mi, mj - mi + 1); // reverse the portion
                ind.fitness = this.Evaluate(ind);
                maxGain = 0;
                oldFit = ind.fitness;
                oldObj = ind.objectiveFunction;
                //InputHandler.inst.ThreadLog($"EndWhile\n: {ind.ToString()}");
            }
        }
        float fit = this.Evaluate(ind);
        //InputHandler.inst.ThreadLog("Best After LocalOpt: \n" + ind.ToString());
        return ind.fitness;
    }

    public float LinK3CVRP(Individual ind) {
        float gain = 0;
        float maxGain = 0;
        bool hasImproved = true;
        int count = 0;
        float oldFit, oldObj;
        int[] origChrom = ind.seqChrom;//        [0..ind.chromosome.Length];
        int[] bestChrom = ind.seqChrom;
        InputHandler.inst.ThreadLog("Best: \n" + ind.ToString());
        oldFit = ind.fitness;
        oldObj = ind.objectiveFunction;
        while(hasImproved && count++ < maxLKIterations) {
            hasImproved = false;
            gain = 0;
            for(int i = 1; i < ind.parameters.seqChromLength - 2; i++) {
                for(int j = i + 1; j < ind.parameters.seqChromLength - 1 ; j++) {
                    for(int k = j + 1; k < ind.parameters.seqChromLength -1; k++) {
                        List<int[]> variants = GenerateVariants(ind.seqChrom, i, j, k);
                        foreach(int[] chrom in variants) {
                            ind.seqChrom = chrom;
                            float newFit = Evaluate(ind);
                            gain = newFit - oldFit;
                            if(gain > maxGain) {
                                maxGain = gain;
                                bestChrom = chrom;
                                hasImproved = true;
                                InputHandler.inst.ThreadLog($"!I: {count}, newFit: {newFit}, oldFit: {oldFit}, obj: {ind.objectiveFunction}, gain: {gain}");
                                //InputHandler.inst.ThreadLog("!C: " + string.Join(", ", chrom));
                            }
                        }
                        
                    }
                }
            }
            if(hasImproved) {

                ind.seqChrom = bestChrom;
                InputHandler.inst.ThreadLog("Chrom!C: " + string.Join(", ", ind.seqChrom));
                ind.fitness = Evaluate(ind);
                InputHandler.inst.ThreadLog($"Fit: {ind.fitness}, obj: {ind.objectiveFunction}");
                maxGain = 0;
                oldFit = ind.fitness;
                oldObj = ind.objectiveFunction;
            }


        }

        ind.seqChrom = bestChrom;
        float fit = Evaluate(ind);
        InputHandler.inst.ThreadLog("3Opt: \n" + ind.ToString());
        return ind.fitness;
    }

    public List<int[]> GenerateVariants(int[] chrom, int i, int j, int k) {
        List<int[]> variants = new List<int[]>();

        //InputHandler.inst.ThreadLog("chrom: " + string.Join(", ", chrom));
        int[] beforei = chrom[0..i];
        //InputHandler.inst.ThreadLog("Beforei: " + string.Join(", ", beforei));
        int[] betweenIAndJ = chrom[i..j];
        //InputHandler.inst.ThreadLog("betweenIJ: " + string.Join(", ", betweenIAndJ));
        int[] betweenJAndK = chrom[j..k];
        //InputHandler.inst.ThreadLog("betweenJK: " + string.Join(", ", betweenJAndK));
        int[] afterK = chrom[k..chrom.Length];
        //InputHandler.inst.ThreadLog("AfterK: " + string.Join(", ", afterK));
        int[] reverseIJ = betweenIAndJ.Reverse().ToArray();
        int[] reverseJK = betweenJAndK.Reverse().ToArray();

        variants.Add(ConcatSegments(beforei, betweenIAndJ, betweenJAndK, afterK));//original
        //InputHandler.inst.ThreadLog("Orig: " + string.Join(", ", variants[0]));

        variants.Add(ConcatSegments(beforei, betweenIAndJ, reverseJK, afterK));   //false, true , false
        //InputHandler.inst.ThreadLog("ftf : " + string.Join(", ", variants[1]));

        variants.Add(ConcatSegments(beforei, reverseIJ, betweenJAndK, afterK));   //true , false, false
        //InputHandler.inst.ThreadLog("tff : " + string.Join(", ", variants[2]));

        variants.Add(ConcatSegments(beforei, reverseIJ, reverseJK, afterK));      //true , true , false
        //InputHandler.inst.ThreadLog("ttf : " + string.Join(", ", variants[3]));

        variants.Add(ConcatSegments(beforei, betweenJAndK, betweenIAndJ, afterK));//false, false, true
        //InputHandler.inst.ThreadLog("fft : " + string.Join(", ", variants[4]));

        variants.Add(ConcatSegments(beforei, reverseJK, betweenIAndJ, afterK));   //false, true , true
        //InputHandler.inst.ThreadLog("ftt : " + string.Join(", ", variants[5]));

        variants.Add(ConcatSegments(beforei, betweenJAndK, reverseIJ, afterK));   //true , false, true
        //InputHandler.inst.ThreadLog("tft : " + string.Join(", ", variants[6]));

        variants.Add(ConcatSegments(beforei, reverseJK, reverseIJ, afterK));      //true , true , true
        //InputHandler.inst.ThreadLog("ttt : " + string.Join(", ", variants[7]));

        return variants;
    }

    int[] ConcatSegments(int[] bi, int[] bij, int[] bjk, int[] ak) {
        return bi.Concat(bij.Concat(bjk.Concat(ak))).ToArray();
    }

    public void TestSlicing(Individual ind) {
        List<int[]> x = GenerateVariants(ind.seqChrom, 2, 5, 9);
        return;

    }

}


/*
 * 
 * 
 * 
 * 
 */