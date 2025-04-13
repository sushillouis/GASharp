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
    public int[] bitChrom;
    public int[] seqChrom;

    public float fitness;
    public float objectiveFunction = -1;

    public List<Route> routes = new List<Route>();
    public List<int> unserved = new List<int>();
    public List<int> available = new List<int>();
    public bool[] swapped;

    public float unservedDemand = 0;
    public float unservedDistance = 0;
    public float sumRouteLengths = 0;
    public float overCapacity = 0;

    public GAParameters parameters;


    public Individual(GAParameters parameters) {
        this.parameters = parameters;
        bitChrom = new int[parameters.bitChromLength];
        seqChrom = new int[parameters.seqChromLength];
    }

    public void Init()
    {
        for(int i = 0; i < parameters.bitChromLength; i++) {
            bitChrom[i] = GARandom.inst.Flip01(0.5f);
        }
        for(int i = 0; i < parameters.seqChromLength; i++) {
            seqChrom[i] = i;
        }
        GAUtils.Shuffle<int>(seqChrom);
        swapped = new bool[parameters.problem.cvrpData.nCustomers];
        routes.Clear();
        parameters.problem.evaluator.Initialize(this);
    }

    public void ResetSwapped() {
        for(int i = 0; i < swapped.Length; i++) 
            swapped[i] = false;
    }

    public void BitFlipMutation(float pm) {
        for(int i = 0; i < parameters.bitChromLength; i++) {
            bitChrom[i] = (GARandom.inst.Flip(pm) ? 1 - bitChrom[i] : bitChrom[i]);
        }
    }

    public void Mutate(float pm)    {
        BitFlipMutation(pm);
        //Swap(pm);
    }

    public void Swap(float pm) {
        for(int i = 0; i < parameters.seqChromLength; i++) {
            if(GARandom.inst.Flip(pm)) {
                int swapIndex = GARandom.inst.RandInt(0, parameters.seqChromLength);
                int tmp = seqChrom[i];
                seqChrom[i] = seqChrom[swapIndex];
                seqChrom[swapIndex] = tmp;
            }
        }
    }

    public void Invert(float pm) {
        for(int i = 0; i < parameters.seqChromLength; i++) {
            if(GARandom.inst.Flip(pm)) {
                int mp = GARandom.inst.RandInt(0, parameters.seqChromLength);
                int low = UnityEngine.Mathf.Min(i, mp);
                int high = UnityEngine.Mathf.Max(i, mp);
                Array.Reverse(seqChrom, low, high - low + 1);
            }
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        /*
        for(int i = 0; i < parameters.bitChromLength; i++) {
            sb.Append(bitChrom[i].ToString("0"));
        }
        sb.Append("\n");
        for(int i = 0; i < parameters.seqChromLength; i++) {
            sb.Append(seqChrom[i].ToString("0") + ", ");
        }
        sb.Append("\n");
        */
        sb.Append("Obj: " + objectiveFunction.ToString("0.0") + ", Fit: " + fitness.ToString("0.0") + 
            ", length: " + sumRouteLengths.ToString("0.0") + ", unserved: " + unservedDemand.ToString("0.0") +
            ", OverCapacity: " + overCapacity + "\n");

        foreach(Route route in routes) {
            sb.Append(route.ToString() + ", ");
        }
        sb.Append("\nUnserved: (" + string.Join(",", unserved) + ") ");
        return sb.ToString();
    }

    public int CompareTo(Individual other)
    {
        return other.fitness.CompareTo(fitness);//From high fitness to low
    }

    public void CreateFrom(Individual other) {
        bitChrom = new int[other.bitChrom.Length];
        seqChrom = new int[other.seqChrom.Length];
        routes = CVRPUtils.CreateRoutes(other.parameters.problem.cvrpData);
        swapped = new bool[other.parameters.problem.cvrpData.nCustomers];
        CopyFrom(other);
    }

    public void CopyFrom(Individual other) {
        for(int i = 0; i < parameters.bitChromLength; i++) {
            bitChrom[i] = other.bitChrom[i];
        }
        for(int i = 0; i < parameters.seqChromLength; i++) {
            seqChrom[i] = other.seqChrom[i];
        }
        fitness = other.fitness;
        objectiveFunction = other.objectiveFunction;
        unservedDemand = other.unservedDemand;
        sumRouteLengths = other.sumRouteLengths;
        overCapacity = other.overCapacity;

        int index = 0;
        foreach(Route route in routes) {
            route.Copy(other.routes[index++]);
        }
    }
}
