using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;




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

    public void Init() {
        cities = new Vector3[gap.chromosomeLength];
        //GA gets initialized and calls this only after app files have been loaded.
        cities = TSPPlotMgr.inst.cities.ToArray();
        nCities = TSPPlotMgr.inst.nCities; 

        Debug.Log("Initialized TSP");
        distances = new float[nCities, nCities];
        ComputeDistances();

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
        for(int i = 1; i < individual.chromLength; i++) {
            int first = individual.chromosome[i-1];
            int second = individual.chromosome[i];
            tourLength += distances[first, second];
        }
        tourLength += distances[individual.chromosome[individual.chromLength - 1], individual.chromosome[0]];
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
        for(int i = 0; i < individual.chromLength - 1; i++) {
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

        while(hasImproved && count < 40) {
            hasImproved = false;
            maxGain = 0;

            for(int i = 1; i < ind.chromLength - 1; i++) {
                for(int j = i + 1; j < ind.chromLength - 1; j++) {

                    di = distances[ind.chromosome[i - 1], ind.chromosome[i]];
                    dj = distances[ind.chromosome[j], ind.chromosome[j + 1]];

                    ndi = distances[ind.chromosome[i], ind.chromosome[j + 1]];
                    ndj = distances[ind.chromosome[j], ind.chromosome[i - 1]];

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
                Array.Reverse(ind.chromosome, bi, bj - bi + 1);
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
        int x1 = ind.chromosome[i];
        ind.chromosome[i] = ind.chromosome[j];
        ind.chromosome[j] = x1;

    }

}
//27, 39, 28, 74, 41, 4, 15, 5, 48, 67, 63, 32, 69, 55, 49, 1, 38, 62, 16, 43, 14, 21, 18, 23, 51, 50, 0, 54, 30, 34, 47, 36, 11, 10, 46, 35, 33, 60, 8, 12, 40, 61, 56, 25, 71, 45, 42, 19, 44, 65, 22, 66, 75, 53, 37, 29, 68, 57, 58, 17, 13, 3, 20, 6, 64, 31, 24, 52, 9, 59, 70, 73, 72, 26, 2, 7, 
//                                                                                         23, 51, 50, 0, 54, 30, 34, 47, 36, 11, 10, 46, 35, 33, 60, 8, 12, 40, 61, 56, 25, 71, 45, 42, 19, 44, 65, 22, 66, 75, 53, 37, 29, 68, 57, 58
//                                                                                         58, 57, 68, 29, 37, 53, 75, 66, 22, 65, 44, 19, 42, 45, 71, 25, 56, 61, 40, 12, 8, 60, 33, 35, 46, 10, 11, 36, 47, 34, 30, 54, 0, 50, 51, 23
//27, 39, 28, 74, 41, 4, 15, 5, 48, 67, 63, 32, 69, 55, 49, 1, 38, 62, 16, 43, 14, 21, 18, 58, 57, 68, 29, 37, 53, 75, 66, 22, 65, 44, 19, 42, 45, 71, 25, 56, 61, 40, 12, 8, 60, 33, 35, 46, 10, 11, 36, 47, 34, 30, 54, 0, 50, 51, 23, 17, 13, 3, 20, 6, 64, 31, 24, 52, 9, 59, 70, 73, 72, 26, 2, 7, 

