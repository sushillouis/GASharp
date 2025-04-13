using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CVRPUtils{

    public static void InitRoutes(Individual ind, CVRPData cvrpData) {
        ind.routes.Clear();
        for(int i = 0; i < cvrpData.nVehicles; i++) {
            Route route = new Route(cvrpData.distances, cvrpData.depotDistances, cvrpData.customers, cvrpData.depots.ToList());
            route.vid = i;
            ind.routes.Add(route);
        }
    }

    public static List<Route> CreateRoutes(CVRPData cvrpData) {
        List<Route> routes = new List<Route>();
        for(int i = 0; i < cvrpData.nVehicles; i++) {
            Route route = new Route(cvrpData.distances, cvrpData.depotDistances, cvrpData.customers, cvrpData.depots.ToList());
            route.vid = i;
            routes.Add(route);
        }
        return routes;
    }



}
