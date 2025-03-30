using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum RouteSorter {
    TourLength = 0,
    Demand,
    VehicleId,
    RemainingDemand,
}

[Serializable]
public enum CustomerHeuristic {
    TourLength = 0,
    Demand,
    Savings, // for each remaining city insert into route at all positions and see which city has the most savings
    CWSavings,
}

[Serializable]
public class MetaCVRPEvaluator : CVRPEvaluator {
    public int nHeuristicBits = 4;
    public int nHeuristicRankBits = 1;

    public int nRouteHeuristicBits = 2;
    public int nRouteRankBits = 1;

    public int nBits = -1; //sum of the above

    public float routePrecision = 0;
    public float customerPrecision = 0;


    public override void Init() {
        base.Init();

        nHeuristicBits = 2;
        nRouteHeuristicBits = 2;
        nRouteRankBits = Mathf.CeilToInt(Mathf.Log(nVehicles, 2));
        routePrecision = nVehicles / Mathf.Pow(2, nRouteRankBits);
        nHeuristicRankBits = Mathf.CeilToInt(Mathf.Log(nCustomers, 2));
        customerPrecision = nCustomers / Mathf.Pow(2, nHeuristicRankBits);
        nBits = nHeuristicBits + nHeuristicRankBits + nRouteHeuristicBits + nRouteRankBits;

    }

    public override void DecodeToRoutes(Individual ind) {
        InitRoutes(ind);
        InitLists(ind);
        int routeHeuristic = -1;
        int routeRank = -1;
        int customerHeuristic = -1;
        int customerRank = -1;
        for(int i = 0; i < ind.parameters.bitChromLength; i += nBits) {
            (routeHeuristic, routeRank, customerHeuristic, customerRank) = GetHeuristicsAndRanks(ind, i);
            ApplyHeuristics(ind, routeHeuristic, routeRank, customerHeuristic, customerRank);
        }
    }

    (int, int, int, int) GetHeuristicsAndRanks(Individual ind, int start) {
        int startIndex = start;
        int routeHeuristic = -1;
        int routeRank = -1;
        int tmp = -1;
        int customerHeuristic = -1;
        int customerRank = -1;

        routeHeuristic = GAUtils.Decode(ind.bitChrom, startIndex, nRouteHeuristicBits);
        startIndex += nRouteHeuristicBits;
        tmp = GAUtils.GetDecodeValue(GAUtils.Decode(ind.bitChrom, startIndex, nRouteRankBits), 0, routePrecision);
        routeRank = GAUtils.GetRR(tmp, nVehicles);
        startIndex += nRouteRankBits;

        customerHeuristic = GAUtils.Decode(ind.bitChrom, startIndex, nHeuristicBits);
        startIndex += nHeuristicBits;
        tmp = GAUtils.GetDecodeValue(GAUtils.Decode(ind.bitChrom, startIndex, nHeuristicRankBits), 0, customerPrecision);
        customerRank = GAUtils.GetRR(tmp, ind.available.Count);

        return (routeHeuristic, routeRank, customerHeuristic, customerRank);
    }

    void ApplyHeuristics(Individual ind, int routeHeuristic, int routeRank, int customerHeuristic, int customerRank) {
        Route route = ApplyRouteHeuristic(ind, routeHeuristic, routeRank); //first
        if(route == null) {
            InputHandler.inst.ThreadLog($"ERROR: apply route heuristic: {routeHeuristic}, rank: {routeRank}\n" + ind.ToString());
        }
        ApplyCustomerHeuristic(ind, route, customerHeuristic, customerRank);
    }

    Route ApplyRouteHeuristic(Individual ind, int routeHeuristic, int routeRank) {
        RouteSorter rs = (RouteSorter) routeHeuristic;
        ind.routes.Sort((a, b) => RouteComparer(a, b, rs));
        //RouteSort(rs, ind.routes);
        if(ind.routes.Count > routeRank)
            return ind.routes[routeRank];
        else
            return null;
    }

    void ApplyCustomerHeuristic(Individual ind, Route route, int customerHeuristic, int customerRank) {
        CustomerHeuristic cs = (CustomerHeuristic) customerHeuristic;
        int customerIndex = GetCustomerIndex(ind, route, cs, customerRank);
        float customerDemand = customers[customerIndex].demand;


        if(cs == CustomerHeuristic.CWSavings) {
            ApplyCWHeuristic(ind, customerIndex, null, customerDemand);
        } else {
            ApplySortingHeuristic(ind, customerIndex, route, customerDemand);
        }

        //ind.available.RemoveAt(customerRank); //-----------------------------!!!!---------------
        ind.available.Remove(customerIndex);
    }

    void ApplySortingHeuristic(Individual ind, int customerIndex, Route route, float customerDemand) {

        if(customerDemand + route.demand <= vehicleCapacity) {
            AddCustomerIndexToRoute(ind, route, customerIndex);
        } else {
            ApplyCWHeuristic(ind, customerIndex, route, customerDemand);
        }
    }

    void ApplyCWHeuristic(Individual ind, int customerIndex, Route route, float customerDemand) {
        (Route chosenRoute, int routeIndex) = FindSavingsRoute(ind, route, customerIndex, customerDemand);
        bool success = InsertCustomerIndexToRoute(ind, chosenRoute, customerIndex, routeIndex);
        if(!success)
            ind.unserved.Add(customerIndex);


    }

    int GetCustomerIndex(Individual ind, Route route, CustomerHeuristic cs, int customerRank) {
        int ci = ind.available[0]; // CWSavings
        if(cs != CustomerHeuristic.CWSavings) {
            if(route.tour.Count > 0) {
                int fromIndex = route.tour[route.tour.Count - 1];
                CustomerIndexListSorter(ind.available, fromIndex, cs);
            } else {
                CustomerIndexListSorter(ind.available, -1, cs);
            }
            ci = ind.available[customerRank];//customerRank is guaranteed to be less than ind.available.Count            
        }
        return ci;
    }

    (Route, int) FindSavingsRoute(Individual ind, Route badRoute, int customerIndex, float customerDemand) {
        Route bestRoute = null;
        float savings;
        int savingsIndex;
        float maxSavings = float.MinValue;
        int maxSavingsIndex = -1;
        foreach(Route route in ind.routes) {
            if(badRoute != null && route == badRoute)
                continue;
            if(route.demand + customerDemand <= vehicleCapacity) {
                (savings, savingsIndex) = FindMaxSavingsIndex(route, customerIndex);
                if(savings > maxSavings) {
                    maxSavings = savings;
                    maxSavingsIndex = savingsIndex;
                    bestRoute = route;
                }
            }
        }
        return (bestRoute, maxSavingsIndex);
    }

    (float, int) FindMaxSavingsIndex(Route route, int insertableCustomerIndex) {
        float savings = 0;
        float maxSavings = float.MinValue;
        int maxSavingsIndex = 0;

        if(route.tour.Count <= 0)
            return (-2 * depotDistances[insertableCustomerIndex], 0);

        for(int i = 0; i < route.tour.Count + 1; i++) {
            float newTourLength = route.tourLength;

            if(i == 0) {
                newTourLength -= depotDistances[route.tour[0]];
                newTourLength += depotDistances[insertableCustomerIndex];
                newTourLength += distances[route.tour[0], insertableCustomerIndex];
                savings = route.tourLength - newTourLength;
                if(savings > maxSavings) {
                    maxSavings = savings;
                    maxSavingsIndex = i;
                }
            } else if(i == route.tour.Count) {
                int endIndex = route.tour[route.tour.Count - 1];
                newTourLength -= depotDistances[endIndex];
                newTourLength += distances[endIndex, insertableCustomerIndex];
                newTourLength += depotDistances[insertableCustomerIndex];
                savings = route.tourLength - newTourLength;
                if(savings > maxSavings) {
                    maxSavings = savings;
                    maxSavingsIndex = i;
                }
            } else {
                newTourLength -= distances[route.tour[i - 1], route.tour[i]];
                newTourLength += distances[route.tour[i - 1], insertableCustomerIndex];
                newTourLength += distances[route.tour[i], insertableCustomerIndex];
                savings = route.tourLength - newTourLength;
                if(savings > maxSavings) {
                    maxSavings = savings;
                    maxSavingsIndex = i;
                }
            }
        }


        return (maxSavings, maxSavingsIndex);
    }

    public bool InsertCustomerIndexToRoute(Individual ind, Route route, int customerIndex, int routeIndex) {
        if(route == null) {
            //InputHandler.inst.ThreadLog("Cannot add ci: " + customerIndex + ", ri: " + routeIndex + " to \n" + ind.ToString());
            return false;
        } else {
            route.tour.Insert(routeIndex, customerIndex);
            route.demand += customers[customerIndex].demand;
            route.tourLength = GetTourLength(route);
            return true;
        }
    }

    float GetTourLength(Route route) {
        float tourLength = depotDistances[route.tour[0]];
        for(int i = 1; i < route.tour.Count; i++)
            tourLength += distances[route.tour[i - 1], route.tour[i]];
        tourLength += depotDistances[route.tour[route.tour.Count - 1]];
        return tourLength;
    }

    public void AddCustomerIndexToRoute(Individual ind, Route route, int customerIndex) {

        if(route.tour.Count > 0) {
            int tourEndCustomerIndex = route.tour[route.tour.Count - 1];
            route.tourLength += distances[tourEndCustomerIndex, customerIndex];
            route.tourLength -= depotDistances[tourEndCustomerIndex];
            route.tourLength += depotDistances[customerIndex];
        } else {
            route.tourLength += 2 * depotDistances[customerIndex]; //single customer route
        }
        route.demand += customers[customerIndex].demand;
        route.tour.Add(customerIndex);

    }


    public void InitLists(Individual ind) {
        ind.available.Clear();
        for(int i = 0; i < nCustomers; i++)
            ind.available.Add(i);
        ind.unserved.Clear();
    }

    public void InitRoutes(Individual ind) {
        ind.routes.Clear();
        for(int i = 0; i < nVehicles; i++) {
            Route route = new Route();
            route.demand = route.tourLength = 0;
            route.vid = i;
            route.tour.Clear();
            ind.routes.Add(route);
        }
    }


    public void CustomerIndexListSorter(List<int> indices, int customerIndex, CustomerHeuristic customerCompareType) {
        indices.Sort((a, b) => CustomerComparer(a, b, customerIndex, customerCompareType));

    }

    public int GetCustomerIndexFromCid(Customer customer) {
        return customer.cid - 2;
    }
    public int CustomerComparer(int ca, int cb, int customerIndex, CustomerHeuristic customerCompareType) {
        int result = 0;

        switch(customerCompareType) {
            case CustomerHeuristic.TourLength: //distance from from
                if(customerIndex >= 0)
                    result = distances[customerIndex, ca].CompareTo(distances[customerIndex, cb]);
                else
                    result = depotDistances[ca].CompareTo(depotDistances[cb]);
                break;
            case CustomerHeuristic.Demand:
                result = customers[ca].demand.CompareTo(customers[cb].demand);
                break;
            case CustomerHeuristic.Savings:
                result = (vehicleCapacity - customers[ca].demand).CompareTo(vehicleCapacity - customers[cb].demand);
                break;
            case CustomerHeuristic.CWSavings:
                result = ca.CompareTo(cb);
                break;
            default:
                break;
        }
        if(result == 0)
            return ca.CompareTo(cb);
        else
            return result;
    }

    public int RouteComparer(Route routeA, Route routeB, RouteSorter compareType) {
        int result = 0;
        switch(compareType) {
            case RouteSorter.TourLength:
                result = routeA.tourLength.CompareTo(routeB.tourLength);
                break;
            case RouteSorter.Demand:
                result = routeA.demand.CompareTo(routeB.demand);
                break;
            case RouteSorter.VehicleId:
                result = routeA.vid.CompareTo(routeB.vid);
                break;
            case RouteSorter.RemainingDemand:
                result = (vehicleCapacity - routeB.demand).CompareTo(vehicleCapacity - routeA.demand);
                break;
            default:
                break;
        }
        if(result == 0)
            return routeA.vid.CompareTo(routeB.vid);
        else
            return result;
    }

    public void RouteSort(RouteSorter compareType, List<Route> routes) {
        routes.Sort((a, b) => RouteComparer(a, b, compareType));
    }




    //--------------------------------------------------------------------------------
    public override float LocalOpt(Individual ind) {
        //return BSO(ind);
        return SHC(ind);
        //return ind.fitness; // do nothing
    }

    public int maxCount = 5;
    public float BSO(Individual ind) {
        float oldFit = Evaluate(ind);
        float newFit = -1;
        bool hasImproved = true;
        int count = 0;

        float maxFit = 0;
        int maxFitChromIndex = -1;
        float lastBestFit = oldFit;
        while(hasImproved == true && count++ < maxCount) {
            for(int i = 0; i < ind.parameters.bitChromLength; i++) {
                hasImproved = false;
                ind.bitChrom[i] = 1 - ind.bitChrom[i]; //flip
                newFit = Evaluate(ind);
                if(newFit >= oldFit) { //keep track of the best flip
                    hasImproved = true;
                    oldFit = newFit;
                    maxFit = newFit;
                    maxFitChromIndex = i;
                    lastBestFit = maxFit;
                } else {
                    ind.bitChrom[i] = 1 - ind.bitChrom[i]; //flip back}

                }
                if(hasImproved) {
                    maxFit = 0;
                    maxFitChromIndex = -1;
                }
            }
        }
        float fit = Evaluate(ind);//Have to re-evaluate since routes may have
        return fit;               //changed during evaluations that do not improve fitness

    }

    public int maxSHCIterations = 40;
    public float SHC(Individual ind) { // simple randomized hill climber
        maxSHCIterations = ind.parameters.bitChromLength;
        float oldFit = Evaluate(ind);
        float oldObj = ind.objectiveFunction;
        float newFit = -1;

        for(int i = 0; i < maxSHCIterations; i++) {
            int flipIndex = GARandom.inst.RandInt(0, ind.parameters.bitChromLength);
            ind.bitChrom[flipIndex] = 1 - ind.bitChrom[flipIndex]; //flip
            newFit = Evaluate(ind);
            if(newFit >= oldFit) {
                oldFit = newFit; // keep the flip, and ratchet up fitness
            } else {
                ind.bitChrom[flipIndex] = 1 - ind.bitChrom[flipIndex]; //flip back

            }
        }

        float fit = Evaluate(ind);//Have to re-evaluate since routes may have
        return fit;               //changed during evaluations that do not improve fitness

    }

}



/*
 * 
 * 
 
    public void RoutesToSequence(Individual ind) {

        int i = 0;
        int nCust = 0;
        foreach(Route route in ind.routes) {
            foreach(int cu in route.tour) {
                ind.seqChrom[i++] = cu;
            }
            nCust += route.tour.Count;
        }
        foreach(int cu in ind.unserved) {
            ind.seqChrom[i++] = cu;
        }
        nCust += ind.unserved.Count;

        if(nCust != nCustomers) {
            InputHandler.inst.ThreadLog("Error: Cannot convert routes to chrom");
            InputHandler.inst.ThreadLog(ind.ToString());
        }

    }

  
 List<Route> InitRoutes() {
        List<Route> routes = new List<Route>();
        for(int i = 0; i < nVehicles; i++) {
            Route route = new Route();
            route.vid = i;
            route.demand = 0;
            route.tour.Clear();
            route.tourLength = 0;
            routes.Add(route);
        }
        return routes;
    }

    int CompareDistancesAndIndex(int a, int b, int last) {

        int result = distances[last, a].CompareTo(distances[last, b]);
        if(result == 0)
            return a.CompareTo(b);
        else
            return result;
    }

    int CompareDepotDistances(int a, int b) {
        int result = depotDistances[a].CompareTo(depotDistances[b]);
        if(result == 0)
            return a.CompareTo(b);
        else
            return result;
    }

    public List<Route> tmpRoutes = new List<Route>();
    public List<int> tmpWorkingList = new List<int>();
    public void SeqToBits2(Individual ind) {
        int[] bitChrom = new int[ind.parameters.bitChromLength];
        tmpWorkingList.Clear();
        for(int i = 0; i < ind.parameters.seqChromLength; i++) 
            tmpWorkingList.Add(i);
        tmpRoutes.Clear();
        tmpRoutes = InitRoutes();

        int bitIndex = 0;
        int currentCustomerIndex = -1;
        int heuristicRankedIndex = -1;
        foreach(Route route in ind.routes) {
            if(route.tour.Count > 0) {
                for(int i = 0; i < route.tour.Count; i++) {
                    if(i == 0) {
                        tmpWorkingList.Sort(CompareDepotDistances);
                        //InputHandler.inst.ThreadLog($"R{route.vid}: " + string.Join(", ", tmpWorkingList));
                    } else {
                        int last = route.tour[i - 1];
                        tmpWorkingList.Sort((a, b) => CompareDistancesAndIndex(a, b, last));
                        //InputHandler.inst.ThreadLog($"R{route.vid}: " + string.Join(", ", tmpWorkingList));
                    }
                    currentCustomerIndex = route.tour[i];
                    heuristicRankedIndex = tmpWorkingList.FindIndex(x => x == currentCustomerIndex);
                    if(heuristicRankedIndex >= 0) {
                        tmpWorkingList.RemoveAt(heuristicRankedIndex);
                        int[] bits = GAUtils.Encode(heuristicRankedIndex, nHeuristicBits);
                        for(int j = 0; j < bits.Length; j++) {
                            bitChrom[bitIndex++] = bits[j];
                        }

                    } else {
                        InputHandler.inst.ThreadLog("ERROR: Cannot find: " + currentCustomerIndex + " in: " + string.Join(", ", tmpWorkingList));
                    }

                }
            } else {
                InputHandler.inst.ThreadLog("ERROR: EMPTY SeqTour: " + route.ToString());
            }
        }
        ind.bitChrom = bitChrom;
    }

 * 
 * 
            for(int i = 0; i < nVehicles; i++) {
                Route route = new Route();
                route.vid = i;
                route.customerDemand = 0;
                route.tour.Clear();
                route.newTourLength = 0;
                ind.routes.Add(route);
            }



    public int GetRR(List<int> available, int k) {
        int max = available.Count;
        if(max <= 1) return 0;
        return (k < max ? k : k % max);
    }

    public int GetIndex(List<int> available, int k) {
        int n = available.Count;
        float fraction = (float) k/ 16;
        int index = Mathf.RoundToInt(fraction * (n - 1));
        return index;
    }


 * 
 * 
 * 
 * 
PLK 
13, 14, 8, 6, 4, 9, dem: 5200 || 16, 15, 7, 5, 0, 1, dem: 5400 || 11, 2, 3, 10, 12, dem: 6000 || 17, 19, 20, 18, dem: 5900 |

Error
13, 14, 8, 6, 4, 9, 7, 5, dem: 5700 || 19, 16, 20, 18, dem: 6000 || 15, 1, 0, 2, 10, dem: 5900 || 11, 17, 12, 3, dem: 4900 |


    public override void DecodeToRoutes(Individual ind) {
        if(isSeqEval) {
            base.DecodeToRoutes(ind);
        } else {
            ind.available.Clear();
            ind.workingList.Clear();
            ind.unserved.Clear();
            ind.routes.Clear();
            ind.routes = InitRoutes();
            for(int i = 0; i < nCustomers; i++) {
                ind.available.Add(i);
                ind.workingList.Add(i);
            }
            for(int i = 0; i < ind.parameters.bitChromLength; i += nHeuristicBits) {
                int k = GAUtils.Decode(ind.bitChrom, i, nHeuristicBits);

                bool didAddCustomer = KthNearest(ind, k);
                //InputHandler.inst.ThreadLog("Decoded Val: " + val + ", added? " + didAddCustomer);
            }
            ind.unserved.AddRange(ind.workingList);
        }
    }

    public void UpdateRouteLengths(List<Route> routes) {
        foreach(Route route in routes) {
            route.newTourLength = 0;
            if(route.tour.Count > 0) {
                route.newTourLength += depotDistances[route.tour[0]];
                for(int i = 1; i < route.tour.Count; i++) {
                    route.newTourLength += distances[route.tour[i - 1], route.tour[i]];
                }
                route.newTourLength += depotDistances[route.tour[route.tour.Count - 1]];
            }
        }
    }
    public bool KthNearest(Individual ind, int k) {
        bool didAddCustomer = false;
        UpdateRouteLengths(ind.routes);
        ind.routes.Sort((a, b) => a.newTourLength.CompareTo(b.newTourLength));
        //InputHandler.inst.ThreadLog("All: \n" + string.Join("\n", ind.routes));
        foreach(Route route in ind.routes) {
            //InputHandler.inst.ThreadLog("Shortest: " + route.ToString());
            if(route.customerDemand < vehicleCapacity) {
                if(route.tour.Count <= 0) {
                    ind.workingList.Sort(CompareDepotDistances);
                    //InputHandler.inst.ThreadLog($"K{route.tour.Count}: " + string.Join(", ", ind.workingList));
                } else {
                    int last = route.tour[route.tour.Count - 1];
                    ind.workingList.Sort((a, b) => CompareDistancesAndIndex(a, b, last));

                    //InputHandler.inst.ThreadLog($"K{route.tour.Count}: " + string.Join(", ", ind.workingList));
                }
                //int actualK = GetIndex(ind.workingList, k); //Mathf.Clamp(n, 0, ind.available.Count - 1);
                int actualK = GetRR(ind.workingList, k);
                //InputHandler.inst.ThreadLog($"k: {k}, Actualk: {actualK}");
                //InputHandler.inst.ThreadLog("Count: " + ind.workingList.Count + ", k: " + k + ", actual: " + actualK);
                int cust = ind.workingList[actualK];
                ind.workingList.RemoveAt(actualK);
                ind.workingList.Insert(0, cust);
                foreach(int cu in ind.workingList) {
                    if(customers[cu].customerDemand + route.customerDemand <= vehicleCapacity) {
                        route.tour.Add(cu);
                        route.customerDemand += customers[cu].customerDemand;
                        ind.workingList.Remove(cu);
                        didAddCustomer = true;

                        break;
                    }
                }
                if(didAddCustomer) {
                    break;
                }

            }
        }
        return didAddCustomer;
    }


    bool isSeqEval = false;
    public override float LocalOpt(Individual ind) {

        BitsToSeq(ind);
        isSeqEval = true;
        LK2CVRP(ind);
        float lk2Fit = Evaluate(ind);//        ind.fitness;
        float lk2Obj = ind.objectiveFunction;

        //InputHandler.inst.ThreadLog("PLK2: \n" + ind.ToString());
        SeqToBits2(ind);
        //isSeqEval = false;
        float fit = Evaluate(ind);

        //if(fit != lk2Fit )
        //InputHandler.inst.ThreadLog("ERROR: \n" + ind.ToString());

        return fit;

    }

    public void BitsToSeq(Individual ind) {
        DecodeToRoutes(ind);
        RoutesToSequence(ind);
    }

    public void SeqToBits(Individual ind) {
        int[] bitChrom = new int[ind.parameters.bitChromLength];
        ind.workingList.Clear();
        for(int i = 0; i < nCustomers; i++) {
            ind.workingList.Add(i);
        }
        tmpRoutes.Clear();
        tmpRoutes = InitRoutes();
        Route currentRoute;
        int seqIndex = 0;
        ind.workingList.Sort((a, b) => depotDistances[a].CompareTo(depotDistances[b]));
        for(int bitIndex = 0; bitIndex < ind.bitChrom.Length; bitIndex += nHeuristicBits) {//sequence of customers
            //UpdateRouteLengths(tmpRoutes);
            tmpRoutes.Sort((a, b) => a.tourLength.CompareTo(b.tourLength));
            currentRoute = tmpRoutes[0];
            if(currentRoute.tour.Count <= 0) {
                ind.workingList.Sort((a, b) => depotDistances[a].CompareTo(depotDistances[b]));
            } else {
                int last = currentRoute.tour[currentRoute.tour.Count - 1];
                ind.workingList.Sort((a, b) => distances[last, a].CompareTo(distances[last, b]));
            }
            int customerIndex = ind.seqChrom[seqIndex++];

            currentRoute.tour.Add(customerIndex);
            currentRoute.demand += customers[customerIndex].demand;


            int heuristic = ind.workingList.FindIndex(x => x == customerIndex);
            if(heuristic < 0)
                InputHandler.inst.ThreadLog("ERROR: cannot find " + customerIndex + " in " + string.Join(", ", ind.workingList));
            ind.workingList.RemoveAt(heuristic);

            int[] bits = GAUtils.Encode(heuristic, nHeuristicBits);
            for(int j = 0; j < nHeuristicBits; j++) {
                bitChrom[bitIndex + j] = bits[j];
            }
        }
        ind.bitChrom = bitChrom;
    }


 * 
 */