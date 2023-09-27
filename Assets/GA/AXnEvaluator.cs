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
        return 1.0f /(1f + ((100f * Mathf.Pow((Mathf.Pow(x1, 2) - x2), 2f)) + Mathf.Pow((1 - x1), 2)));
    }

    public float F3(Individual individual)
    {
        int sum = 25;
        //float[] x = new float[5];
        for(int i = 0; i < 5; i++) {
            int x = (int) Decode(i * 10, i * 10 + 10, -5.12f, precision, individual);
            sum += x;
            //InputHandler.inst.ThreadLog("F3: x: " + x + ", " + x);
        }
        if(sum < 0)
            InputHandler.inst.ThreadLog("Exception: " + sum);
        return (1f / (sum + 1f));
    }

    public float F4(Individual individual)
    {
        float sum = 0;
        float[] x = new float[30];
        for(int i = 0; i < 30; i++) {
            x[i] = Decode(i * 8, i * 8 + 8, -1.28f, precision, individual);
            sum += i * Mathf.Pow(x[i], 4f) ;
            //InputHandler.inst.ThreadLog("F3: x: " + x + ", " + x);
        }
        sum += (float) GenerateGaussianNoise(0, 1);
        if(sum < 0)
            sum = 99999f;
        return (1f / (sum + 1f));


    }

    // Function to generate Gaussian noise using the Box-Muller transform (ChatGPT)
    static double GenerateGaussianNoise(double mean, double stdDev)
    {
        double u1 = 1.0 - Rand.inst.rand.NextDouble();
        double u2 = 1.0 - Rand.inst.rand.NextDouble();
        double standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * standardNormal;
    }
   
}
