using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FixedQueue<T> : Queue<T> {
    public int maxSize;
    public FixedQueue(int limit) : base() {
        //Debug.Log("Creating FixedQueue with limit: " + limit);
        maxSize = limit;
    }
    public new void Enqueue(T item) {
        //Debug.Log("Enqueueing: " + item + ", Count: " + Count + ", maxSize: " + maxSize);
        if(Count >= maxSize) {
            Dequeue();
        }
        base.Enqueue(item);
    }
}

[Serializable]
public class CataclysmTracker : FixedQueue<float> {

    public float newSum = 0;
    public float oldSum = 0;
    public CataclysmTracker(int limit) : base(limit) {
       // Debug.Log("Creating CataclysmTracker with limit: " + limit);

    }

    public bool CheckCataclysm() {
        bool result = false;
        if(Count < maxSize) {
            result = false;
        } else {
            oldSum = newSum;
            newSum = 0;
            foreach(float item in this) {
                newSum += item;
            }
            result = (Mathf.Abs(newSum - oldSum) < 1);
        }
        return result;
    }

    public void Reset() {
        oldSum = newSum = 0;
        Clear();
        //Debug.Log("Resetting CataclysmTracker, count: " + Count);

    }



}

