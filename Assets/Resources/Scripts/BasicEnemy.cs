using UnityEngine;
using System.Collections;

public class BasicEnemy : BaseEnemy
{   
    // The enemy will move towards the target's transform.      
    private Transform targetTransform;  

	// Use this for initialization
	void Start () {
        base.Create();
        // movementSpeed = 3.5f;
        // maxHealth = 3.0f;
        targetTransform = player.transform;
        base.contactDamage = 5.0f;
    }
	
	// Update is called once per frame
	void Update () {
        targetTransform = player.transform;
        // Check if the room is active and then if the player is near.
        if (room.IsActive()) // && Vector3.Distance(targetTransform.position, this.transform.position) < 12)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetTransform.position, movementSpeed * Time.deltaTime);
        }
    }
}
