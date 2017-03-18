using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class Player : MonoBehaviour {

    public GameObject bulletPrefab;

    /// <summary>
    /// The states the player may have.
    /// </summary>
    private enum STATE { NORMAL, CARNAGE, DECEASED };
    /// <summary>
    /// The current state of the player.
    /// </summary>
    private STATE state;

    private Vector3 previousLocation;
    private Vector2 movementDirectionVertical;
    private Vector2 movementDirectionHorizontal;

    /// <summary>
    /// The number of kills required for a full carnage bar.
    /// </summary>
    private int chargeNeededForCarnage = 20;
    /// <summary>
    /// The number of kills the player currently has.
    /// </summary>
    private int currentBarCharge = 0;
    /// <summary>
    /// The total number of kills the player got throughout the game.
    /// </summary>
    private int totalKills;

    // When the player gets hit, they are temporarily invulnerable to give them a chance to move away.
    private float invulnerableTime = 0.5f;
    private float invulnerableTimeLeft = 1.5f;
    private bool invulnerable = false;
    /// <summary>
    /// Player's maximum health.
    /// </summary>
    private float maxHealth = 50f;
    /// <summary>
    /// The player's current health value.
    /// </summary>
    private float health;
    /// <summary>
    /// Normal Movement Variable.
    /// </summary>
    private float walkSpeed = 4.0f;
    // Fire-rate related.
    private float fireCooldown = 0.2f;
    private float fireCooldownLeft = 0.0f;
    /// <summary>
    /// Affects how long Carnage Mode lasts.
    /// </summary>
    private float carnageModeDecrement = 0.002f;

    // References to the heatlh bar, carnage bar, and carnage bar indicator images.
    private Image healthBar;
    private Image carnageBar;
    private Image carnageReadyIndicator;
    // How fast the health and carnage bars should drain/fill (in terms of the animation).
    private const float HEALTH_BAR_ANIMATION_SPEED = 0.05f;
    private const float CARNAGE_BAR_ANIMATION_SPEED = 0.05f;
    /// <summary>
    /// Indicates whether or not Carnage Mode is ready.
    /// </summary>
    private bool carnageReady = false;
    /// <summary>
    /// Reference to the Kill Counter text on the Canvas.
    /// </summary>
    private Text killCounterText; 

    // References to the dead and alive sprite versions of the player.
    public Sprite aliveImage;
    public Sprite deadImage;
    public Sprite carnageImage;

    /// <summary>
    /// Used for fading out on death.
    /// </summary>
    private SceneTransition transition;
    /// <summary>
    /// Time until fade out when player dies.
    /// </summary>
    private float timeToFadeout = 5.0f;

    /// <summary>
    /// Reference to the Animator component. 
    /// </summary>
    private Animator animator;

    // References to the music.
    public AudioClip rynosTheme;
    public AudioClip bodies;
    private AudioSource musicPlayer;
    private AudioSource attackSoundPlayer;

    /// <summary>
    /// Reference to where in the normal song the audio player was prior to bodies playing.
    /// </summary>
    private float previousAudioLocation;

    void Start()
    {
        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        healthBar = canvas.transform.FindChild("HealthBarFiller").gameObject.GetComponent<Image>();
        carnageBar = canvas.transform.FindChild("CarnageBarFiller").gameObject.GetComponent<Image>();
        carnageReadyIndicator = canvas.transform.GetChild(4).gameObject.GetComponent<Image>();
        carnageReadyIndicator.enabled = false;
        killCounterText = canvas.transform.FindChild("KillCounterText").gameObject.GetComponent<Text>();
        previousLocation = this.transform.position;
        health = maxHealth;
        state = STATE.NORMAL;
        transition = GameObject.Find("SceneTransition").GetComponent<SceneTransition>();
        animator = GetComponent<Animator>();
        musicPlayer = GetComponents<AudioSource>()[0];
        attackSoundPlayer = GetComponents<AudioSource>()[1];
    }

    void Update()
    {
        // Don't let the player's health go above the maximum.
        if (health > maxHealth) health = maxHealth;
        
        // If needed, fill the carnage bar. Fill it slowly to animate it, basically.
        float carnagePercentage = (float)currentBarCharge / chargeNeededForCarnage;
        // Note that if the player is in Carnage Mode, we don't increase the carnage bar at all.
        if (carnageBar.fillAmount < carnagePercentage && state != STATE.CARNAGE) carnageBar.fillAmount += CARNAGE_BAR_ANIMATION_SPEED;
        
        // If we're in carnage mode, decrease the bar a little.
        if (state == STATE.CARNAGE)
        {
            // Decrease the bar, as Carnage Mode is active so long as the bar's fill is greater than zero.
            carnageBar.fillAmount -= carnageModeDecrement;
            // If the bar is depleted, return to normal mode.
            if (carnageBar.fillAmount <= 0.0f)
            {
                DeactivateCarnageMode();
            }
        }

        // If the bar is all-the-way filled, then we can activate carnage mode.
        if (carnagePercentage >= 1.0f)
        {
            carnageReady = true;
            carnageReadyIndicator.enabled = true;
        }

        // If needed, drain/fill the health bar. We fill it slowly to animate it, basically.
        // float healthPercentage = (float)Math.Round((double)health / (double)maxHealth, 1);
        float healthPercentage = health / maxHealth;
        if (healthBar.fillAmount > healthPercentage) healthBar.fillAmount -= HEALTH_BAR_ANIMATION_SPEED;
        if (healthBar.fillAmount < healthPercentage) healthBar.fillAmount = healthPercentage;
        // else if (healthBar.fillAmount < healthPercentage) healthBar.fillAmount += HEALTH_BAR_ANIMATION_SPEED;

        // If the player died, fade to black.
        if (state == STATE.DECEASED)
        {
            timeToFadeout -= 0.1f;
            if (timeToFadeout <= 0)
            {
                transition.OnPlayerDeath();
            }
        }
    }

    void FixedUpdate()
    {
        GetComponent<Rigidbody2D>().velocity = new Vector2(Mathf.Lerp(0, Input.GetAxis("Horizontal") * walkSpeed, 1f),
                                            Mathf.Lerp(0, Input.GetAxis("Vertical") * walkSpeed, 1f));
        // Decrement the cooldown to fire.
        fireCooldownLeft -= Time.deltaTime;
        if (fireCooldownLeft < 0) fireCooldownLeft = 0;

        // Decrement the invulnerability time.
        invulnerableTimeLeft -= Time.deltaTime;
        if (invulnerableTimeLeft <= 0)
        {
            invulnerableTimeLeft = 0;
            invulnerable = false;
        }
        
        if (fireCooldownLeft <= 0)
        {
            if (Input.GetKey("up"))
            {
                shoot(Vector2.up);
            }
            else if (Input.GetKey("down"))
            {
                shoot(Vector2.down);
            }
            else if (Input.GetKey("right"))
            {
                shoot(Vector2.right);
            }
            else if (Input.GetKey("left"))
            {
                shoot(Vector2.left);
            }
        }

        // If Carnage Mode is ready and the player presses space, activate Carnage Mode.
        if (Input.GetKey("space") && carnageReady)
        {
            currentBarCharge = 0;
            carnageReady = false;
            carnageBar.fillAmount -= 0.01f;
            ActivateCarnageMode();
        }
    }

    // Create an instance of a bullet and assign to it the correct direction.
    private void shoot(Vector2 direction)
    {
        if (state == STATE.CARNAGE) attackSoundPlayer.PlayOneShot(attackSoundPlayer.clip, 0.25f);
        else attackSoundPlayer.PlayOneShot(attackSoundPlayer.clip, 1);
        GameObject bulletGO = (GameObject)Instantiate(bulletPrefab, this.transform.position, this.transform.rotation);
        fireCooldownLeft = fireCooldown;
        Bullet bullet = bulletGO.GetComponent<Bullet>();
        bullet.dir = direction;
        // If the player is in Carnage Mode, make sure the bullet's sprite is appropriate.
        if (state == STATE.CARNAGE) bullet.carnageMode = true;
    }

    /// <summary>
    /// Increments the kill counter.
    /// </summary>
    public void IncrementCarnageBar(int amount, bool incrementKillCounter)
    {
        if (state != STATE.CARNAGE)
        {
            currentBarCharge += amount;
        }
        if (incrementKillCounter)
        {
            killCounterText.text = "" + (++totalKills);
        }
    }

    /// <summary>
    /// Takes damage based on the parameterized amount.
    /// </summary>  
    public void TakeDamage(float damage)
    {
        health -= damage;
        print("Health: " + health);
        if (health <= 0.0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Puts the player into Carnage Mode.
    /// 
    /// Carnage Mode does the following things:
    /// - Restores 50% of the player's health.
    /// - Increases fire-rate by factor of four.
    /// - Increases walking speed by 2.
    /// - Increases invulnerable time after being hit.
    /// </summary>
    public void ActivateCarnageMode()
    {
        state = STATE.CARNAGE;
        carnageReadyIndicator.enabled = false;
        animator.SetInteger("state", (int)STATE.CARNAGE);
        health += maxHealth * 0.5f;
        fireCooldown = fireCooldown / 4.0f;
        walkSpeed += 2.0f; ;
        invulnerableTime += 0.5f;
        previousAudioLocation = musicPlayer.time; // Save time.
        musicPlayer.Stop();
        musicPlayer.volume = 1;
        musicPlayer.clip = bodies; // Change music.
        musicPlayer.Play();
    }

    /// <summary>
    /// Returns the player to normal mode. Restores player stats to normal.
    /// </summary>
    public void DeactivateCarnageMode()
    {
        state = STATE.NORMAL;
        animator.SetInteger("state", (int)state);
        fireCooldown *= 4.0f;
        walkSpeed -= 2.0f;
        invulnerableTime -= 0.5f;
        musicPlayer.Stop();
        musicPlayer.volume = 0.5f;
        musicPlayer.clip = rynosTheme; // Restore old music.
        musicPlayer.time = previousAudioLocation; // Restore position in song.
        musicPlayer.Play();

    }

    /// <summary>
    /// Handles the death of the player.
    /// </summary>
    public void Die()
    {
        this.animator.SetInteger("state", 2);
        this.GetComponent<SpriteRenderer>().sprite = deadImage; // Make the player look dead.
        walkSpeed = 0.0f;                       // Prevent the player from moving.
        fireCooldownLeft = int.MaxValue;        // Prevent the player from firing.
        invulnerable = true;                    // Prevent any damage-taking stuff from happening.
        invulnerableTimeLeft = int.MaxValue;    // Forever.
        state = STATE.DECEASED;                 // Update the player's state.
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Enemy")
        {
            BaseEnemy b = coll.collider.GetComponent<BaseEnemy>();
            if (b != null)
            {
                if (!invulnerable)
                {
                    invulnerable = true;
                    invulnerableTimeLeft = invulnerableTime;
                    TakeDamage(b.ContactDamage);
                }
            }
        }
    }
}

