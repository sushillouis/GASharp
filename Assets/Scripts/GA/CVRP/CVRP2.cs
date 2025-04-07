using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public interface ICVRPEvaluator {
    public void Initialize(Individual ind);
    public int GetChromLength();
    float Evaluate(Individual ind);

    float LocalOpt(Individual ind);

    void Decode(Individual ind);
}


[Serializable]
public enum CVRPRepresentationType {
    CustomerList,
    CustomerHeuristics,
    RouteCityHeuristics,
}


[Serializable]
public class CVRP2 {

    public CVRPData cvrpData;
    public float cMax = 1000000;
    public float overCapacityPenalty = 10;

    public ICVRPEvaluator evaluator;

    public CVRP2(CVRPData data) {
        this.cvrpData = data;
        switch(cvrpData.representationType) {
            case CVRPRepresentationType.CustomerList:
                evaluator = new CVRPCustomerList(data);
                break;
            case CVRPRepresentationType.CustomerHeuristics:
                evaluator = new CVRPCityHeuristics(data);
                break;
            case CVRPRepresentationType.RouteCityHeuristics:
                evaluator = new CVRPRouteCustomerHeuristics(data);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

    public void Initialize(Individual ind) {
        evaluator.Initialize(ind);
    }

    public float Evaluate(Individual ind) {
        return evaluator.Evaluate(ind);

    }

    public float LocalOpt(Individual ind) {
        return evaluator.LocalOpt(ind);
    }

    public void Decode(Individual ind) {
        evaluator.Decode(ind);
    }

    public int GetChromLength() {
        InputHandler.inst.ThreadLog("GetChromLength: " + evaluator.GetChromLength());
        return evaluator.GetChromLength();
    }

}

    //public float EvaluateCityHeuristic(Individual ind) {
    //    Decode(ind);
    //    float routeLength = RouteLength(ind);
    //    float overCapacity = OverCapacity(ind);
    //    ind.overCapacity = overCapacity;
    //    ind.objectiveFunction = routeLength + (overCapacityPenalty * overCapacity);
    //    ind.fitness = cMax - ind.objectiveFunction;
    //    return ind.fitness;
    //}

    //public void Decode(Individual ind) {
    //    int val = -1;
    //    int routeIndex = -1;
    //    int ci = 0;
    //    ClearRoutes(ind);
    //    for(int i = 0; i < ind.bitChrom.Length; i += cvrpData.nBitsPerRoute) {
    //        val = GAUtils.Decode(ind.bitChrom, i, cvrpData.nBitsPerRoute);
    //        routeIndex = GAUtils.GetDecodeValue(val, 0, cvrpData.bprPrecision);
    //        ind.routes[routeIndex].tour.Add(ci++);
    //    }
    //    foreach(Route route in ind.routes) {
    //        route.ComputeTourLength();
    //        route.LK2();
    //    }
    //}
    //public void ClearRoutes(Individual ind) {
    //    foreach(Route route in ind.routes)
    //        route.Reset();
    //}

    //public float RouteLength(Individual ind) {
    //    float testSum = 0;
    //    foreach(Route route in ind.routes) {
    //        route.ComputeTourLength();
    //        testSum += route.tourLength;
    //    }

    //    return testSum;
    //}

    //public float OverCapacity(Individual ind) {
    //    float overCapacity = 0;
    //    foreach(Route route in ind.routes) {
    //        route.demand = 0;
    //        foreach(int ci in route.tour) {
    //            route.demand += cvrpData.customers[ci].demand;
    //        }
    //        if(route.demand > cvrpData.vehicleCapacity) {
    //            overCapacity += route.demand - cvrpData.vehicleCapacity;
    //        }
    //    }
    //    return overCapacity;
    //}

    //int maxOptIterations = 100;
    //public float LocalOpt(Individual ind) {
    //    int index = -1;
    //    float oldFit = ind.fitness;
    //    float newFit = -1;
    //    for(int i = 0; i < maxOptIterations; i++) {
    //        index = GARandom.inst.RandInt(0, ind.bitChrom.Length);
    //        ind.bitChrom[index] = 1 - ind.bitChrom[index];
    //        newFit = Evaluate(ind);
    //        if(newFit >= oldFit) {
    //            oldFit = newFit;
    //        } else {
    //            ind.bitChrom[index] = 1 - ind.bitChrom[index];
    //        }

    //    }
    //    return oldFit;
    //}

