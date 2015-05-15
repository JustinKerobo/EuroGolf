using UnityEngine;

/// <summary>
///     Script for the golf club animation.
/// </summary>
public class ClubScript : MonoBehaviour
{
    private BallScript _ballScript;

    private bool _animActive;
    private Vector3 _curFwd;

    /// <summary>
    ///     Awakes this instance.
    /// </summary>
    internal void Awake()
    {
        _animActive = false;
    }

    /// <summary>
    ///     Late updates this instance.
    /// </summary>
    internal void LateUpdate()
    {
        if (_animActive)
        {
            _curFwd.y = transform.forward.y;
            transform.forward = _curFwd;
        }
    }

    /// <summary>
    ///     Sets the parent script.
    /// </summary>
    /// <param name="script">The script.</param>
    public void SetScript(BallScript script)
    {
        _ballScript = script;
    }

    /// <summary>
    ///     Starts the animaion.
    /// </summary>
    public void StartAnimation()
    {
        GetComponent<Animation>()["Club"].time = 0;
        GetComponent<Animation>().Play();

        _curFwd = transform.forward;
        _animActive = true;
    }

    /// <summary>
    ///     Called when the animation ends.
    /// </summary>
    public void OnAnimationEnd()
    {
        _animActive = false;
        GetComponent<Animation>().Stop();

        transform.forward = _curFwd;
    }

    /// <summary>
    ///     Called when the club hits the ball.
    /// </summary>
    public void OnAnimationHit()
    {
        GetComponent<AudioSource>().Play();
        _ballScript.ClubAnimationEnd();
    }
}