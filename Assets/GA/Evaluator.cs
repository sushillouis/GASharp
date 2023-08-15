using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Evaluator 
{

    public static float Evaluate(Individual individual)
    {
        float sum = 0;
        //Maximize number of 1's in the chromosome
        for(int i = 0; i < individual.chromosome.Length; i++) {
            sum += individual.chromosome[i]; // count number of one's
        }
        return sum;
    }

}
