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
        float obj = A * Mathf.Pow(val, N);
        return obj;
    }

    public float F2(Individual individual)
    {
        float x1 = Decode(0, 12, -2.048f, precision, individual);
        float x2 = Decode(12, 24, -2.048f, precision, individual);
        //InputHandler.inst.ThreadLog("xs: " + x1+ ", " + x2);
        return 1.0f/(1f + ((100f * Mathf.Pow((Mathf.Pow(x1, 2) - x2), 2f)) + Mathf.Pow((1 - x1), 2)));
    }

    public float F3(Individual individual)
    {
        int sum = 0;
        //float[] x = new float[5];
        for(int i = 0; i < 5; i++) {
            sum += (int) Decode(i * 10, i * 10 + 10, -5.12f, precision, individual);
        }
        sum += 30;
        if(sum < 0)
            InputHandler.inst.ThreadLog("Exception: " + sum);
        return (30f/ (sum + 1f));
    }
}
