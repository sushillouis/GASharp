using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;


[Serializable]
public enum TSPRepresentationType {
    CityList,
    Heuristics,
}

[Serializable]
public class Individual : IComparable<Individual>
{
    
    public int chromLength;
    public int[] chromosome;
    public float fitness;
    public float objectiveFunction = -1;
    public TSPEvaluator tspEvaluator;

    public GAParameters parameters;


    public Individual(GAParameters parameters)//int chromLength)
    {
        this.parameters = parameters;
        chromLength = parameters.chromosomeLength;
        chromosome = new int[chromLength];
    }

    public void Init()
    {
        for(int i = 0; i < chromLength; i++) {
            chromosome[i] = i;
        }
        GAUtils.Shuffle<int>(chromosome);

    }

    public void Mutate(float pm)
    {
        Swap(pm);
    }

    public void Swap(float pm) {
        for(int i = 0; i < chromLength; i++) {
            if(GARandom.inst.Flip(pm)) {
                int swapIndex = GARandom.inst.RandInt(0, chromLength);
                int tmp = chromosome[i];
                chromosome[i] = chromosome[swapIndex];
                chromosome[swapIndex] = tmp;
            }
        }
    }

    public void Invert(float pm) {
        for(int i = 0; i < chromLength; i++) {
            if(GARandom.inst.Flip(pm)) {
                int mp = GARandom.inst.RandInt(0, chromLength);
                int low = UnityEngine.Mathf.Min(i, mp);
                int high = UnityEngine.Mathf.Max(i, mp);
                Array.Reverse(chromosome, low, high - low + 1);
            }
        }
    }


    public int FindClosestCity(int city, List<int> segment, HashSet<int> visited) {
        float minDistance = int.MaxValue;
        int minCity = -1;
        float distance;
        foreach(int other in segment) {
            if(visited.Contains(other))
                continue;
            distance = tspEvaluator.GetDistance(city, other);
            if(distance < minDistance) {
                minDistance = distance;
                minCity = other;
            }
        }
        return minCity;
    }


    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < chromLength; i++) {
            sb.Append(chromosome[i].ToString("0") + ", ");
        }
        sb.Append("\n");
        sb.Append("Obj: " + objectiveFunction.ToString("0.000") + ", Fit: " + fitness.ToString("0.000"));
        return sb.ToString();
    }

    public int CompareTo(Individual other)
    {
        return other.fitness.CompareTo(fitness);//From high fitness to low
    }
}
