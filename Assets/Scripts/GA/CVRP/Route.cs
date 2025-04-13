using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class representing a route in the CVRP (Capacitated Vehicle Routing Problem).
/// A route consists of the starting depot, a sequence of customers, and the same ending depot.
/// In benchmark data files, a depot has demand 0
/// an empty route consists of two occurences of the depot
/// a customer or depot is represented by the INDEX into the customer array.
/// Typically, depots are at the beginning of the customer array with index 0
/// We are gettings CVRP benchmark data from the CVRPLib website http://vrp.galgos.inf.puc-rio.br/index.php/en/
/// </summary>
[Serializable]
public class Route {
    public int vid = -1;
    public float demand = -1;
    public float overCapacity = 0;
    public List<int> tour;
    public float tourLength = 0;

    public Vector3 depotPosition;
    public Vector3 centroid;
    public float diameter;

    public int outlierIndexInRoute;
    public int outlierCustomerIndex;


    public float[,] distances;
    public float[] depotDistances;
    public List<Customer> depots;
    public Customer[] customers;
    public int[] depotIndices;




    public Route(float[,] distances, float[] depotDistances, Customer[] customers, List<Customer> depots) {
        this.distances = distances;
        this.depotDistances = depotDistances;
        this.customers = customers;
        this.depots = depots;
        List<int> depIndices = new List<int>();

        for(int i = 0; i < customers.Length; i++) 
            if(customers[i].demand == 0) 
                depIndices.Add(i);
        depotIndices = depIndices.ToArray();

        depotPosition = depots[0].pos; // Assume depots[0] is the only depot
        tour = new List<int>();
    }

    public void Reset() {
        tour.Clear();
        tourLength = 0;
        demand = 0;
        overCapacity = 0;
        centroid = Vector3.zero;
        diameter = 0;
    }

    public void Copy(Route other) {
        this.vid = other.vid;
        this.demand = other.demand;
        this.overCapacity = other.overCapacity;
        this.tour = new List<int>(other.tour);
        this.tourLength = other.tourLength;
        this.centroid = other.centroid;
        this.diameter = other.diameter;
    }

    public override string ToString() {
        return "R: " + vid + //", dem: " + demand + ", Length: " + tourLength +
            //", ctrd: " + centroid.ToString() + ", diam: " + diameter +
            " | " + string.Join(", ", tour) + " |";
    }

    public float ComputeTourLength() {
        tourLength = 0;
        if(tour.Count > 0) {
            tourLength += depotDistances[tour[0]];
            for(int i = 0; i < tour.Count - 1; i++) {
                tourLength += distances[tour[i], tour[i + 1]];
            }
            tourLength += depotDistances[tour[tour.Count - 1]];
        }
        return tourLength;
    }

    public void ComputeMetrics() {
        ComputeTourLength();
        ComputeCentroid();
        ComputeDiameter();
        ComputeDemand();
    }


    public (int, int) FindOutlier(bool[] swapped) {
        float maxDistSqr = 0;
        float distSqr;
        int index = 0;
        outlierIndexInRoute = -1;
        outlierCustomerIndex = -1;
        foreach(int ci in tour) {
            if(!swapped[ci]) {
                distSqr = Vector3.SqrMagnitude(customers[ci].pos - centroid);
                if(distSqr > maxDistSqr) {
                    maxDistSqr = distSqr;
                    outlierIndexInRoute = index;
                    outlierCustomerIndex = ci;
                }
            }
            index++;
        }
        return (outlierIndexInRoute, outlierCustomerIndex);
    }

    public void ComputeCentroid() {
        centroid = Vector3.zero;
        foreach(int i in tour) {
            centroid += (customers[i].pos - depotPosition);
        }
        centroid /= tour.Count;
    }

    public float ComputeDemand() {
        demand = 0;
        foreach(int i in tour) {
            demand += customers[i].demand;
        }
        return demand;
    }

    public void ComputeDiameter() {
        ComputeCentroid();
        float radius = 0.0f;
        foreach(int ci in tour) {
            radius += Vector3.Distance(customers[ci].pos, centroid);
        }
        diameter = radius * 2;
    }

    public int maxLKIterations = 400;
    public float LK2() {
        //InputHandler.inst.ThreadLog(ToString());
        float di, dj;
        float ndi, ndj;
        int bi = 0, bj = -1;
        float gain;
        float maxGain = 0;
        bool hasImproved = true;
        int count = 0;
        float oldTourLength = tourLength;

        while(hasImproved && count < maxLKIterations) {
            hasImproved = false;
            maxGain = 0;

            for(int i = 0; i < tour.Count - 1; i++) {
                for(int j = i + 1; j < tour.Count; j++) {
                    di = GetDistance(i - 1, i);
                    dj = GetDistance(j, j + 1);

                    ndi = GetDistance(i, j + 1);
                    ndj = GetDistance(j, i - 1);

                    gain = (di + dj) - (ndi + ndj);
                    if(gain > maxGain) {
                        maxGain = gain;
                        bi = i;
                        bj = j;
                        //InputHandler.inst.ThreadLog($"count: {count}, i: {i}, j: {j}, gain: {gain}");
                        hasImproved = true;
                    }
                }
            }
            //int[] chrom = ind.chromosome;
            //InputHandler.inst.ThreadLog($"i-1: {bi-1}, c[i-1]: {chrom[bi-1]}, i:{bi}, c[i]:{chrom[bi]} || j:{bj}, c[j]: {chrom[bj]}, j+1: {bj+1}, c[j+1]: {chrom[bj+1]}");
            if(hasImproved) {
                tour.Reverse(bi, bj - bi + 1);
                float tmp = ComputeTourLength();
                ComputeCentroid();
                //InputHandler.inst.ThreadLog($"maxGain: {maxGain}, Tour: {ToString()}");
                //float evalFit = Evaluate(ind);
                //InputHandler.inst.ThreadLog(ind.ToString());
                //if(evalFit != newFit) {
                //    InputHandler.inst.ThreadLog($"Fitness mismatch: new: {newFit}, eval: {evalFit}");
                //    InputHandler.inst.ThreadLog(ind.ToString());
                //}
            }
            count++;
        }
        return oldTourLength - tourLength;
    }

    float GetDistance(int i, int j) {
        if(i < 0 || i >= tour.Count)
            return depotDistances[tour[j]];
        if(j < 0 || j >= tour.Count)
            return depotDistances[tour[i]];

        return distances[tour[i], tour[j]];

    }

}
