using UnityEngine;
using System;

public class AXnEvaluator
{

    public AXnEvaluator(float mn, float mx, float aVal, float nVal, int nBits) 
    {
        min = mn;
        max = mx;
        precision = Precision(min, max, nBits);
        A = aVal;
        N = nVal;
        InputHandler.inst.ThreadLog("Axn Constructor: " + min + ", " + max + ", " + precision + ", "
            + aVal + ", " + nVal + ", " + nBits);
    }
    public static float Decode(int start, int end, float min, float precision, Individual individual)
    {
        float sum = 0;
        for(int i = start; i < end; i++) {
            sum += individual.chromosome[i] * Mathf.Pow(2, i - start);
        }
        return min + (precision * sum);
    }

    public static float Precision(float min, float max, int nBits)
    {
        return (max - min) / (Mathf.Pow(2, nBits) - 1);
    }

    public float min = 0f;
    public float max = 31f;
    public float precision = 0f;
    public float A = 1f;
    public float N = 2f;

    public void SetPrecision(float min, float max, int nBits)
    {
        precision = Precision(min, max, nBits);
    }

    public float Evaluate(Individual individual)
    {
        float val = Decode(0, individual.chromLength, min, precision, individual);
        return A * Mathf.Pow(val, N);
    }
}
