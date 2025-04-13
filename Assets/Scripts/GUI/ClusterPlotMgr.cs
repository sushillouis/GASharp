using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;



public class ClusterPlotMgr : MonoBehaviour
{
    public static ClusterPlotMgr inst;
    private void Awake() {
        inst = this;
    }

    public GameObject SpherePrefab;
    public GameObject SelectionCylinderPrefab;
    public List<GameObject> points = new List<GameObject>();
    public List<GameObject> circles = new List<GameObject>();

    public List<Cluster> clusters;
    public Plotter plotter;
    public CVRPPlotMgr cvrpPlotMgr;

    // Start is called before the first frame update
    void Start()    {
        
    }

    // Update is called once per frame
    void Update()    {
        
    }

    public int nPoints = 0;
    public int nClusters = 0;
    public Customer[] customers;

    public void Init(Customer[] customers, int k) {
        this.customers = customers;
        this.nPoints = customers.Length;
        this.nClusters = k;
        points.Clear();
        for(int i = 0; i < nPoints; i++) {
            GameObject go = Instantiate(SpherePrefab, this.transform);
            points.Add(go);
        }
        circles.Clear();
        for(int i = 0; i < nClusters; i++) {
            GameObject go = Instantiate(SelectionCylinderPrefab, this.transform);
            circles.Add(go);
        }
        plotter.InitAxes();
    }

    public void SetClusters(List<Cluster> clusters) {
        int clusterIndex = 0;
        int pointsIndex = 0;
        this.clusters = clusters;
        SetClusterLimits(clusters);

        foreach(Cluster cluster in clusters) {
            GameObject cGo = circles[clusterIndex];
            Color clusterColor = cvrpPlotMgr.vColors[clusterIndex];
            cGo.transform.position = plotter.Convert(cluster.centroid);
            cGo.transform.localScale = new Vector3(cluster.radius*2 * plotter.xInc, 1, cluster.radius*2*plotter.zInc);
            cGo.GetComponentInChildren<MeshRenderer>().material.color = clusterColor;
            clusterIndex++;
            foreach(int ci in cluster.customerIndices) {
                GameObject go = points[pointsIndex];
                go.transform.position = plotter.Convert(customers[ci].pos);
                go.GetComponentInChildren<MeshRenderer>().material.color = clusterColor;
                pointsIndex++;
            }
        }
    }

    public void SetClusterLimits(List<Cluster> clusters) {
        foreach(Cluster cluster in clusters) {
            plotter.AddPoint(cluster.centroid);
            foreach(int ci in cluster.customerIndices) {
                plotter.AddPoint(customers[ci].pos);
            }
        }
    }

}
