using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class XOver
{
    public static void OnePoint(Individual parent1, Individual parent2, Individual child1, Individual child2, int chromosomeLength)
    {
        int x1 = GARandom.inst.RandInt(0, chromosomeLength);
        for(int i = x1; i < chromosomeLength; i++) {
            child1.bitChrom[i] = parent2.bitChrom[i];
            child2.bitChrom[i] = parent1.bitChrom[i];
        }

    }

    public static void TwoPoint(Individual parent1, Individual parent2, Individual child1, Individual child2, int chromosomeLength) {
        int x1 = GARandom.inst.RandInt(0, chromosomeLength);
        int x2 = GARandom.inst.RandInt(0, chromosomeLength);
        int low = Math.Min(x1, x2);
        int high = Math.Max(x1, x2);
        for(int i = low; i < high; i++) {
            child1.bitChrom[i] = parent2.bitChrom[i];
            child2.bitChrom[i] = parent1.bitChrom[i];
        }

    }

    public static void Greedy(Individual parent1, Individual parent2, Individual child1, Individual child2, int chromosomeLength, TSPEvaluator tspEvaluator) {
        int xp1 = GARandom.inst.RandInt(0, chromosomeLength);
        int xp2 = GARandom.inst.RandInt(xp1 + 1, chromosomeLength);
        for(int i = xp1; i <= xp2; i++) {
            if(i <= chromosomeLength - 2) {
                int city1 = child1.seqChrom[i];
                int city1Next = child1.seqChrom[i + 1];
                float d1 = tspEvaluator.GetDistance(city1, city1Next);

                int indexIn2 = Array.IndexOf(child2.seqChrom, city1);
                int city2 = child2.seqChrom[indexIn2];
                int nextIndex = (indexIn2 < chromosomeLength - 1 ? indexIn2 + 1 : 0);
                int city2Next = child2.seqChrom[nextIndex];
                float d2 = tspEvaluator.distances[city2, city2Next];
                if(d2 < d1) {
                    int swapIndex = Array.IndexOf(child1.seqChrom, city2Next);
                    int city = child1.seqChrom[swapIndex];
                    child1.seqChrom[swapIndex] = child1.seqChrom[i];
                    child1.seqChrom[i] = city;
                }
            }
        }

    }

    public static void PMX(Individual parent1, Individual parent2, Individual child1, Individual child2, int chromosomeLength) {
        int x1 = GARandom.inst.RandInt(0, chromosomeLength);
        int x2 = GARandom.inst.RandInt(0, chromosomeLength);
        int xp1 = Mathf.Min(x1, x2);
        int xp2 = Mathf.Max(x1, x2);
        if(xp2 - xp1 < 2)
            return;

        child1.seqChrom = Enumerable.Repeat(-1, chromosomeLength).ToArray();
        child2.seqChrom = Enumerable.Repeat(-1, chromosomeLength).ToArray();
        //Debug.Log("xp1: " + xp1 + " xp2: " + xp2);
        for(int i = xp1; i <= xp2; i++) {

            child1.seqChrom[i] = parent2.seqChrom[i];
            child2.seqChrom[i] = parent1.seqChrom[i];
        }

        FillOffspring(child1.seqChrom, parent1.seqChrom, xp1, xp2);
        FillOffspring(child2.seqChrom, parent2.seqChrom, xp1, xp2);

        for(int i = 0; i < chromosomeLength; i++) {
            if(child1.seqChrom[i] == -1)
                child1.seqChrom[i] = parent1.seqChrom[i];
            if(child2.seqChrom[i] == -1)
                child2.seqChrom[i] = parent2.seqChrom[i];
        }
    }

    static void FillOffspring(int[] child, int[] parent, int xp1, int xp2) {
        for(int i = 0; i < child.Length; i++) {
            if(i >= xp1 && i <= xp2)
                continue;
            int val = parent[i];
            int counter = 0;
            int index = Array.IndexOf(child, val, xp1, xp2 - xp1 + 1);
            while(index != -1 && counter < child.Length) {
                val = parent[index];
                counter++;
                index = Array.IndexOf(child, val, xp1, xp2 - xp1 + 1);
                if(counter == child.Length) {
                    Debug.Log("Error: too many iterations." + " i: " + i + ", index: " + index + ", val: " + val + ", xp1: " + xp1 + ", xp2: " + xp2);
                    Debug.Log("c: " + string.Join(", ", child));
                    Debug.Log("p: " + string.Join(", ", parent));
                }
            }
            child[i] = val;
        }
    
    }



}
