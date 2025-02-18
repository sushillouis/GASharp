using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class XOver
{
    public static void OnePoint(Individual parent1, Individual parent2, Individual child1, Individual child2, int chromosomeLength)
    {
        int x1 = GARandom.inst.RandInt(0, chromosomeLength);
        for(int i = x1; i < chromosomeLength; i++) {
            child1.chromosome[i] = parent2.chromosome[i];
            child2.chromosome[i] = parent1.chromosome[i];
        }

    }

    public static void TwoPoint(Individual parent1, Individual parent2, Individual child1, Individual child2, int chromosomeLength)
    {
        int x1 = GARandom.inst.RandInt(0, chromosomeLength);
        int x2 = GARandom.inst.RandInt(0, chromosomeLength);
        int low = Math.Min(x1, x2);
        int high = Math.Max(x1, x2);
        for(int i = low; i < high; i++) {
            child1.chromosome[i] = parent2.chromosome[i];
            child2.chromosome[i] = parent1.chromosome[i];
        }

    }



}
