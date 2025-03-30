using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

public class TSPPlotMgr : MonoBehaviour
{
    public static TSPPlotMgr inst;
    private void Awake() {
        inst = this;
    }

    public bool isDoneLoading = false;
    public string baseURL = "https://www.cse.unr.edu/~sushil/class/gas/TSP/problems/";
    public Plotter tspPlotter;
    public List<Vector3> cities = new List<Vector3>();
    public List<Vector3> tour = new List<Vector3>();

    private void Start() {

    }

    public void Init(string filename) {

        tspPlotter.InitAxes();
        InitCities(filename);
    }
    void InitCities(string filename) {

        //nCities = LoadAndCountCities(filename);
        if(Application.platform == RuntimePlatform.WebGLPlayer)
            LoadAndCountCititiesFromWeb(baseURL + filename); //sets cities and nCitites
        else
            LoadAndCountCitiesFromFile(filename); //sets cities and nCities
    }

    
    public void SetBest(Individual individual) {
        tour.Clear();
        for(int i = 0; i < individual.parameters.seqChromLength; i++) {
            tour.Add(cities[individual.seqChrom[i]]);
        }
        tspPlotter.SetPoints(tour);
    }

    public string tspName;
    public int nCities;
    public string line;
    
    public void LoadAndCountCititiesFromWeb(string url) {
        StartCoroutine(DownloadAndCountCities(url));
    }

    IEnumerator DownloadAndCountCities(string url) {
        using(UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
            Debug.Log("Dowloading from: " + url);
            yield return webRequest.SendWebRequest();
            if(webRequest.result != UnityWebRequest.Result.Success) {
                Debug.Log($"Failed to download: {webRequest.error}");
            } else {
                Debug.Log($"Succeeded in downloading: {url}");
                ParseAndLoad(webRequest.downloadHandler.text);
            }
        }
    }

    public void LoadAndCountCitiesFromFile(string filename) {
        Debug.Log("Reading TSP data: " + filename);
        StreamReader sr = new StreamReader(filename);
        string contents = sr.ReadToEnd();
        ParseAndLoad(contents);
        if(nCities != cities.Count) 
            Debug.Log("Mismatch in city count: nC: " + nCities + ", count: " + cities.Count);

    }

    void ParseAndLoad(string tspFileContent) {
        Debug.Log("file: " + tspFileContent);
        string[] lines = tspFileContent.Split('\n');
        bool isNodeSection = false;
        cities.Clear();
        foreach(string line in lines) {
            if(line.Contains("NAME"))
                tspName = line.Substring(7);
            if(line.Contains("DIMENSION"))
                nCities = int.Parse(line.Substring(11).Trim());
            if(line.Contains("NODE_")) {
                isNodeSection = true;
                continue;
            }
            if(isNodeSection) {
                string[] items = line.Split(' ');
                if(items.Length >= 3) {
                    Vector3 coords = new Vector3(float.Parse(items[1]), 0, float.Parse(items[2]));
                    cities.Add(coords);
                }
            }
        }

        tspPlotter.SetPoints(cities);
        tspPlotter.PlotPoints();
        isDoneLoading = true;
    }


    void SkipLines(StreamReader sr, int n) {
        for(int i = 0; i < n; i++) {
            sr.ReadLine();
        }
    }



    public void PlotTour() {
        tspPlotter.PlotPoints();
    }



}



/*
 * 
 * 
 * 
public int LoadAndCountCities(string filename) {
        StreamReader streamReader = new StreamReader(filename);
        line = streamReader.ReadLine();
        problemFilename = line.Substring(6);
        SkipLines(streamReader, 2);
        line = streamReader.ReadLine();
        nCities = int.Parse(line.Substring(11));
        SkipLines(streamReader, 2);
        cities.Clear();
        for(int i = 0; i < nCities; i++) {
            line = streamReader.ReadLine();
            string[] items = line.Split(' ');
            Vector3 coords = new Vector3(float.Parse(items[1]), 0, float.Parse(items[2]));
            cities.Add(coords);
        }
        return cities.Count;
    }

 * 
 * 
 * 
 */