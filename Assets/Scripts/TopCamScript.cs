using UnityEngine;

public struct StorageStruct
{
    public int Hits;
    public GameObject Ball;
}

/// <summary>
///     Script for the aerial perspective camera.
/// </summary>
public class TopCamScript : MonoBehaviour
{
    public AudioClip TargetClip;

    public GameObject GolfBall;
    private GameObject _golfBall;
    private BallScript _ballScript;

    private bool _active;
    private Vector3 _endZoom;
    private float _zoomPer;

    private bool _autoZoom;
    private float _lastPer;

    /// <summary>
    ///     Starts this instance.
    /// </summary>
    internal void Start()
    {
        _endZoom = transform.position;

        ResetCam();
    }

    /// <summary>
    ///     Resets the cam.
    /// </summary>
    private void ResetCam()
    {
        _active = false;
        _zoomPer = 0;

        _autoZoom = false;
        _lastPer = 0;
    }

    /// <summary>
    ///     Updates this instance.
    /// </summary>
    internal void Update()
    {
        if (!_active || _autoZoom) return;

        var zoomIn = Input.GetAxis("Mouse ScrollWheel") > 0;
        var zoomOut = Input.GetAxis("Mouse ScrollWheel") < 0;

        if (zoomIn) _zoomPer = Mathf.Min(90, _zoomPer + 2f);
        if (zoomOut) _zoomPer = Mathf.Max(0f, _zoomPer - 2f);

        RePositionCamera(zoomIn, zoomOut);
    }

    /// <summary>
    ///     Repositions the camera.
    /// </summary>
    /// <param name="zoomIn">if set to <c>true</c> [zoom in].</param>
    /// <param name="zoomOut">if set to <c>true</c> [zoom out].</param>
    private void RePositionCamera(bool zoomIn, bool zoomOut)
    {
        // to the ground
        var yDiff = (_endZoom - new Vector3(_endZoom.x, 0, _endZoom.z)).y/100*_zoomPer;
        var camPosY = _endZoom.y - yDiff;

        // to the ball
        var xzDiff = (_endZoom - _golfBall.transform.position)/30*Mathf.Min(30, _zoomPer);
        var camPosXZ = _endZoom - xzDiff;

        // total
        var camPos = new Vector3(camPosXZ.x, camPosY, camPosXZ.z);

        transform.position = camPos;

        if (zoomIn || zoomOut)
            _ballScript.Redraw();
    }

    /// <summary>
    ///     Starts the game.
    /// </summary>
    public void StartGame(StartData data)
    {
        ResetCam();

        _active = true;

        GetComponent<Camera>().enabled = true;

        _golfBall = (GameObject) Instantiate(GolfBall);
        _golfBall.transform.parent = GameObject.Find("Karte").transform;

        _ballScript = _golfBall.GetComponent<BallScript>();
        _ballScript.SetCamera(gameObject);

        _ballScript.StartGame(data);

        GetComponent<AudioSource>().Play();
    }

    /// <summary>
    ///     Shows the main menu.
    /// </summary>
    public void MainMenu()
    {
        GetComponent<Camera>().enabled = false;
    }

    /// <summary>
    ///     Automatic zoom.
    /// </summary>
    /// <param name="autoZoom">automatic zoom if set to <c>true</c>.</param>
    public void AutoZoom(bool autoZoom)
    {
        // if ball near target: zoom topcam
        if (autoZoom && !_autoZoom)
        {
            _lastPer = _zoomPer;
            _zoomPer = 90;
            _autoZoom = true;
        }

        // if not anymore: zoom back to normal
        if (!autoZoom && _autoZoom)
        {
            _zoomPer = _lastPer;
            _autoZoom = false;
        }

        RePositionCamera(true, true);
    }

    /// <summary>
    ///     Ball hits the target.
    /// </summary>
    /// <param name="hits">The number of hits.</param>
    public void BallInHole(int hits)
    {
        AutoZoom(true);
        _active = false;

        AudioSource.PlayClipAtPoint(TargetClip, _golfBall.transform.position);

        // send information to GUI
        var tempStorage = new StorageStruct
        {
            Hits = hits,
            Ball = _golfBall
        };

        SendMessageUpwards("ShowTargetText", tempStorage);
    }
}