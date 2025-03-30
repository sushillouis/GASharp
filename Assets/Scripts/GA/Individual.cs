using System;
using System.Collections.Generic;
using System.Text;


[Serializable]
public enum TSPRepresentationType {
    CityList,
    Heuristics,
}

[Serializable]
public class Route{
    public int vid = -1;
    public float demand = -1;
    public float capacity = 0;
    public List<int> tour;
    public float tourLength = 0;

    public Route() {
        tour = new List<int>();
    }

    public override string ToString() {
        return "Route: vid: " + vid + ", demand: " + demand + ", tourLength: " + tourLength + " |" + string.Join(", ", tour) + "|";
    }

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
    public List<int> workingList = new List<int>();
    public float unservedDemand = 0;
    public float sumRouteLengths = 0;

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

        routes.Clear();
        parameters.metaCVRPEvaluator.InitRoutes(this);



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
        for(int i = 0; i < parameters.bitChromLength; i++) {
            sb.Append(bitChrom[i].ToString("0"));
        }
        sb.Append("\n");
        for(int i = 0; i < parameters.seqChromLength; i++) {
            sb.Append(seqChrom[i].ToString("0") + ", ");
        }
        sb.Append("\n");
        sb.Append("Obj: " + objectiveFunction.ToString("0.000") + ", Fit: " + fitness.ToString("0.000") + "\n");

        foreach(Route route in routes) {
            sb.Append("| " + string.Join(", ", route.tour) + ", dem: " + route.demand + " |");
        }
        return sb.ToString();
    }

    public int CompareTo(Individual other)
    {
        return other.fitness.CompareTo(fitness);//From high fitness to low
    }
}
