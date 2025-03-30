using System;
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

    public static int Decode(int[] chrom, int start, int length) {
        int sum = 0;
        for(int i = start; i < start + length; i++) {
            sum += (int) (Mathf.Pow(2, i - start)) * chrom[i];
        }
        return sum;
    }

    public static int GetDecodeValue(int inVal, int min, float precision) {
        return min + Mathf.RoundToInt(inVal * precision);
    }

    public static int GetRR(int val, int limit) {
        if(limit <= 1)
            return 0;
        else
            return val % limit;
    }


    public static int[] Encode(int val, int nBits) {
        int[] bits = new int[nBits];
        for(int i = 0; i < nBits; i++) {
            bits[i] = val % 2;
            val = val / 2;
        }
        return bits;
    }


}
