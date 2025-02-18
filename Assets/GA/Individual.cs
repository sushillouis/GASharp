using System;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class Individual : IComparable<Individual>
{
    
    public int chromLength;
    public int[] chromosome;
    public float fitness;
    public float objectiveFunction = -1;


    public GAParameters parameters;
    public float[] vars;



    public Individual(GAParameters parameters)//int chromLength)
    {
        this.parameters = parameters;
        chromLength = parameters.chromosomeLength;
        chromosome = new int[chromLength];
        vars = new float[parameters.nVars];

    }

    public void Init()
    {
        for(int i = 0; i < chromLength; i++) {
            chromosome[i] = GARandom.inst.Flip01(0.5f);
        }
    }

    public void Mutate(float pm)
    {
        for(int i = 0; i < chromLength; i++) {
            chromosome[i] = (GARandom.inst.Flip(pm) ? 1 - chromosome[i] : chromosome[i]);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < chromLength; i++) {
            if(i % parameters.nBits == 0)
                sb.Append('|');
            sb.Append(chromosome[i].ToString("0"));
        }
        sb.Append("|\n");
        for(int i = 0; i < parameters.nVars; i++) {
            sb.Append(vars[i].ToString("0.000") + ", " );
        }
        sb.Append('\n');
        sb.Append("Obj: " + objectiveFunction.ToString("0.000") + ", Fit: " + fitness.ToString("0.000"));
        return sb.ToString();
    }

    public int CompareTo(Individual other)
    {
        return other.fitness.CompareTo(fitness);//From high fitness to low
    }
}
