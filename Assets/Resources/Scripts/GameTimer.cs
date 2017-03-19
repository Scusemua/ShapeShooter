using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour {

    public Text timerLabel;

    private float time;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        time += Time.deltaTime;

        var minutes = Mathf.Floor(time / 60);
        var seconds = time % 60;

        timerLabel.text = string.Format("{0:00} : {1:00}", minutes, seconds);
    }

    #region Properties

    /// <summary>
    /// Exposes the Time property, which is the amount of Time.deltaTime which has passed since game started.
    /// 
    /// Note that Time.deltaTime is the time in seconds it took to complete the "last" frame.
    /// </summary>
    public float TimeSinceStart
    {
        get
        {
            return time;
        }
        set
        {
            time = value;
        }
    }

    #endregion
}
