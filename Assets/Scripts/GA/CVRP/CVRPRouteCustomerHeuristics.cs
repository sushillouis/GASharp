using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CVRPRouteCustomerHeuristics : IEvaluator {


    public CVRPData cvrpData;

    public float cMax = 100000;
    public float overCapacityPenalty = 10;
    public float distancePenalty = 100;
    public float unservedDemandPenalty = 10;

    public int nHeuristicBits = 4;
    public int nHeuristicRankBits = 1;

    public int nRouteHeuristicBits = 2;
    public int nRouteRankBits = 1;

    public int nBits = -1; //testSum of the above

    public float routePrecision = 0;
    public float customerPrecision = 0;
    public CVRPRouteCustomerHeuristics(CVRPData data) {
        this.cvrpData = data;

        nHeuristicBits = 2;
        nRouteHeuristicBits = 2;

        nRouteRankBits = Mathf.CeilToInt(Mathf.Log(cvrpData.nVehicles, 2));
        routePrecision = (cvrpData.nVehicles-1) / (Mathf.Pow(2, nRouteRankBits) - 1);

        nHeuristicRankBits = Mathf.CeilToInt(Mathf.Log(cvrpData.nCustomers, 2));
        customerPrecision = (cvrpData.nCustomers - 1) / (Mathf.Pow(2, nHeuristicRankBits) - 1);

        nBits = nHeuristicBits + nHeuristicRankBits + nRouteHeuristicBits + nRouteRankBits;


    }
    public void Initialize(Individual ind) {
        CVRPUtils.InitRoutes(ind, cvrpData);
    }

    public int GetChromLength() {
        return cvrpData.nCustomers * nBits;
    }

    public float Evaluate(Individual ind) {
        Decode(ind);
        float tmp = EvaluateRoutes(ind);

        LK2All(ind);
        tmp = EvaluateRoutes(ind);

        //if(GARandom.inst.Flip(0.5f)) {
        OutlierNeighborSwap(ind);
        tmp = EvaluateRoutes(ind);
        //        }


        return tmp;

    }

    public void LK2All(Individual ind) {
        foreach(Route route in ind.routes) {
            if(route.tour.Count > 0) {
                route.LK2();
            }
        }
    }

    public float EvaluateRoutes(Individual ind) {
        float sum = 0;

        foreach(Route route in ind.routes) {
            route.ComputeMetrics();
            sum += route.tourLength;
        }

        ind.unservedDistance = UnservedDistance(ind);
        ind.unservedDemand = UnservedDemand(ind);
        ind.overCapacity = OverCapacity(ind);
        ind.sumRouteLengths = sum;
        ind.objectiveFunction = sum + ind.unservedDemand + ind.overCapacity + ind.unservedDistance;
        ind.fitness = cMax - ind.objectiveFunction;
        return ind.fitness;
    }

    public float RecomputeRouteMetrics(Individual ind) {
        float sum = 0;
        foreach(Route route in ind.routes) {
            route.ComputeMetrics();
            sum += route.tourLength;
        }
        return sum;
    }

    public float UnservedDistance(Individual ind) {
        float sum = 0;
        if(ind.unserved.Count > 0) { // penalize unserved customers
            int[] unservedArray = ind.unserved.ToArray();
            sum += cvrpData.depotDistances[unservedArray[0]];
            for(int i = 1; i < unservedArray.Length; i++) {
                sum += cvrpData.distances[unservedArray[i - 1], unservedArray[i]];
            }
            sum += cvrpData.depotDistances[unservedArray[unservedArray.Length - 1]];
            sum = distancePenalty * sum;
        }
        return sum;
    }

    public float UnservedDemand(Individual ind) {
        float sum = 0;
        foreach(int customerIndex in ind.unserved) {
            sum += cvrpData.customers[customerIndex].demand;
        }
        return sum * unservedDemandPenalty;
    }

    public float OverCapacity(Individual ind) {
        float sum = 0;
        foreach(Route route in ind.routes) {
            if(route.demand > cvrpData.vehicleCapacity) {
                sum += route.demand - cvrpData.vehicleCapacity;
            }
        }
        return sum * overCapacityPenalty;
    }



    public void Decode(Individual ind) {

        ResetRoutes(ind);
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
        routeRank = GAUtils.GetRR(tmp, cvrpData.nVehicles);
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
        RouteHeuristic rs = (RouteHeuristic) routeHeuristic;
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
        float customerDemand = cvrpData.customers[customerIndex].demand;


        if(cs == CustomerHeuristic.CWSavings) {
            ApplyCWHeuristic(ind, customerIndex, null, customerDemand);
        } else {
            ApplySortingHeuristic(ind, customerIndex, route, customerDemand);
        }

        //ind.available.RemoveAt(customerRank); //-----------------------------!!!!---------------
        ind.available.Remove(customerIndex);
    }

    void ApplySortingHeuristic(Individual ind, int customerIndex, Route route, float customerDemand) {

        if(customerDemand + route.demand <= cvrpData.vehicleCapacity) {
            AddCustomerIndexToRoute(ind, route, customerIndex);            //ApplyCWHeuristic(ind, customerIndex, outlierRoute, customerDemand);//          
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
            if(route.demand + customerDemand <= cvrpData.vehicleCapacity) {
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

    public (float, int) FindMaxSavingsIndex(Route route, int insertableCustomerIndex) {
        float savings = 0;
        float maxSavings = float.MinValue;
        int maxSavingsIndex = 0;

        if(route.tour.Count <= 0)
            return (-2 * cvrpData.depotDistances[insertableCustomerIndex], 0);

        for(int i = 0; i < route.tour.Count + 1; i++) {
            float newTourLength = route.tourLength;

            if(i == 0) {
                newTourLength -= cvrpData.depotDistances[route.tour[0]];
                newTourLength += cvrpData.depotDistances[insertableCustomerIndex];
                newTourLength += cvrpData.distances[route.tour[0], insertableCustomerIndex];
                savings = route.tourLength - newTourLength;
                if(savings > maxSavings) {
                    maxSavings = savings;
                    maxSavingsIndex = i;
                }
            } else if(i == route.tour.Count) {
                int endIndex = route.tour[route.tour.Count - 1];
                newTourLength -= cvrpData.depotDistances[endIndex];
                newTourLength += cvrpData.distances[endIndex, insertableCustomerIndex];
                newTourLength += cvrpData.depotDistances[insertableCustomerIndex];
                savings = route.tourLength - newTourLength;
                if(savings > maxSavings) {
                    maxSavings = savings;
                    maxSavingsIndex = i;
                }
            } else {
                newTourLength -= cvrpData.distances[route.tour[i - 1], route.tour[i]];
                newTourLength += cvrpData.distances[route.tour[i - 1], insertableCustomerIndex];
                newTourLength += cvrpData.distances[route.tour[i], insertableCustomerIndex];
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
            route.demand += cvrpData.customers[customerIndex].demand;
            route.tourLength = route.ComputeTourLength();// GetTourLength(outlierRoute);
            return true;
        }
    }


    public void AddCustomerIndexToRoute(Individual ind, Route route, int customerIndex) {

        if(route.tour.Count > 0) {
            int tourEndCustomerIndex = route.tour[route.tour.Count - 1];
            route.tourLength += cvrpData.distances[tourEndCustomerIndex, customerIndex];
            route.tourLength -= cvrpData.depotDistances[tourEndCustomerIndex];
            route.tourLength += cvrpData.depotDistances[customerIndex];
        } else {
            route.tourLength += 2 * cvrpData.depotDistances[customerIndex]; //single customer outlierRoute
        }
        route.demand += cvrpData.customers[customerIndex].demand;
        route.tour.Add(customerIndex);

    }


    public void InitLists(Individual ind) {
        ind.available.Clear();
        for(int i = 0; i < cvrpData.nCustomers; i++)
            ind.available.Add(i);
        ind.unserved.Clear();
    }

    public void ResetRoutes(Individual ind) {
        foreach(Route route in ind.routes)
            route.Reset();
    }


    public void CustomerIndexListSorter(List<int> indices, int customerIndex, CustomerHeuristic customerCompareType) {
        indices.Sort((a, b) => CustomerComparer(a, b, customerIndex, customerCompareType));

    }

    //public int GetCustomerIndexFromCid(Customer customer) {
    //    return customer.cid - 2;
    //}
    public int CustomerComparer(int ca, int cb, int customerIndex, CustomerHeuristic customerCompareType) {
        int result = 0;

        switch(customerCompareType) {
            case CustomerHeuristic.TourLength: //distance from from
                if(customerIndex >= 0)
                    result = cvrpData.distances[customerIndex, ca].CompareTo(cvrpData.distances[customerIndex, cb]);
                else
                    result = cvrpData.depotDistances[ca].CompareTo(cvrpData.depotDistances[cb]);
                break;
            case CustomerHeuristic.Demand:
                result = cvrpData.customers[ca].demand.CompareTo(cvrpData.customers[cb].demand);
                break;
            case CustomerHeuristic.Savings:
                result = (cvrpData.vehicleCapacity - cvrpData.customers[ca].demand).CompareTo(cvrpData.vehicleCapacity - cvrpData.customers[cb].demand);
                break;
            case CustomerHeuristic.CWSavings://not implemented yet, just compare customer indexes
                result = ca.CompareTo(cb);
                break;
            default:
                break;
        }
        if(result == 0)
            return ca.CompareTo(cb); //if two items are equal, sort by index
        else
            return result;           //return actual sort compare
    }

    public int RouteComparer(Route routeA, Route routeB, RouteHeuristic compareType) {
        int result = 0;
        switch(compareType) {
            case RouteHeuristic.TourLength:
                result = routeA.tourLength.CompareTo(routeB.tourLength);
                break;
            case RouteHeuristic.Demand:
                result = routeA.demand.CompareTo(routeB.demand);
                break;
            case RouteHeuristic.VehicleId:
                result = routeA.vid.CompareTo(routeB.vid);
                break;
            case RouteHeuristic.RemainingDemand:
                result = (cvrpData.vehicleCapacity - routeA.demand).CompareTo(cvrpData.vehicleCapacity - routeB.demand);
                break;
            default:
                break;
        }
        if(result == 0)
            return routeA.vid.CompareTo(routeB.vid);
        else
            return result;
    }

    public void RouteSort(RouteHeuristic compareType, List<Route> routes) {
        routes.Sort((a, b) => RouteComparer(a, b, compareType));
    }
    //--------------------------------------------------------------------------------


    //-------------------------------local optimizers-------------------

    /// <summary>
    /// Finds outlier customers in each outlierRoute and swaps them with the closest customer in another outlierRoute.
    /// </summary>
    /// <param name="ind"></param>
    public void OutlierNeighborSwap(Individual ind) {
        int outlierRouteIndex = -1;
        int outlierCustomerIndex = -1;
        int closestCustomerIndex = -1;
        int closestCustomerRouteIndex = -1;
        Route closestRoute = null;
        float oldFit = ind.fitness;
        float newFit = 0;

        ind.ResetSwapped();

        foreach(Route outlierRoute in ind.routes) {
            if(outlierRoute.tour.Count <= 0)
                continue;
            (outlierRouteIndex, outlierCustomerIndex) = outlierRoute.FindOutlier(ind.swapped);
            if(outlierRouteIndex == -1)
                continue;

            //InputHandler.inst.ThreadLog("Outlier: " + outlierCustomerIndex + ", route: " + outlierRoute.vid + ", index: " + outlierRouteIndex);

            (closestCustomerRouteIndex, closestCustomerIndex, closestRoute) = FindClosestCustomer(ind, outlierRoute, outlierCustomerIndex);

            //InputHandler.inst.ThreadLog("Closest: " + closestCustomerIndex + ", route: " + closestRoute.vid + ", index: " + closestCustomerRouteIndex);
            //InputHandler.inst.ThreadLog("Distance: " + cvrpData.distances[outlierCustomerIndex, closestCustomerIndex]);

            SwapCustomers(outlierRoute, outlierRouteIndex, outlierCustomerIndex, closestRoute, closestCustomerRouteIndex, closestCustomerIndex);

            newFit = EvaluateRoutes(ind);
            if(newFit < oldFit) {
                SwapCustomers(outlierRoute, outlierRouteIndex, closestCustomerIndex, closestRoute, closestCustomerRouteIndex, outlierCustomerIndex);
            } else {
                ind.swapped[closestCustomerIndex] = true;
                ind.swapped[outlierCustomerIndex] = true;
                //InputHandler.inst.ThreadLog("Swapped: " + outlierCustomerIndex + " with " + closestCustomerIndex + 
                //    ", deltaFit: " + (newFit-oldFit));
                oldFit = newFit;
                break;
            }

        }
    }

    public void SwapCustomers(Route outlierRoute, int outlierRouteIndex, int outlierCustomerIndex, 
        Route closestRoute, int closestRouteIndex, int closestCustomerIndex) {
        //swap between different routes
        //InputHandler.inst.ThreadLog(outlierRoute.ToString() + " |Before| " + closestRoute.ToString());
        outlierRoute.tour.RemoveAt(outlierRouteIndex);
        closestRoute.tour.RemoveAt(closestRouteIndex);

        outlierRoute.tour.Insert(outlierRouteIndex, closestCustomerIndex);
        closestRoute.tour.Insert(closestRouteIndex, outlierCustomerIndex);
        //InputHandler.inst.ThreadLog(outlierRoute.ToString() + " |After | " + closestRoute.ToString());

    }

    public (int, int, Route) FindClosestCustomer(Individual ind, Route route, int outlierCustomerIndex) {
        float minDistance = float.MaxValue;
        int closestCustomerIndex = -1;
        int closestRouteIndex = -1;
        Route closestRoute = null;
        foreach(Route otherRoute in ind.routes) {
            if(otherRoute == route)
                continue;
            int index = 0;
            foreach(int customerIndex in otherRoute.tour) {
                if(!ind.swapped[customerIndex]) {
                    float dist = cvrpData.distances[customerIndex, outlierCustomerIndex];
                    if(dist < minDistance) {
                        minDistance = dist;
                        closestCustomerIndex = customerIndex;
                        closestRouteIndex = index;
                        closestRoute = otherRoute;
                    }
                }
                index++;
            }
        }
        return (closestRouteIndex, closestCustomerIndex, closestRoute);
    }


    /// <summary>
    /// Swap outlier ci in outlierRoute i with cj in neighbor outlierRoute j in same positions. ci goes to cj's position, cj goes to ci's position.
    /// </summary>
    /// <param name="ind"></param>
    public void OutlierSavingsSwap(Individual ind) {


    }

    //------------------------------------------------------------------
    public float LocalOpt(Individual ind) {
        OutlierNeighborSwap(ind);
        return EvaluateRoutes(ind);

        //NeighborInsert(ind);
        //ConvertToBits(ind);
        //        return Evaluate(ind);

    }

    public float BitHillClimber(Individual ind) {
        float newFitness = ind.fitness;
        float maxFitness = ind.fitness;
        int maxIndex = 0;
        for(int i = 1; i < ind.bitChrom.Length; i++) {
            ind.bitChrom[i] = 1 - ind.bitChrom[i];
            newFitness = Evaluate(ind);

            if(newFitness >= maxFitness) {
                maxFitness = newFitness;
                maxIndex = i;
            } else {
                ind.bitChrom[i] = 1 - ind.bitChrom[i];
            }
        }

        newFitness = Evaluate(ind);

        return newFitness;

    }
    
    public int maxSHCIterations = 10;

    public float SHC1Bit(Individual ind) {
        float newFitness = ind.fitness;
        float maxFitness = ind.fitness;
        int maxIndex = 0;
        int index = 0;
        for(int i = 1; i < maxSHCIterations; i++) {
            index = GARandom.inst.RandInt(0, ind.bitChrom.Length);
            ind.bitChrom[index] = 1 - ind.bitChrom[index];
            newFitness = Evaluate(ind);
            if(newFitness >= maxFitness) {
                maxFitness = newFitness;
                maxIndex = i;
            } else {
                ind.bitChrom[index] = 1 - ind.bitChrom[index];
            }
        }

        newFitness = Evaluate(ind);

        return newFitness;
    }

    public float BSOptimizer(Individual ind) {

        float newFitness = ind.fitness;
        float maxFitness = ind.fitness;
        int maxIndex = 0;
        for(int i = 1; i < ind.bitChrom.Length; i++) {
            ind.bitChrom[i] = 1 - ind.bitChrom[i];
            newFitness = Evaluate(ind);
            ind.bitChrom[i] = 1 - ind.bitChrom[i];
            if(newFitness >= maxFitness) {
                maxFitness = newFitness;
                maxIndex = i;
            }
        }
        ind.bitChrom[maxIndex] = 1 - ind.bitChrom[maxIndex];
        newFitness = Evaluate(ind);

        return newFitness;
    }

    public int maxBSOIterations = 10;

    public float BSOMulti(Individual ind) {
        float newFit = ind.fitness;
        float maxFit = ind.fitness;
        bool hasImproved = true;
        int count = 0;
        while(hasImproved && count++ < maxBSOIterations) {
            hasImproved = false;
            newFit = BSOptimizer(ind);
            if(newFit > maxFit) {
                maxFit = newFit;
                hasImproved = true;
            }
        }

        return maxFit;
    }
    public float BSO2Opt(Individual ind) {
        int oi = -1;
        int oj = -1;
        //int ni = -1;
        //int nj = -1;
        int bestI = 0;
        int bestJ = 1;
        (int, int) bestPair = (0, 0);
        (int, int)[] pairs = new (int, int)[4];
        float maxFit = ind.fitness;
        float newFit = ind.fitness;
        for(int i = 0; i < ind.bitChrom.Length; i++) {
            for(int j = i + 1; j < ind.bitChrom.Length; j++) {
                oi = ind.bitChrom[i];
                oj = ind.bitChrom[j];
                pairs = GenPairs(ind, oi, oj);
                for(int k = 0; k < pairs.Length; k++) {
                    ind.bitChrom[i] = pairs[k].Item1;
                    ind.bitChrom[j] = pairs[k].Item2;
                    newFit = Evaluate(ind);
                    if(newFit >= maxFit) {
                        maxFit = newFit;
                        ind.bitChrom[i] = oi;
                        ind.bitChrom[j] = oj;
                        bestI = i;
                        bestJ = j;
                        bestPair = pairs[k];
                    }
                }
            }
        }
        if(maxFit > ind.fitness) {
            ind.bitChrom[bestI] = bestPair.Item1;
            ind.bitChrom[bestJ] = bestPair.Item2;
            newFit = Evaluate(ind);
        }
        return newFit;
    }

    public (int, int)[] GenPairs(Individual ind, int oi, int oj) {
        List<(int, int)> pairs = new List<(int, int)>();
        pairs.Add((0, 0));
        pairs.Add((0, 1)); 
        pairs.Add((1, 0));
        pairs.Add((1, 1));

        for(int k = 0; k < pairs.Count; k++) {
            if(pairs[k].Item1 == oi && pairs[k].Item2 == oj) {
                pairs.RemoveAt(k);
                break;
            }
        }

        return pairs.ToArray();
    }
}


/*
 * 


    public void NeighborMerge(Individual ind) {
        //TODO
        List<(Route, Route)> neighbors = new List<(Route, Route)>();
        foreach(Route outlierRoute in ind.routes) {
            foreach(Route neighbor in ind.routes) {
                if(outlierRoute != neighbor) {
                    if(IsNeighbor(ind, outlierRoute, neighbor)) {
                        neighbors.Add((outlierRoute, neighbor));
                    }
                }
            }
        }
    }

    public bool IsNeighbor(Individual ind, Route baseRoute, Route b) {



        return false;
    }

    public List<RouteNeighbors> GetRouteNeighbors(Individual ind) {
        List<RouteNeighbors> routeNeighbors = new List<RouteNeighbors>();
        foreach(Route outlierRoute in ind.routes) {
            RouteNeighbors rn = new RouteNeighbors();
            rn.outlierRoute = outlierRoute;
            rn.neighbors = new List<Route>();
            foreach(Route neighbor in ind.routes) {
                if(outlierRoute != neighbor) {
                    rn.neighbors.Add(neighbor);

                }
            }

            routeNeighbors.Add(rn);
        }
        return routeNeighbors;
    }
    public void NeighborSort(Individual ind) {




    }


    public Vector3 Centroid(Route outlierRoute) {
        Vector3 sum = new Vector3(0, 0, 0);
        foreach(int cu in outlierRoute.tour) {
            sum += (cvrpData.customers[cu].pos - cvrpData.depots[0].pos);
        }
        return sum / outlierRoute.tour.Count;
    }

    public float CenteroidAngle(Vector3 centroid) {
        return Mathf.Atan2(centroid.z, centroid.x) * Mathf.Rad2Deg;

    }


    public void NeighborInsert(Individual ind) {
        foreach(Route outlierRoute in ind.routes) {
            outlierRoute.ComputeMetrics();
        }
        //------------------------------------
        foreach(Route outlierRoute in ind.routes) {
            Route neighbor = FindNeighbor(ind, outlierRoute);
            InsertMaxSavings(outlierRoute, neighbor);
        }
    }

    public Route FindNeighbor(Individual ind, Route outlierRoute) {
        float minDist = float.MaxValue;
        Route minRoute = null;
        foreach(Route other in ind.routes) {
            if(other == outlierRoute)
                continue;
            float dist = Vector3.Distance(outlierRoute.centroid, other.centroid);
            if(dist < minDist) {
                minDist = dist;
                minRoute = other;
            }
        }
        return minRoute;
    }

    void InsertMaxSavings(Route outlierRoute, Route neighbor) {
        float maxSavings = 0;
        int customerIndex = -1;
        int index = 0;
        float savings = 0;
        int insertAtIndex = -1;
        foreach(int ci in outlierRoute.tour) {
            (savings, insertAtIndex) = FindMaxSavingsIndex(neighbor, ci);
            if(savings > maxSavings) {
                maxSavings = savings;
                customerIndex = ci;
            }
            index++;
        }
        if(customerIndex >= 0 && insertAtIndex >= 0) {
            InsertIntoNeighbor(outlierRoute, neighbor, customerIndex, insertAtIndex);
        }

    }

    void InsertIntoNeighbor(Route outlierRoute, Route neighbor, int customerIndex, int insertAtIndex) {
        //Insert customerIndex into neighbor at insertAtIndex
        outlierRoute.tour.Remove(customerIndex);
        outlierRoute.ComputeMetrics();
        neighbor.tour.Insert(insertAtIndex, customerIndex);
        neighbor.ComputeMetrics();
    }

    void ConvertToBits(Individual ind) {
        int[] newChrom = new int[ind.bitChrom.Length];
        int start = 0;
        List<Route> newRoutes = CVRPUtils.CreateRoutes(cvrpData);

        foreach(Route outlierRoute in ind.routes) {
            int nbits = ConvertRouteToBits(ind, outlierRoute, newChrom, start, newRoutes);
            start += nbits;
        }
    }

    int ConvertRouteToBits(Individual ind, Route outlierRoute, int[] newChrom, int start, List<Route> newRoutes) {
        int[] routeBits;
        int[] customerHeuristicBits;
        int routeHeuristic = -1;
        int routeRank = -1;
        int customerHeuristic = -1;
        int customerRank = -1;
        for(int i = 0; i < ind.bitChrom.Length; i += nBits) {
            (routeHeuristic, routeRank, customerHeuristic, customerRank) = GetHeuristicsAndRanks(ind, i);

        }


        return outlierRoute.tour.Count * nBits;

    }


 * 
 */