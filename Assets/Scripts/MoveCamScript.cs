using UnityEngine;

/// <summary>
///     Script for the moving intro camera (animation).
/// </summary>
public class MoveCamScript : MonoBehaviour
{
    /// <summary>
    ///     Starts the intro (called by message).
    /// </summary>
    public void StartIntro()
    {
        GetComponent<Animation>()["Kamera"].time = 0;
        GetComponent<Animation>()["Kamera"].speed = 1;
        GetComponent<Animation>().Play();

        GetComponent<AudioSource>().Play();
    }

    /// <summary>
    ///     Skips the intro (called by message).
    /// </summary>
    public void SkipIntro()
    {
        GetComponent<Animation>().Play();

        GetComponent<Animation>()["Kamera"].time = GetComponent<Animation>()["Kamera"].length;
        GetComponent<Animation>()["Kamera"].speed = 0;

        GetComponent<AudioSource>().Stop();
    }

    /// <summary>
    ///     End of the animation/intro.
    /// </summary>
    public void AnimationEnd()
    {
        SendMessageUpwards("IntroEnd");
    }

    /// <summary>
    ///     Starts the game (called by message).
    /// </summary>
    public void StartGame()
    {
        GetComponent<Camera>().enabled = false;
    }

    /// <summary>
    ///     Shows the main menu (called by message).
    /// </summary>
    public void MainMenu()
    {
        GetComponent<Camera>().enabled = true;
    }
}