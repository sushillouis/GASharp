using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;


[Serializable]
public class Customer {
    public Vector3 pos;
    public float demand;
    public int cid;
}

[Serializable]
public class DistanceList {
    public int index;
    public List<float> distances = new List<float>();
}

[Serializable]
public class HeuristicsList {
    public int routeHerisitic;
    public int routeRank;
    public int customerHeuristic;
    public int customerRank;
}

[Serializable]
public class SwapTrackList {
    

}

[Serializable]
public class CVRPData {
    public string problemFilenames = "E-n22-k4.vrp E-n30-k3.vrp F-n45-k4.vrp F-n72-k4.vrp F-n135-k7.vrp M-n101-k10.vrp X-n101-k25.vrp X-n167-k10.vrp X-n573-k30.vrp";
    public string problemFilename = "E-n22-k4.vrp";
    public string problemName = "E-n22-k4";
    public string problemDescription = "E-n22-k4";

    public CVRPRepresentationType representationType = CVRPRepresentationType.RouteCityHeuristics;

    public List<DistanceList> distancesList = new List<DistanceList>();

    public float[,] distances;
    public float[] depotDistances;
    public int nVehicles;
    public int nCustomers;
    public Customer[] customers;
    public Customer[] depots;
    public List<int> depotIds = new List<int>();
    public int[] nearest;

    public Vector3 depotPosition = Vector3.zero;

    public float totalCustomerDemand = 0;
    public float vehicleCapacity = 0;


    public CVRPData() {

    }
    //public void LoadData(string filename, CVRPRepresentationType repType) {
    public void LoadData(CVRPProblem cvrpProblem) {
        this.problemFilename = cvrpProblem.cvrpData.problemFilename;
        this.representationType = cvrpProblem.cvrpData.representationType;
        isDataLoaded = false;
        InputHandler.inst.StartCoroutine(ReadCoroutine(problemFilename));
    }

    public List<string> GetProblemNames() {
        string[] tmp = problemFilenames.Split();
        problemFilename = tmp[0];
        return tmp.ToList();
    }

    public void ConvertDistancesToList() {
        distancesList.Clear();
        for(int i = 0; i < nCustomers; i++) {
            List<float> tmp = new List<float>();
            for(int j = 0; j < nCustomers; j++) {
                tmp.Add(distances[i, j]);
            }
            DistanceList dl = new DistanceList();
            dl.index = i;
            dl.distances = tmp;
            distancesList.Add(dl);
        }
    }

    public virtual void SetupDistances() {

        distances = new float[nCustomers, nCustomers];
        depotDistances = new float[nCustomers];
        depotPosition = depots[0].pos;
        nearest = new int[nCustomers];

        ComputeDistances();       //distances and depotDistances
        ConvertDistancesToList(); //for debugging
        ComputeNearest();         //Compute nearest customer for each customer. Assumes ComputeDistances() is called first
    }

    void ComputeDistances() {
        for(int i = 0; i < nCustomers; i++) {
            depotDistances[i] = Mathf.RoundToInt(Vector3.Distance(customers[i].pos, depotPosition));
        }

        for(int i = 0; i < nCustomers; i++) {
            for(int j = i + 1; j < nCustomers; j++) {
                distances[i, j] = Mathf.RoundToInt(Vector3.Distance(customers[i].pos, customers[j].pos));
                distances[j, i] = distances[i, j];
            }
        }
        for(int i = 0; i < nCustomers; i++) {
            distances[i, i] = 0;
        }
    }

    public bool isDataLoaded = false;
    public IEnumerator ReadCoroutine(string filename) {
        ReadUtils.inst.ReadFile(filename);
        while(!ReadUtils.inst.isReadDone)
            yield return new WaitForSeconds(0.1f);
        SetupProblemData(ReadUtils.inst.fileText.Trim().Split("\n"));
        SetupDistances();

        isDataLoaded = true;
    }

    public void ComputeNearest() {

        for(int i = 0; i < nCustomers; i++) {
            float min = float.MaxValue;
            for(int j = 0; j < nCustomers; j++) {
                if(i != j && distances[i, j] < min) {
                    nearest[i] = j;
                    min = distances[i, j];
                }
            }
        }
    }


    //---------------------------------------------------------------------------------------


    public bool coordsSection = false;
    public bool demandSection = false;
    public bool depotSection = false;

    public List<Customer> customerList = new List<Customer>();

    public void SetupProblemData(string[] lines) {
        foreach(string line in lines) {
            //Debug.Log("Line: " + line);
            if(line.Contains("NAME")) {
                problemName = line.Substring(7);
                string[] items = line.Trim().Split('-');
                nVehicles = int.Parse(items[2].Trim().Substring(1));
                continue;
            }
            if(line.Contains("COMMENT")) {
                problemDescription = line.Substring(9);
                continue;
            }
            if(line.Contains("DIMENSION"))
                nCustomers = int.Parse(line.Substring(12));
            if(line.Contains("CAPACITY")) {
                vehicleCapacity = int.Parse(line.Substring(10).Trim());
                customers = new Customer[nCustomers];
            }
            if(line.Contains("NODE_COORD")) {
                coordsSection = true;
                demandSection = false;
                depotSection = false;
                continue;
            }
            if(line.Contains("DEMAND_SECTION")) {
                demandSection = true;
                coordsSection = false;
                depotSection = false;
                continue;
            }
            if(line.Contains("DEPOT_SECTION")) {
                depotSection = true;
                coordsSection = false;
                demandSection = false;
                continue;
            }
            if(coordsSection && !demandSection && !depotSection) {
                //Debug.Log("line: " + line);
                string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); //three items
                //Debug.Log("Items: " + string.Join(", ", items));
                if(items.Length < 3)
                    items = line.Split('\t');
                Customer c = new Customer();
                c.cid = int.Parse(items[0].Trim());
                c.pos = new Vector3(float.Parse(items[1].Trim()), 0, float.Parse(items[2].Trim()));
                customerList.Add(c);

            }
            if(demandSection && !coordsSection && !depotSection) {
                string[] items = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); //three items                string[] items = line.Split(" "); //two items
                if(items.Length < 2)
                    items = line.Split('\t');
                int cid = int.Parse(items[0].Trim());
                Customer c = customerList.Find(x => x.cid == cid);
                c.demand = float.Parse(items[1].Trim());
            }
            if(depotSection && !coordsSection && !demandSection) {
                int did = int.Parse(line.Trim());
                if(did > 0) {
                    depotIds.Add(did);
                } else {
                    depotSection = false;
                }
            }
        }
        GetDepots();
        BuildCustomers();
        TotalDemand();
    }

    public List<Customer> tmpDepots = new List<Customer>();
    void GetDepots() {
        tmpDepots.Clear();
        foreach(int cit in depotIds) {
            if(customerList.Exists(x => x.cid == cit)) {
                Customer customer = customerList.Find(x => x.cid == cit);
                tmpDepots.Add(customer);
            }
        }
        depots = tmpDepots.ToArray();
    }

    void BuildCustomers() {
        foreach(int ci in depotIds) {
            if(customerList.Exists(x => x.cid == ci)) {
                Customer customer = customerList.Find(x => x.cid == ci);
                customerList.Remove(customer);
            }
        }
        customers = customerList.ToArray();
        nCustomers = customers.Length;
    }

    void TotalDemand() {
        totalCustomerDemand = 0;
        for(int i = 0; i < nCustomers; i++) 
            totalCustomerDemand += customers[i].demand;
    }

}
