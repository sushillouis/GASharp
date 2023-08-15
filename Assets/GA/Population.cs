using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Population 
{
    GAParameters parameters;
    public Individual[] members;
    public float min, max, avg, sumFitness;

    public Population(GAParameters p)
    {
        parameters = p;
        members = new Individual[parameters.populationSize * 2]; // *2 for CHC implementation since children double popsize
    }

    public void Init()
    {
        for(int i = 0; i < members.Length; i++) {
            members[i] = new Individual(parameters.chromosomeLength);
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
        for(int i = 0; i < parameters.chromosomeLength; i++) {
            child1.chromosome[i] = parent1.chromosome[i];
            child2.chromosome[i] = parent2.chromosome[i];
        }

        if(Rand.inst.Flip(parameters.pCross))
            XOver.OnePoint(parent1, parent2, child1, child2, parameters.chromosomeLength);

        child1.Mutate(parameters.pMut);
        child2.Mutate(parameters.pMut);
    }

    public void Halve(Population child)
    {
        Array.Sort(members);
        for(int i = 0; i < parameters.populationSize; i++) {
            child.members[i] = members[i];
        }
    }

    public void CHCGeneration(Population child)
    {
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
        Halve(child);
    }

    public void Report(int gen)
    {
        string report = gen + ": " + min + ", " + avg + ", " + max;

        GraphMgr.inst.AddPoint(gen, avg, max);
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
        min = max = sumFitness = members[start].fitness;
        for(int i =  start + 1; i < end; i++) {
            fit = members[i].fitness;
            sumFitness += fit;
            if(fit < min) min = fit;
            if(fit > max) max = fit;
        }
        avg = sumFitness/(end - start);

    }

    public int ProportionalSelector() // always on members[0 .. population size]
    {
        int index = -1;
        float sum = 0;
        float limit = (float) Rand.inst.rand.NextDouble() * sumFitness;
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
            members[i].fitness = Evaluator.Evaluate(members[i]);
        }
    }

    public void Print()
    {
        for(int i = 0; i < parameters.populationSize; i++) {
            InputHandler.inst.ThreadLog(members[i].ToString());
        }
    }
}
