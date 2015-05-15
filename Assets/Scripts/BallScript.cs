using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal struct HitPoint
{
    internal Vector3 FwdVector;
    internal float SpeedMag;
    internal Vector3 PosVector;
}

public struct StartData
{
    internal int Level;
    internal TutScript TutScript;
}

/// <summary>
///     The most important script. Handles physics and all the in-game stuff!
/// </summary>
public class BallScript : MonoBehaviour
{
    // data for the custom physics engine
    private const float PhyTimestep = 0.005f;
    public float BreakAcc = 0.00003f;
    public float MaxSpeed = 4.0f;
    public float MinSpeed = 0.05f;

    private float _maxSpeedSc;
    private float _minSpeedSc;

    private float _ballRadius;

    // vector settings
    private Vector3 _fwdVector;
    private int _currRotation;
    private Vector3 _lastKnownPos;

    // power settings / power bar
    private float _power;

    private const float MinPower = 0.1f;
    private float _powerBarMarker;
    private float _powerBarVal;
    private int _powerBarDir;

    private readonly int[] _levels = {80, 110, 200, 400};
    private int _curLevel;

    // drawable lines
    private VectorLine _vectorLine;
    private List<HitPoint> _wayPoints;

    // states
    private enum States
    {
        Inactive,
        FirstRun,
        Setting,
        Power,
        Animation,
        Running
    }

    private States _gameState;
    private int _hits;

    // reference to other objects
    public LayerMask SphCastLayer;
    public LayerMask TargetCastLayer;

    public GameObject TargetSpot;
    private GameObject _targetSpot;

    private TopCamScript _cameraScript;
    private GameObject _targetObj;

    public GameObject GolfClub;
    private GameObject _golfClub;
    private ClubScript _clubScript;

    public AudioClip ObstacleClip;

    private TutScript _tutScript;

    // gui / power bar
    private GUIStyle _borderStyle;
    private GUIStyle _boxInnerStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _markerStyle;

    /// <summary>
    ///     Awakes this instance.
    /// </summary>
    internal void Awake()
    {
        _gameState = States.Inactive;

        // load from external
        float tmpfloat;
        if (float.TryParse(ExternalData.ExtData["BreakAcc"], out tmpfloat)) BreakAcc = tmpfloat;
        if (float.TryParse(ExternalData.ExtData["MaxSpeed"], out tmpfloat)) MaxSpeed = tmpfloat;
        if (float.TryParse(ExternalData.ExtData["MinSpeed"], out tmpfloat)) MinSpeed = tmpfloat;

        int tmpint;
        if (int.TryParse(ExternalData.ExtData["LvlVal0"], out tmpint)) _levels[0] = tmpint;
        if (int.TryParse(ExternalData.ExtData["LvlVal1"], out tmpint)) _levels[1] = tmpint;
        if (int.TryParse(ExternalData.ExtData["LvlVal2"], out tmpint)) _levels[2] = tmpint;
        if (int.TryParse(ExternalData.ExtData["LvlVal3"], out tmpint)) _levels[3] = tmpint;

        // prepary physics
        _maxSpeedSc = MaxSpeed*PhyTimestep;
        _minSpeedSc = MinSpeed*PhyTimestep;

        // gravity
        _ballRadius = transform.localScale.y/2;

        RaycastHit grHit;
        if (Physics.Raycast(transform.position, -Vector3.up, out grHit))
            transform.position = grHit.point + Vector3.up*_ballRadius;

        _lastKnownPos = transform.position;

        // where is the target?
        _targetObj = GameObject.Find("Loch");
    }


    /// <summary>
    ///     Starts the game.
    /// </summary>
    /// <param name="data">Information data.</param>
    public void StartGame(StartData data)
    {
        // reset data
        _hits = 1;

        _power = 0.4f;
        _powerBarDir = 1;
        _curLevel = data.Level;

        // instantiate target + club
        _targetSpot = (GameObject) Instantiate(TargetSpot);
        _targetSpot.transform.parent = transform.parent;

        _golfClub = (GameObject) Instantiate(GolfClub);
        _golfClub.transform.parent = transform.parent;

        _clubScript = _golfClub.GetComponent<ClubScript>();
        _clubScript.SetScript(this);

        _tutScript = data.TutScript;

        // let's go
        _vectorLine.ForceClear();
        _gameState = States.FirstRun;
    }

    /// <summary>
    ///     Initialization per Change/Round
    /// </summary>
    private void SetUpPhase(bool newRound = false)
    {
        _gameState = States.Setting;

        if (newRound)
        {
            _hits++;
            _tutScript.SetText(_hits);
        }

        if (_wayPoints == null)
        {
            _wayPoints = new List<HitPoint>();
            _fwdVector = -Vector3.forward*_maxSpeedSc*_power;
        }
        else
            _fwdVector = _fwdVector.normalized*_maxSpeedSc*_power;

        CalcWayPoints();
        RePosGolfClub();
    }

    /// <summary>
    ///     Updates this instance.
    /// </summary>
    internal void Update()
    {
        if (_gameState == States.Inactive || _gameState == States.Running)
            return;

        if (_gameState == States.Setting || _gameState == States.FirstRun)
        {
            bool changed = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
                           Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
                           _gameState == States.FirstRun;

            // modifier
            var modifier = 1f;

            if (Input.GetKey(KeyCode.LeftShift)) modifier = 1/8.0f;
            if (Input.GetKey(KeyCode.RightShift)) modifier = 1/8.0f;
            if (Input.GetKey(KeyCode.LeftControl)) modifier = 8.0f;
            if (Input.GetKey(KeyCode.RightControl)) modifier = 8.0f;

            // angle
            float step = 0f;

            if (Input.GetKey(KeyCode.A)) step = -0.1f;
            if (Input.GetKey(KeyCode.D)) step = +0.1f;

            step *= modifier;
            transform.Rotate(Vector3.up, step, Space.World);
            RotateVector(ref _fwdVector, step);

            // power
            float power = 0f;

            if (Input.GetKey(KeyCode.W)) power = +0.01f;
            if (Input.GetKey(KeyCode.S)) power = -0.01f;

            power *= modifier;
            _power = Mathf.Min(1, Mathf.Max(MinPower, _power + power));

            // update
            if (changed)
                SetUpPhase();

            // shoot
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _powerBarVal = 0;
                _powerBarMarker = _power;

                _gameState = States.Power;
            }

            if (_gameState == States.FirstRun)
                _gameState = States.Setting;
        }

        if (_gameState == States.Power)
        {
            // increase/decrease power bar
            _powerBarVal += _powerBarDir*Time.deltaTime*_levels[_curLevel];

            if (_powerBarDir == +1 && _powerBarVal >= 100) _powerBarDir = -1;
            if (_powerBarDir == -1 && _powerBarVal <= 0) _powerBarDir = +1;

            if (Input.GetKeyUp(KeyCode.Space))
            {
                // recalculate way
                var tmp = _power;
                _power = Mathf.Clamp(_powerBarVal/100, MinPower, 1);

                SetUpPhase();

                _power = tmp;

                _gameState = States.Animation;
                _clubScript.StartAnimation();
                Redraw();
            }
        }
    }

    /// <summary>
    ///     Repositions the golf club.
    /// </summary>
    private void RePosGolfClub()
    {
        if (_fwdVector.normalized != Vector3.zero)
        {
            var gcPos = transform.position - 0.05f*_fwdVector.normalized;
            _golfClub.transform.position = new Vector3(gcPos.x, 0.665f, gcPos.z);
            _golfClub.transform.forward = _fwdVector.normalized;
        }
    }

    /// <summary>
    ///     Calculates the reflection vector.
    /// </summary>
    /// <param name="forward">The forward.</param>
    /// <param name="normal">The normal.</param>
    /// <returns>The reflection vector.</returns>
    private static Vector3 CalcReflectVector(Vector3 forward, Vector3 normal)
    {
        Vector3 reflect = forward - 2*Vector3.Dot(forward, normal)*normal;
        return (reflect + forward*0.4f).normalized;
    }

    /// <summary>
    ///     Rotates a vector.
    /// </summary>
    /// <param name="forward">The vector to rotate.</param>
    /// <param name="angle">The angle.</param>
    private static void RotateVector(ref Vector3 forward, float angle)
    {
        forward = Quaternion.Euler(0, angle, 0)*forward;
    }

    /// <summary>
    ///     Draws the way points.
    /// </summary>
    private void DrawWayPoints()
    {
        _vectorLine.ClearPoints();

        if (_targetSpot == null) return;

        if (_gameState == States.Setting)
        {
            _vectorLine.AddPoint(transform.position);

            foreach (var hpt in _wayPoints)
                _vectorLine.AddPoint(hpt.PosVector);

            _targetSpot.GetComponent<MeshRenderer>().enabled = true;
            _targetSpot.transform.position = _wayPoints[_wayPoints.Count - 1].PosVector;

            _vectorLine.ForceDraw();
        }
        else
        {
            _targetSpot.GetComponent<MeshRenderer>().enabled = false;
            _vectorLine.ForceClear();
        }
    }

    /// <summary>
    ///     Redraws the way points.
    /// </summary>
    public void Redraw()
    {
        DrawWayPoints();
    }

    /// <summary>
    ///     Calculates the way points.
    /// </summary>
    private void CalcWayPoints()
    {
        // list
        _wayPoints.Clear();

        // physics
        Vector3 curVec = transform.position;
        Vector3 curFVec = _fwdVector;

        float totalDist = (_maxSpeedSc*_maxSpeedSc*_power*_power)/(2.0f*BreakAcc);

        // raycasts / spherecasts
        while (totalDist >= 0)
        {
            float curFMag = 0;

            RaycastHit hit;
            if (Physics.SphereCast(curVec, _ballRadius, curFVec, out hit, 10, SphCastLayer))
            {
                // calc waypoints
                if (totalDist - hit.distance > 0)
                {
                    var norm = new Vector3(hit.normal.x, 0, hit.normal.z);
                    curVec = hit.point + norm*_ballRadius;

                    curFMag = curFVec.magnitude;
                    curFMag = Mathf.Sqrt((curFMag*curFMag) - 2.0f*BreakAcc*hit.distance);

                    curFVec = CalcReflectVector(curFVec, hit.normal)*curFMag;
                    curFVec.y = 0;
                }
                else
                {
                    curVec = curVec + curFVec.normalized*totalDist;
                    totalDist = -1;
                }

                _wayPoints.Add(new HitPoint {FwdVector = curFVec, SpeedMag = curFMag, PosVector = curVec});
                totalDist -= hit.distance;
            }
            else
                break;
        }

        DrawWayPoints();
    }

    /// <summary>
    ///     (Re-)Calculate way points every second.
    /// </summary>
    internal IEnumerator AutoCalcWayPoints()
    {
        while (_gameState == States.Setting)
        {
            CalcWayPoints();
            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    ///     End of club animation -> start ball
    /// </summary>
    public void ClubAnimationEnd()
    {
        _gameState = States.Running;
        GetComponent<AudioSource>().Play();

        Redraw();
    }

    /// <summary>
    ///     Fixed update.
    /// </summary>
    internal void FixedUpdate()
    {
        if (_gameState != States.Running)
            return;

        // gravity
        RaycastHit grHit;
        if (Physics.SphereCast(transform.position, _ballRadius/1.2f, -Vector3.up, out grHit))
        {
            if (grHit.collider.gameObject.tag == "Target")
            {
                BallInHole();
                return;
            }

            if (grHit.collider.gameObject.tag != "Ground")
                return;

            transform.position = grHit.point + Vector3.up * _ballRadius;
            _lastKnownPos = transform.position;
        }

        // are we still in the lane?
        if (Physics.Raycast(transform.position, -Vector3.up, out grHit))
        {
            if (grHit.collider.gameObject.tag == "Terrain")
                transform.position = _lastKnownPos;
        }
        else
            transform.position = _lastKnownPos;

        // forward movement
        float distLeft = 1;
        RaycastHit hit;

        if (Physics.SphereCast(transform.position, _ballRadius, _fwdVector, out hit, _fwdVector.magnitude,
            SphCastLayer))
        {
            var hitVolume = ((_fwdVector.magnitude - _minSpeedSc)/(_maxSpeedSc - _minSpeedSc))*0.6f;
            AudioSource.PlayClipAtPoint(ObstacleClip, transform.position, hitVolume);

            if (_wayPoints.Count > 1)
            {
                transform.position = _wayPoints[0].PosVector;

                _fwdVector = _wayPoints[0].FwdVector;
                _fwdVector = _fwdVector.normalized*_wayPoints[0].SpeedMag;

                _wayPoints.RemoveAt(0);
            }
            else
                _fwdVector = CalcReflectVector(_fwdVector, hit.normal);

            distLeft = (_fwdVector.magnitude - hit.distance)/_fwdVector.magnitude;
        }

        float angle = (_fwdVector.magnitude*distLeft)/(2*Mathf.PI*_ballRadius);

        transform.position += _fwdVector*distLeft;
        transform.Rotate(transform.right, angle*360);

        var newSpeed = _fwdVector.magnitude - BreakAcc;

        if (newSpeed < _minSpeedSc)
        {
            GetComponent<AudioSource>().Stop();
            SetUpPhase(true);
        }
        else
        {
            GetComponent<AudioSource>().volume = ((_fwdVector.magnitude - _minSpeedSc)/(_maxSpeedSc - _minSpeedSc))*0.4f;
            _fwdVector = _fwdVector.normalized*newSpeed;
        }

        // if near target: zoom topcam
        // if (Vector3.Distance(TargetObj.transform.position, transform.position) < 0.2f)
        //    _cameraScript.AutoZoom(true);
    }

    /// <summary>
    ///     Ball hits the target.
    /// </summary>
    private void BallInHole()
    {
        _vectorLine.ForceClear();
        _gameState = States.Inactive;

        GetComponent<AudioSource>().Stop();
        Redraw();

        // destroy objects
        Destroy(_targetSpot);
        Destroy(_golfClub);

        // move ball to target
        var targPos = _targetObj.transform.position;
        transform.position = new Vector3(targPos.x, -0.26f, targPos.z);

        // move/zoom camera
        _cameraScript.BallInHole(_hits);
    }

    /// <summary>
    ///     Sets the top camera gameobject.
    /// </summary>
    /// <param name="topCam">The top cam.</param>
    internal void SetCamera(GameObject topCam)
    {
        _cameraScript = topCam.GetComponent<TopCamScript>();
        _vectorLine = topCam.GetComponent<VectorLine>();
    }

    /// <summary>
    ///     Called when the GUI is drawn.
    /// </summary>
    internal void OnGUI()
    {
        if (_gameState != States.Power && _gameState != States.Animation)
            return;

        InitStyles();

        // coordinates
        var screenWidth = Screen.width/2.0f;
        var screenHeight = Screen.height/3.0f;

        var width = screenWidth*0.95f;
        const int height = 100;
        var left = screenWidth - (screenWidth*0.95f/2.0f);
        var top = screenHeight - 50;

        // power bar border
        GUI.Box(new Rect(left, top, width, height), "", _borderStyle);

        // power bar inner part
        GUI.Box(new Rect(left + 5, top + 5, width - 10, height - 10), "", _boxInnerStyle);

        // power bar itself
        var powerWidth = Mathf.Clamp(_powerBarVal/100.0f*(width - 10), 6, width - 10);
        GUI.Box(new Rect(left + 5, top + 5, powerWidth, height - 10), "", _boxStyle);

        // marker on it
        var perc = (_powerBarMarker - MinPower)/(1 - MinPower);
        left += Mathf.Clamp(width*perc, 5, width - 15);

        GUI.Box(new Rect(left, top - 10, 10, height + 20), "", _markerStyle);
    }

    /// <summary>
    ///     Makes a background tex.
    /// </summary>
    /// <param name="col">The color.</param>
    /// <returns></returns>
    private static Texture2D MakeTex(Color col)
    {
        var pix = new Color[4];

        for (var i = 0; i < pix.Length; ++i)
            pix[i] = col;

        var result = new Texture2D(2, 2);

        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    /// <summary>
    ///     Initializes the styles.
    /// </summary>
    private void InitStyles()
    {
        if (_borderStyle != null) return;

        // styles
        _borderStyle = new GUIStyle(GUI.skin.button)
        {
            normal = {background = MakeTex(new Color(0.7f, 0.7f, 0.7f, 0.9f))},
            hover = {background = MakeTex(new Color(0.7f, 0.7f, 0.7f, 0.9f))}
        };

        _boxInnerStyle = new GUIStyle(GUI.skin.button)
        {
            normal = {background = MakeTex(new Color(0.4f, 0.4f, 0.4f, 0.7f))},
            hover = {background = MakeTex(new Color(0.4f, 0.4f, 0.4f, 0.7f))}
        };

        _boxStyle = new GUIStyle(GUI.skin.button)
        {
            normal = {background = MakeTex(new Color(0.9f, 0.1f, 0.1f, 0.7f))},
            hover = {background = MakeTex(new Color(0.9f, 0.1f, 0.1f, 0.7f))}
        };

        _markerStyle = new GUIStyle(GUI.skin.button)
        {
            normal = {background = MakeTex(new Color(0, 0, 0, 0.7f))},
            hover = {background = MakeTex(new Color(0, 0, 0, 0.7f))}
        };
    }
}