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

        var minutes = time / 60;
        var seconds = time % 60;
        var fraction = (time * 100) % 100;

        timerLabel.text = string.Format("{0:00} : {1:00} : {2:000}", minutes, seconds, fraction);
    }

    #region Properties

    /// <summary>
    /// Exposes the Time property.
    /// </summary>
    public float GameTime
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
