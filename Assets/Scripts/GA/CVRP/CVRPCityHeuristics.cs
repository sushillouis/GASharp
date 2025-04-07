using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CVRPCityHeuristics : ICVRPEvaluator {

    public CVRPData cvrpData;
    public float cMax = 1000000;
    public float overCapacityPenalty = 10;


    public int nBitsPerRoute = 0;
    public float bprPrecision = 0;
    public int nBitsPerHeuristic = 0;
    public float bphPrecision = 0;
    public int nBits = 2;

    public CVRPCityHeuristics(CVRPData data) {
        this.cvrpData = data;

        nBitsPerRoute = Mathf.CeilToInt(Mathf.Log(cvrpData.nVehicles, 2));
        bprPrecision = cvrpData.nVehicles / Mathf.Pow(2, nBitsPerRoute);
        nBits = nBitsPerRoute + nBitsPerHeuristic;

    }

    public int GetChromLength() {
        return nBits * cvrpData.nCustomers;
    }


    public void Initialize(Individual ind) {
        CVRPUtils.InitRoutes(ind, cvrpData);
    }
    public float Evaluate(Individual ind) {
        Decode(ind);
        float routeLength = RouteLength(ind);
        float overCapacity = OverCapacity(ind);
        ind.overCapacity = overCapacity;
        ind.objectiveFunction = routeLength + (overCapacityPenalty * overCapacity);
        ind.fitness = cMax - ind.objectiveFunction;
        return ind.fitness;
    }

    public void Decode(Individual ind) {
        int val = -1;
        int routeIndex = -1;
        int ci = 0;
        ClearRoutes(ind);
        for(int i = 0; i < ind.bitChrom.Length; i += nBitsPerRoute) {
            val = GAUtils.Decode(ind.bitChrom, i, nBitsPerRoute);
            routeIndex = GAUtils.GetDecodeValue(val, 0, bprPrecision);
            ind.routes[routeIndex].tour.Add(ci++);
        }
        foreach(Route route in ind.routes) {
            float tmp = route.ComputeTourLength();
            route.LK2();
        }
    }
    public void ClearRoutes(Individual ind) {
        foreach(Route route in ind.routes)
            route.Reset();
    }

    public float RouteLength(Individual ind) {
        float sum = 0;
        foreach(Route route in ind.routes) {
            sum += route.ComputeTourLength();

        }

        return sum;
    }

    public float OverCapacity(Individual ind) {
        float overCapacity = 0;
        foreach(Route route in ind.routes) {
            route.demand = 0;
            foreach(int ci in route.tour) {
                route.demand += cvrpData.customers[ci].demand;
            }
            if(route.demand > cvrpData.vehicleCapacity) {
                overCapacity += route.demand - cvrpData.vehicleCapacity;
            }
        }
        return overCapacity;
    }

    int maxOptIterations = 100;
    public float LocalOpt(Individual ind) {
        int index = -1;
        float oldFit = ind.fitness;
        float newFit = -1;
        for(int i = 0; i < maxOptIterations; i++) {
            index = GARandom.inst.RandInt(0, ind.bitChrom.Length);
            ind.bitChrom[index] = 1 - ind.bitChrom[index];
            newFit = Evaluate(ind);
            if(newFit >= oldFit) {
                oldFit = newFit;
            } else {
                ind.bitChrom[index] = 1 - ind.bitChrom[index];
            }

        }
        return oldFit;
    }

}
