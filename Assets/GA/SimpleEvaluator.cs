using UnityEngine;
using System;
using System.Text;


[Serializable]
public class SimpleEvaluator
{
    public float precision = 0f;
    public GAParameters parameters;
    public SimpleEvaluator(GAParameters parameters) {

        this.parameters = parameters;
        precision = Precision(parameters.min, parameters.max, parameters.nBits);
        InputHandler.inst.ThreadLog("DeJong Constructor: " + parameters.min + ", " + parameters.max + ", " + precision + ", "
           + parameters.nBits + ", " + parameters.nVars);
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
    public float Evaluate(Individual individual)
    {
        float fitness = -1;
        switch(parameters.GAFunction) {
            case EvalFunction.DeJongF1:
                fitness = F1(individual);
                break;
            case EvalFunction.DeJongF2:
                fitness = F2(individual);
                break;
            case EvalFunction.DeJongF3:
                fitness = F3(individual);
                break;
            case EvalFunction.DeJongF4:
                fitness = F4(individual);
                break;
            case EvalFunction.X:
                fitness = XFunc(individual);
                break;
            case EvalFunction.OneMax:
                fitness = OneMax(individual);
                break;
            default:
                fitness = -1;
                break;
        }

        return fitness;
    }

    public float OneMax(Individual individual) {
        float sum = 0;
        for(int i = 0; i < parameters.nBits; i++) {
            sum += individual.chromosome[i];
        }
        individual.objectiveFunction = sum;
        return sum;
    }

    public float XFunc(Individual individual) {
        individual.objectiveFunction = Decode(0, parameters.nBits, 0, precision, individual);
        individual.vars[0]  = individual.objectiveFunction;
        return individual.objectiveFunction;
    }
    public float F1(Individual individual) {
        float sum = 0;
        for(int i = 0; i < parameters.nVars; i++) {
            float x = Decode(i * parameters.nBits, i * parameters.nBits + parameters.nBits, parameters.min, precision, individual);
            individual.vars[i] = x;
            sum += (x * x);
        }
        individual.objectiveFunction = sum;
        return 1f / (sum + 1f);
    }

    public float F2(Individual individual)
    {
        float x1 = Decode(0, 12, -2.048f, precision, individual);
        individual.vars[0] = x1;
        float x2 = Decode(12, 24, -2.048f, precision, individual);
        individual.vars[1] = x2;
        //InputHandler.inst.ThreadLog("xs: " + x1+ ", " + x2);
        individual.objectiveFunction = (100f * Mathf.Pow((Mathf.Pow(x1, 2) - x2), 2f) + Mathf.Pow((1 - x1), 2));
        return 1.0f / (individual.objectiveFunction + 1f);
    }

    public float F3(Individual individual)
    {
        int sum = 0;
        //float[] x = new float[5];
        for(int i = 0; i < 5; i++) {
            int x = (int) Decode(i * 10, i * 10 + 10, -5.12f, precision, individual);
            individual.vars[i] = (int)(x);
            sum += x;
            //InputHandler.inst.ThreadLog("F3: x: " + x + ", " + x);
        }
        individual.objectiveFunction = sum;

        return 1f/(30f + sum);//        25f + (1f / (sum + 0.0001f));
    }

    public float F4(Individual individual)
    {
        float sum = 0;
        float[] x = new float[30];
        for(int i = 0; i < 30; i++) {
            x[i] = Decode(i * 8, i * 8 + 8, -1.28f, precision, individual);
            individual.vars[i] = x[i];
            sum += i * Mathf.Pow(x[i], 4f) ;
            //InputHandler.inst.ThreadLog("F3: x: " + x + ", " + x);
        }
        sum += (float) GenerateGaussianNoise(0, 1);
        if(sum < 0)
            sum = 99999f;
        individual.objectiveFunction = sum;
        return (1f / (sum + 1f));


    }

    public float F5(Individual individual) {
        return 0; // not implemented
    }


    // Function to generate Gaussian noise using the Box-Muller transform (ChatGPT)
    static double GenerateGaussianNoise(double mean, double stdDev)
    {
        double u1 = 1.0 - GARandom.inst.rand.NextDouble();
        double u2 = 1.0 - GARandom.inst.rand.NextDouble();
        double standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * standardNormal;
    }
   
}
