using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CVRPRouteCustomerHeuristics : ICVRPEvaluator {


    public CVRPData cvrpData;
    public float cMax = 10000;
    public float overCapacityPenalty = 10;
    public float distancePenalty = 10;

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
        return EvaluateRoutes(ind);
    }


    public float EvaluateRoutes(Individual ind) {
        float sum = 0;
        float maxTourLength = 0;

        foreach(Route route in ind.routes) {
            route.ComputeMetrics();
            sum += route.tourLength;
            if(route.tourLength > maxTourLength) {
                maxTourLength = route.tourLength;
            }
        }
        ind.unservedDemand = UnservedDemand(ind);
        ind.overCapacity = OverCapacity(ind);
        ind.sumRouteLengths = sum;
        ind.objectiveFunction = sum + ind.unservedDemand + (overCapacityPenalty * ind.overCapacity);
        ind.fitness = cMax - ind.objectiveFunction;
        return ind.fitness;
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
        return sum;
    }

    public float OverCapacity(Individual ind) {
        float sum = 0;
        foreach(Route route in ind.routes) {
            if(route.demand > cvrpData.vehicleCapacity) {
                sum += route.demand - cvrpData.vehicleCapacity;
            }
        }
        return sum;
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
            AddCustomerIndexToRoute(ind, route, customerIndex);            //ApplyCWHeuristic(ind, customerIndex, route, customerDemand);//          
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
            route.tourLength = route.ComputeTourLength();// GetTourLength(route);
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
            route.tourLength += 2 * cvrpData.depotDistances[customerIndex]; //single customer route
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

    public void NeighborMerge(Individual ind) {
        //TODO
        List<(Route, Route)> neighbors = new List<(Route, Route)>();
        foreach(Route route in ind.routes) {
            foreach(Route neighbor in ind.routes) {
                if(route != neighbor) {
                    if(IsNeighbor(ind, route, neighbor)) {
                        neighbors.Add((route, neighbor));
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
        foreach(Route route in ind.routes) {
            RouteNeighbors rn = new RouteNeighbors();
            rn.route = route;
            rn.neighbors = new List<Route>();
            foreach(Route neighbor in ind.routes) {
                if(route != neighbor) {
                    rn.neighbors.Add(neighbor);

                }
            }

            routeNeighbors.Add(rn);
        }
        return routeNeighbors;
    }
    public void NeighborSort(Individual ind) {




    }


    public Vector3 Centroid(Route route) {
        Vector3 sum = new Vector3(0, 0, 0);
        foreach(int cu in route.tour) {
            sum += (cvrpData.customers[cu].pos - cvrpData.depots[0].pos);
        }
        return sum / route.tour.Count;
    }

    public float CenteroidAngle(Vector3 centroid) {
        return Mathf.Atan2(centroid.z, centroid.x) * Mathf.Rad2Deg;

    }

    //-------------------------------local optimizers-------------------

    //------------------------------------------------------------------
    public float LocalOpt(Individual ind) {
        return BitHillClimber(ind);
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
        for(int i = 1; i < ind.bitChrom.Length; i++) {
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
        int ni = -1;
        int nj = -1;
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
