using System;
using UnityEngine;

[Serializable]
public enum TSPName {
    eil76,
    berlin52,
    eil51,
    lin105,
    lin318,
}

[Serializable]
public class TSPEvaluator 
{
    GAParameters gap;
    public TSPEvaluator(GAParameters parameters) {
        this.gap = parameters;
    }

    public Vector3[] cities;
    public int nCities;
    public float[,] distances;
    public int maxLKIterations = 100;
    public void Init() {
        cities = new Vector3[gap.bitChromLength];
        //GA gets initialized and calls this only after app files have been loaded.
        cities = TSPPlotMgr.inst.cities.ToArray();
        nCities = TSPPlotMgr.inst.nCities; 

        Debug.Log("Initialized TSP");
        distances = new float[nCities, nCities];
        ComputeDistances();
        maxLKIterations = (nCities -1) * (nCities - 1);

    }

    void ComputeDistances() {
        for(int i = 0; i < cities.Length; i++) {
            for(int j = i+1; j < cities.Length; j++) {
                distances[i, j] = Mathf.RoundToInt(Vector3.Distance(cities[i], cities[j]));
                distances[j, i] = distances[i, j];
            }
        }
        for(int i = 0; i < cities.Length; i++) {
            distances[i, i] = 0;
        }
    }

    public float Evaluate(Individual individual) {
        float tourLength = 0;
        for(int i = 1; i < individual.parameters.seqChromLength; i++) {
            int first = individual.seqChrom[i-1];
            int second = individual.seqChrom[i];
            tourLength += distances[first, second];
        }
        tourLength += distances[individual.seqChrom[individual.parameters.seqChromLength - 1], individual.seqChrom[0]];
        individual.objectiveFunction = tourLength;
        individual.fitness = 1000000 - tourLength;


        return individual.fitness;

    }

    public float GetDistance(int i, int j) {
        return distances[i, j];
    }
 
    public float LocalOpt(Individual individual) {

        float oldFit = individual.fitness;
        float newFit = -1;
        for(int i = 0; i < individual.parameters.seqChromLength - 1; i++) {
            SwapIndexes(individual, i, i + 1);
            newFit = Evaluate(individual);

            if(newFit > oldFit) {
                oldFit = newFit; // keep swap;
            } else {
                SwapIndexes(individual, i, i + 1); //swap back
            }

        }
        newFit = Evaluate(individual);
        return newFit;
    }

    public float LK2(Individual ind) {
        //InputHandler.inst.ThreadLog(ind.ToString());
        float di, dj;
        float ndi, ndj;
        int bi = 0, bj = -1;
        float gain;
        float maxGain = 0;
        bool hasImproved = true;
        int count = 0;
        float newObj, newFit;

        while(hasImproved && count < maxLKIterations) {
            hasImproved = false;
            maxGain = 0;

            for(int i = 1; i < ind.parameters.seqChromLength - 1; i++) {
                for(int j = i + 1; j < ind.parameters.seqChromLength - 1; j++) {

                    di = distances[ind.seqChrom[i - 1], ind.seqChrom[i]];
                    dj = distances[ind.seqChrom[j], ind.seqChrom[j + 1]];

                    ndi = distances[ind.seqChrom[i], ind.seqChrom[j + 1]];
                    ndj = distances[ind.seqChrom[j], ind.seqChrom[i - 1]];

                    gain = (di + dj) - (ndi + ndj);
                    if(gain > maxGain) {
                        maxGain = gain;
                        bi = i;
                        bj = j;
                        InputHandler.inst.ThreadLog($"count: {count}, i: {i}, j: {j}, gain: {gain}");
                        hasImproved = true;
                    }
                }
            }
            //int[] chrom = ind.chromosome;
            //InputHandler.inst.ThreadLog($"i-1: {bi-1}, c[i-1]: {chrom[bi-1]}, i:{bi}, c[i]:{chrom[bi]} || j:{bj}, c[j]: {chrom[bj]}, j+1: {bj+1}, c[j+1]: {chrom[bj+1]}");
            if(hasImproved) {
                Array.Reverse(ind.seqChrom, bi, bj - bi + 1);
                newObj = ind.objectiveFunction - maxGain;
                newFit = ind.fitness + maxGain;
                ind.objectiveFunction = newObj;
                ind.fitness = newFit;
                InputHandler.inst.ThreadLog($"maxGain: {maxGain}, nO: {newObj}, nF: {newFit}");
                //float evalFit = Evaluate(ind);
                //InputHandler.inst.ThreadLog(ind.ToString());
                //if(evalFit != newFit) {
                //    InputHandler.inst.ThreadLog($"Fitness mismatch: new: {newFit}, eval: {evalFit}");
                //    InputHandler.inst.ThreadLog(ind.ToString());
                //}
            }
            count++;
        }
        return maxGain;
    }

    public void SwapIndexes(Individual ind, int i, int j) {
        int x1 = ind.seqChrom[i];
        ind.seqChrom[i] = ind.seqChrom[j];
        ind.seqChrom[j] = x1;

    }

}
