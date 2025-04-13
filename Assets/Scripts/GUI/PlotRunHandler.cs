using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlotRunHandler : MonoBehaviour{

    public TMP_Text RunNumberText;
    public Button Forward;
    public Button Backward;
    public int currentRun = 0;
    public int maxRun = 1;
    // Start is called before the first frame update
    void Start()
    {
        RunNumberText.text = "Run: " + currentRun.ToString();
        Forward.onClick.AddListener(ForwardButtonClicked);
        Backward.onClick.AddListener(BackwardButtonClicked);
    }

    public void SetMaxRuns(int max) {
        maxRun = max;
    }

    public void ForwardButtonClicked() {
        currentRun++;
        currentRun = Mathf.Clamp(currentRun, 0, maxRun-1);
        RunNumberText.text = "Run: " + currentRun.ToString();

    }


    public void BackwardButtonClicked() {
        currentRun--;
        currentRun = Mathf.Clamp(currentRun, 0, maxRun-1);
        RunNumberText.text = "Run: " + currentRun.ToString();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
