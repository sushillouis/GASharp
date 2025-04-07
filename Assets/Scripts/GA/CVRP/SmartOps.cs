using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SmartOps {

    public CVRPRouteCustomerHeuristics evaluator;
    public SmartOps(CVRPRouteCustomerHeuristics evaluator) {
        this.evaluator = evaluator;
    }

    public void NeighborSwap(Individual ind) {
        foreach(Route route in ind.routes) {
            Route neighbor = FindNeighbor(ind, route);
            SwapIfSavings(route, neighbor);
        }
    }

    public void NeighborInsert(Individual ind) {
        foreach(Route route in ind.routes) {
            route.ComputeMetrics();
        }
        //------------------------------------
        foreach(Route route in ind.routes) {
            Route neighbor = FindNeighbor(ind, route);
            InsertMaxSavings(route, neighbor);
        }
    }

    public Route FindNeighbor(Individual ind, Route route) {
        float minDist = float.MaxValue;
        Route minRoute = null;
        foreach(Route other in ind.routes) {
            if(other == route)
                continue;
            float dist = Vector3.Distance(route.centroid, other.centroid);
            if(dist < minDist) {
                minDist = dist;
                minRoute = other;
            } 
        }
        return minRoute;
    }

    void InsertMaxSavings(Route route, Route neighbor) {
        float maxSavings = 0;
        int customerIndex = -1;
        int index = 0;
        float savings = 0;
        int insertAtIndex = -1;
        foreach(int ci in route.tour) {
            (savings, insertAtIndex) = TrySavingsInsert(ci, neighbor);
            if(savings > maxSavings) {
                maxSavings = savings;
                customerIndex = ci;
            }
            index++;
        }
        if(customerIndex >= 0 && insertAtIndex >= 0) {
            InsertIntoNeighbor(route, neighbor, customerIndex, insertAtIndex);
        }

    }

    void InsertIntoNeighbor(Route route, Route neighbor, int customerIndex, int insertAtIndex) {
        //Insert customerIndex into neighbor at insertAtIndex
        route.tour.Remove(customerIndex);
        route.ComputeMetrics();
        neighbor.tour.Insert(insertAtIndex, customerIndex);
        neighbor.ComputeMetrics();
    }

    (float, int) TrySavingsInsert(int ci, Route neighbor) {
        return evaluator.FindMaxSavingsIndex(neighbor, ci);

    }

    void SwapIfSavings(Route route, Route neighbor) {
        foreach(int ci in route.tour) {
            float overCapacity = TrySavingsSwap(ci, neighbor);

        }
    }

    float TrySavingsSwap(int ci, Route neighbor) {
        //ToDo
        return 0;
    }



}
