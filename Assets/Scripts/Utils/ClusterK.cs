using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Cluster {
    public Vector3 centroid;
    public Vector3 oldCentroid;
    public float radius;
    public List<int> customerIndices;
    public int clusterIndex = -1;

    public Cluster(int clusterIndex, Vector3 centroid, float diameter, List<int> customerIndices) {
        this.clusterIndex = clusterIndex;
        this.centroid = centroid;
        this.radius = diameter;
        this.customerIndices = customerIndices;
    }
}

[Serializable]
public class ClusterK
{
    //public float[,] distances;
    public Customer[] customers;

    public List<Cluster> myClusters = new List<Cluster>();

    public ClusterK(Customer[] customers) {
        this.customers = customers;
        myClusters = new List<Cluster>();

    }

    public int maxIterations = 100;
    public float tolerance = 1;

    public List<Cluster> Cluster(int k) {
        InitCentroids(k);
        KMeansCluster(k);
        return myClusters;
    }

    public void InitCentroids(int k) {
        int[] indices = new int[customers.Length];
        for(int i = 0; i < customers.Length; i++) {
            indices[i] = UnityEngine.Random.Range(0, customers.Length);
        }

        //GAUtils.Shuffle(indices);
        for(int i = 0; i < k; i++) {//choose k random customer positions as centroids
            Cluster mCluster = new Cluster(i, customers[indices[i]].pos, 0, new List<int>());
            myClusters.Add(mCluster);
        }
    }

    public void KMeansCluster(int k) {
        for(int i = 0; i < maxIterations; i++) {
            AssignPointsToClusters();
            UpdateCentroids();
            UpdateDiameters();
            if(ShouldStop(tolerance))
                break;
        }
    }

    public void AssignPointsToClusters() {
        foreach(Cluster mCluster in myClusters) 
            mCluster.customerIndices.Clear();

        for(int i = 0; i < customers.Length; i++) 
            AddToNearestCentroid(i);//, centroids);

    }
    
    public void AddToNearestCentroid(int ci) {
        float minDist = float.MaxValue;
        Cluster minCluster = null;
        foreach(Cluster mCluster in myClusters) {
            float dist = Vector3.Distance(customers[ci].pos, mCluster.centroid);
            if(dist < minDist) {
                minDist = dist;
                minCluster = mCluster;
            }
        }
        minCluster.customerIndices.Add(ci);
    }

    public bool ShouldStop(float tolerance) {
        foreach(Cluster mCluster in myClusters) 
            if(Vector3.Distance(mCluster.centroid, mCluster.oldCentroid) > tolerance) 
                return false;

        return true;
    }

    public void UpdateCentroids() {
        foreach(Cluster mCluster in myClusters) {
            mCluster.oldCentroid = mCluster.centroid; //save old centroid!!!!!!!
            mCluster.centroid = Vector3.zero;
            foreach(int ci in mCluster.customerIndices) {
                mCluster.centroid += customers[ci].pos;
            }
            mCluster.centroid /= mCluster.customerIndices.Count;
        }
    }

    public void UpdateDiameters() {
        foreach(Cluster mCluster in myClusters) {
            float maxDist = 0;
            foreach(int ci in mCluster.customerIndices) {
                float dist = Vector3.Distance(customers[ci].pos, mCluster.centroid);
                if(dist > maxDist)
                    maxDist = dist;
            }
            mCluster.radius = maxDist;
        }
    }

}
