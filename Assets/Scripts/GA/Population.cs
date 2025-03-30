using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[Serializable]
public enum DeJong {
    F1 = 0, F2, F3, F4, F5,
}

[Serializable]
public class Population 
{
    GAParameters parameters;
    public Individual[] members;
    public float min, max, avg, sumFitness;
    public MetaCVRPEvaluator evaluator;// Constructed in Population constructor
    public CataclysmTracker fitnessTracker;

    public Individual bestIndividual;

    public Population(GAParameters p) {
        parameters = p;
        members = new Individual[parameters.populationSize * 2]; // *2 for CHC implementation since children double popsize
        fitnessTracker = new CataclysmTracker(parameters.npInterval);
    }

    public void Init(MetaCVRPEvaluator metaCVRPEvaluator)
    {
        evaluator = metaCVRPEvaluator;

        for(int i = 0; i < members.Length; i++) {
            members[i] = new Individual(parameters);
            members[i].Init();

        }
        Evaluate();
    }

    public void Generation(Population child)
    {
        int p1, p2;
        Individual parent1, parent2, child1, child2;
        for(int i = 0; i < members.Length; i += 2) {
            p1 = ProportionalSelector();
            p2 = ProportionalSelector();
            parent1 = members[p1];
            parent2 = members[p2];

            child1 = child.members[i]; // From the child's population
            child2 = child.members[i + 1];

            Reproduce(parent1, parent2, child1, child2);

        }
        child.Evaluate();
    }
    public void Reproduce(Individual parent1, Individual parent2, Individual child1, Individual child2)
    {
        for(int i = 0; i < parameters.bitChromLength; i++) {
            child1.bitChrom[i] = parent1.bitChrom[i];
            child2.bitChrom[i] = parent2.bitChrom[i];
        }

        if(GARandom.inst.Flip(parameters.pCross))
            XOver.TwoPoint(parent1, parent2, child1, child2, parameters.bitChromLength);
        //XOver.Greedy(parent1, parent2, child1, child2, parameters.bitChromLength, evaluator);
        //XOver.PMX(parent1, parent2, child1, child2, parameters.bitChromLength);

        child1.Mutate(parameters.pMut);
        child2.Mutate(parameters.pMut);
    }

    public void Halve(Population child)
    {
        Array.Sort(members); //Individual defines sorting from high fitness to low
        for(int i = 0; i < parameters.populationSize; i++) {
            child.members[i] = members[i];
        }
    }

    public void CHCWithCataclysms(Population child, int gen) {
        fitnessTracker.Enqueue(max);
        if(fitnessTracker.CheckCataclysm() && gen % parameters.localOptInterval != 0) {
            fitnessTracker.Reset();
            CataclysmicEvent(0, parameters.populationSize);
            Statistics();
        } 
        CHCGeneration(child);

    }

    public void CHCGeneration(Population child) {
        int p1, p2;
        Individual parent1, parent2, child1, child2;
        for(int i = 0; i < parameters.populationSize; i += 2) {
            p1 = ProportionalSelector();
            p2 = ProportionalSelector();
            parent1 = members[p1];
            parent2 = members[p2];

            child1 = members[parameters.populationSize + i]; //ADD to the parent's population
            child2 = members[parameters.populationSize + i + 1];

            Reproduce(parent1, parent2, child1, child2);
        }
        Evaluate(parameters.populationSize, members.Length);
        Halve(child); // sort and choose best half to make child population
    }

    public void Report(int gen)
    {
        GAPlotMgr.inst.AddStats(gen, avg, max);
        GAPlotMgr.inst.SetBest(bestIndividual);
        CVRPPlotMgr.inst.SetBest(bestIndividual);

        string report = gen + ": " + min + ", " + avg + ", " + max;
        InputHandler.inst.ThreadLog(report);

        using(StreamWriter w = File.AppendText("outfile")) {
            w.WriteLine(report);
        }
    }

    public void Statistics()
    {
        Statistics(0, parameters.populationSize);
    }
    public void Statistics(int start, int end)
    {
        float fit;
        bestIndividual = members[start];
        min = max = sumFitness = members[start].fitness;
        for(int i =  start + 1; i < end; i++) {
            fit = members[i].fitness;
            sumFitness += fit;
            if(fit < min) min = fit;
            if(fit > max) {
                max = fit;
                bestIndividual = members[i];
            }
        }
        avg = sumFitness/(end - start);

    }

    public int ProportionalSelector() // always on members[0 .. population size]
    {
        int index = -1;
        float sum = 0;
        float limit = (float) GARandom.inst.rand.NextDouble() * sumFitness;
        do {
            index = index + 1;
            sum += members[index].fitness;
        } while (sum < limit && index < parameters.populationSize - 1);
        return index;
    }

    public void Evaluate()
    {
        Evaluate(0, parameters.populationSize);
    }

    public void Evaluate(int start, int end)
    {
        for(int i = start; i < end; i++) {
            members[i].fitness = evaluator.Evaluate(members[i]);
        }
    }


    public void CataclysmicEvent(int start, int end) {
        members[start] = bestIndividual;
        for(int i = start + 1; i < end; i++) {
            for(int j = 0; j < parameters.bitChromLength; j++) {
                if(GARandom.inst.Flip(parameters.pmCat))
                    members[i].bitChrom[j] = 1 - bestIndividual.bitChrom[j];
                else
                    members[i].bitChrom[j] = bestIndividual.bitChrom[j];
            }
        }
        Evaluate(start, end);

    }

    public void LocalOpt(int start, int end) {

        Statistics();
        evaluator.LocalOpt(bestIndividual);
        for(int i = start; i < end; i++) {
            if(GARandom.inst.Flip(parameters.pMut))
                evaluator.LocalOpt(members[i]);
        }
    }

    public void Print()
    {
        for(int i = 0; i < parameters.populationSize; i++) {
            InputHandler.inst.ThreadLog(members[i].ToString());
        }
    }
}
