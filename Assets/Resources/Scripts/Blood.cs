using UnityEngine;
using System.Collections;

public class Blood : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/SpriteSheet");
        int bloodType = Random.Range(1, 4);
        switch (bloodType)
        {
            case 1:
                GetComponent<SpriteRenderer>().sprite = sprites[6];
                break;
            case 2:
                GetComponent<SpriteRenderer>().sprite = sprites[7];
                break;
            case 3:
                GetComponent<SpriteRenderer>().sprite = sprites[8];
                break;
        }
	}
	
}
