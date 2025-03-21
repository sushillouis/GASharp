using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class F2 
{

    public float x1;
    public float x2;

    public float Evaluate(Individual individual)
    {

        return 100 * (x2 - Mathf.Pow(x1, 2)) + Mathf.Pow((x1 - 1), 2);
    }


}
