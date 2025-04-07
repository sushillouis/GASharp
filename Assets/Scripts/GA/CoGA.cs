using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoGA : GA
{
    public static CoGA instance;
    public CoGA(GAParameters gap): base(gap) {
        Debug.Log("Initialed base GA");
        instance = this;
    }

    public void RunAsCoroutine(InputHandler ih) {
        ih.StartCoroutine(CoGAEvolve());
    }

    IEnumerator CoGAEvolve() {
        Init();
        yield return null;
        for(int i = 0; i < gaParameters.numberOfGenerations; i++) {
            GenerationStep(i);
            yield return null;
        }
        //parents.evaluator.LocalOpt(parents.bestIndividual);
        Cleanup();
        Debug.Log("CoGA done!");
    }


}
