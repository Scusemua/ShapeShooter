using UnityEngine;
using System.Collections;

public abstract class BaseEnemy : MonoBehaviour {

    /// <summary>
    /// The states an enemy may have.
    /// </summary>
    protected enum STATE { LIVING, DECEASED };
    /// <summary>
    /// The current state of the enemy.
    /// </summary>
    protected STATE state;
    /// <summary>
    /// How fast the enemy may move.
    /// </summary>
    public float movementSpeed;
    /// <summary>
    /// The max health of the enemy.
    /// </summary>
    public float maxHealth;
    /// <summary>
    /// How much damage the enemy does on contact.
    /// </summary>
    public float contactDamage;
    /// <summary>
    /// Reference to the blood prefab (which is instantiated upon the death of the enemy).
    /// </summary>
    public GameObject blood;
    /// <summary>
    /// Whether or not the enemy's health should be displayed.
    /// </summary>
    protected bool showHealth = true;
    /// <summary>
    /// Current enemy health.
    /// </summary>
    protected float health;
    /// <summary>
    /// Reference to the player.
    /// </summary>
    protected Player player;
    /// <summary>
    /// The amount the carnage bar is filled for killing this enemy.
    /// </summary>
    public int carnageFill;
    /// <summary>
    /// The Room from which the enemy originated.
    /// </summary>
    protected Room room;

    /// <summary>
    /// Called within the Start() method of child classes.
    /// </summary>
    protected void Create()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        state = STATE.LIVING;
        health = maxHealth;
    }

    void OnGUI()
    {
        if (!showHealth) return;
        Vector2 targetPos = Camera.main.WorldToScreenPoint(transform.position);
        GUI.Box(new Rect(targetPos.x, Screen.height - targetPos.y - 10, 60, 20), health + "/" + maxHealth);
    }

    /// <summary>
    /// Function which handles bullets/projectiles colliding with this enemy.
    /// </summary>
    /// <param name="damage">The damage of the bullet (its a field of the bullet which collided with this enemy).</param>
    public void DoBulletCollision(float damage)
    {
        if (state == STATE.DECEASED) return;
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Function which handles the death of this enemy.
    /// </summary>
    void Die()
    {
        state = STATE.DECEASED;
        player.IncrementCarnageBar(carnageFill, true);
        Instantiate(blood, this.transform.position, this.transform.rotation);
        GetComponent<Renderer>().enabled = false;
        // Disable the collider for the enemy.
        if (GetComponent<PolygonCollider2D>() != null) GetComponent<PolygonCollider2D>().enabled = false;
        if (GetComponent<CircleCollider2D>() != null) GetComponent<CircleCollider2D>().enabled = false;
        showHealth = false;
    }

    /// <summary>
    /// Returns true if this enemy is still alive, otherwise returns false.
    /// </summary>
    public bool IsAlive()
    {
        return state == STATE.LIVING;
    }

    #region Properties

    /// <summary>
    /// Exposes the contactDamage data field.
    /// </summary>
    public float ContactDamage { get { return contactDamage; } }

    /// <summary>
    /// Exposes the room property, which is a reference to the room from which this enemy originated.
    /// </summary>
    public Room Room { get { return room; } set { room = value; } }

    #endregion 

}
