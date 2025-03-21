using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GAUtils 
{

    public static void Shuffle<T>(T[] array) {
        for(int i = 0; i < array.Length; i++) {
            int j = GARandom.inst.RandInt(0, array.Length);
            int k = GARandom.inst.RandInt(0, array.Length);
            T tmp = array[j];
            array[j] = array[k];
            array[k] = tmp;
        }
    }

}
