using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CVRPCustomerList : ICVRPEvaluator {
    public CVRPData cvrpData;
    public float cMax = 1000000;
    public float overCapacityPenalty = 10;
    public CVRPCustomerList(CVRPData data) {
        this.cvrpData = data;
    }

    public void Initialize(Individual ind) {
        CVRPUtils.InitRoutes(ind, cvrpData);
    }

    public int GetChromLength() {
        return cvrpData.nCustomers;
    }

    public float Evaluate(Individual ind) {
        return ind.fitness;
    }

    public float LocalOpt(Individual ind) {
        return ind.fitness;
    }

    public void Decode(Individual ind) {
    }
}
