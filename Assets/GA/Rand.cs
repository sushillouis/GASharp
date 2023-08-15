using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rand
{
    public static Rand inst;

    public System.Random rand;
    public Rand(int seed)
    {
        inst = this;
        rand = new System.Random(seed);
    }

    public bool Flip(float prob)
    {
        return (rand.NextDouble() < prob);
    }

    public int Flip01(float prob)
    {
        return (rand.NextDouble() < prob ? 0 : 1);
    }

    public int RandInt(int low, int high)
    {
        return rand.Next(low, high);
    }
}
