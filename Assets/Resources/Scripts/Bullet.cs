using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 1f;
    public float radius = 0f;

    public Vector2 dir;

    // Reference to all the projectile sprites.
    private Sprite[] sprites;

    public bool carnageMode = false;    // Flag which identifies when sprite must be changed.
    public bool changed = false;    // Prevents changing sprite constantly.

    void Start()
    {
        sprites = Resources.LoadAll<Sprite>("Sprites/SpriteSheetProjectiles");
    }

    // Update is called once per frame
    void Update()
    {
        float distThisFrame = speed * Time.deltaTime;

        transform.Translate(dir.normalized * distThisFrame, Space.World);

        // To find out if a point in the scene is seen by a camera, 
        // you can use Camera.WorldToViewportPoint to get that point in viewport space.
        // If both the x and y coordinate of the returned point is between 0 and 1 
        // and the z coordinate is positive, then the point is seen by the camera.
        Vector3 convertedPos = Camera.main.WorldToViewportPoint(transform.position);
        if (!(convertedPos.z > 0) || !(Mathf.Abs(convertedPos.x + convertedPos.y) < 2)) 
        {
            Destroy(gameObject);
        }

        // If the player is in Carnage Mode, the sprite will be different.
        if (carnageMode && !changed)
        {
            GetComponent<SpriteRenderer>().sprite = sprites[2];
            changed = true;
        }
    }

    /// <summary>
    /// Handles collisions.
    /// </summary>
    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Enemy")
        {
            BaseEnemy b = coll.collider.GetComponent<BaseEnemy>();
            if (b != null)
            {
                b.DoBulletCollision(damage);   
            }
        }
        Destroy(gameObject);
    }
}