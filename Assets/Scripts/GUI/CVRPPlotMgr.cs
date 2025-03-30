using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CVRPPlotMgr : MonoBehaviour
{
    public static CVRPPlotMgr inst;
    private void Awake() {
        inst = this;
    }

    public CVRPEvaluator cvrpEvaluator;
    public GAParameters gap;

    public List<Plotter> routePlotters = new List<Plotter>();
    public Plotter PlotterPrefab;
    public Individual best;

    public void Init(GAParameters pars) {

        gap = pars;
        cvrpEvaluator = pars.metaCVRPEvaluator;
        SetupColors(cvrpEvaluator.nVehicles);
        routePlotters.Clear();
        for(int i = 0; i < cvrpEvaluator.nVehicles; i++) {
            Plotter plotter = Instantiate(PlotterPrefab, transform);
            plotter.InitAxes();
            plotter.pointsRenderer.startColor = vColors[i];
            plotter.pointsRenderer.endColor = vColors[i];
            routePlotters.Add(plotter);
        }

    }

    [ContextMenu("SetColors")]
    public void SetColors() {
        int i = 0;
        foreach(Plotter plotter in routePlotters) {
            plotter.pointsRenderer.startColor = vColors[i];
            plotter.pointsRenderer.endColor= vColors[i];
            i++;
        }
    }

    public void SetBest(Individual ind) {

        int vehicleIndex = 0;
        foreach(Route route in ind.routes) {
            List<Vector3> positions = new List<Vector3>();
            foreach(int ci in route.tour) {
                Vector3 pos = cvrpEvaluator.customers[ci].pos;
                positions.Add(pos);
            }
            routePlotters[route.vid].SetRoute(route.vid, positions, cvrpEvaluator.depots[0].pos);
            vehicleIndex++;
        }

    }


    public void Plot() {

        foreach(Plotter plotter in routePlotters) {
            plotter.ShareLimits(routePlotters);
        }
        foreach(Plotter plotter in routePlotters) {
            plotter.PlotPoints();
        }
        SetColors();
    }


    public Individual tester;
    public void TestPlot() {
        tester = new Individual(gap);

        for(int i = 0; i < gap.seqChromLength; i++) {
            tester.seqChrom[i] = i;
        }

        cvrpEvaluator.DecodeToRoutes(tester);
        SetBest(tester);
        Plot();
    }

    public List<Color> vColors = new List<Color>();
    public void SetupColors(int nColors = 10) {
        if(vColors.Count < gap.metaCVRPEvaluator.nVehicles) {
            float hue = Random.value;//        GARandom.inst.rand.Next();
            float gr = 0.61803398875f;
            for(int i = vColors.Count; i < nColors; i++) {
                hue += gr;
                hue %= 1f;
                Color color = new Color(hue, hue, Random.value);
                vColors.Add(color);
            }
        }

    }

}
