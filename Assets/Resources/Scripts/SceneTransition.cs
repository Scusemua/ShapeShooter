using UnityEngine;

public class SceneTransition : MonoBehaviour
{

    public Texture2D fadeOutTexture; // The texture that will overlay the screen. This can be a black image or a loading graphic.
    public Texture2D youDied;       // Displayed after fade-out when player dies.
    public float fadeSpeed = 0.8f;  // The fading speed.

    private int drawDepth = -1000;  // The texture's order in the draw hierarchy: a low number means it renders on top.
    private float alpha = 1.0f;   // The texture's alpha value between 0 and 1.
    private int fadeDir = -1;   // The direction to fade: in = -1 or out = 1.

    private bool playerDeath = false; // If true, displays "You Died" after fading-out complete.

    void OnGUI()
    {
        // Fade out/in the alpha value using a direction, a speed and Time.deltaTime to convert the operation to seconds.
        alpha += fadeDir * fadeSpeed * Time.deltaTime;
        // Force (clamp) the number to be between 0 and 1 because GUI.color uses Alpha values between 0 and 1.
        alpha = Mathf.Clamp01(alpha);

        // Set color of our GUI (in this case our texture). All color values remain the same & the Alpha is set to the alpha variable.
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, alpha);
        GUI.depth = drawDepth;                // Make the black texture render on top (drawn last).
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeOutTexture);  // Draw the texture to fit the entire screen area.
        if (playerDeath)
        {
            GUI.DrawTexture(new Rect((int)(Screen.width * 0.125), (int)(Screen.height * 0.125), 
                (int)(Screen.width * 0.75), (int)(Screen.height * 0.75)), youDied);  // Draw the texture to fit the entire screen area.
        }
    }

    // Sets fadeDir to the direction parameter making the scene fade in if -1 and out if 1.
    public float BeginFade(int direction)
    {
        fadeDir = direction;
        return (fadeSpeed);
    }

    // OnLevelWasLoaded is called when a level is loaded. It takes loaded level index (int) as a parameter so you can limit the fade in to certain scenes.
    void OnLevelWasLoaded()
    {
        // alpha = 1;  // Use this if the alpha is not set to 1 by default.
        BeginFade(-1);  // Call the fade in function.
    }

    public void OnPlayerDeath()
    {
        playerDeath = true;
        BeginFade(1);
    }
}