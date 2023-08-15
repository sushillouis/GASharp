using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GAState
{
    GAInput = 0,
    GARunning,
    GAPaused,
    GADone,
}

public class GUIMgr : MonoBehaviour
{
    public static GUIMgr inst;
    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public GAPanel InputPanel;
    public GAPanel ConsolePanel;
    public GAPanel GraphPanel;
    public GAPanel PhenotypePanel;

    public GAState _state = GAState.GAInput;

    public GAState State
    {
        get { return _state; }
        set { 
            _state = value;

            InputPanel.isVisible = (_state == GAState.GAInput);
            //ConsolePanel.isVisible = (_state == GAState.GARunning);
            //GraphPanel.isVisible = (_state == GAState.GARunning || _state == GAState.GADone);

            }
    }


}
