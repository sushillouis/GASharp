using System;
using System.Collections.Generic;
using System.Text;

public class Individual : IComparable<Individual>
{
    
    public int chromLength;
    public int[] chromosome;
    public float fitness;
    public float objectiveFunction;

    public Individual(int chromLength)
    {
        chromosome = new int[chromLength];
    }

    public void Init()
    {
        for(int i = 0; i < chromosome.Length; i++) {
            chromosome[i] = Rand.inst.Flip01(0.5f);
        }
    }

    public void Mutate(float pm)
    {
        for(int i = 0; i < chromosome.Length; i++) {
            chromosome[i] = (Rand.inst.Flip(pm) ? 1 - chromosome[i] : chromosome[i]);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < chromosome.Length; i++) {
            sb.Append(chromosome[i].ToString("0"));
        }
        sb.Append(", " + fitness);
        return sb.ToString();
    }

    public int CompareTo(Individual other)
    {
        return other.fitness.CompareTo(fitness);//From high fitness to low
    }
}
