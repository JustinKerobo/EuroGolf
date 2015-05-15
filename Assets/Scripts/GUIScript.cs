using UnityEngine;

/// <summary>
///     Main GUI script, handles the intro, 3D text and different menus.
/// </summary>
public class GUIScript : MonoBehaviour
{
    private enum GUIStates
    {
        Loading,
        Intro,
        MainMenu,
        InGame,
        Score
    }

    private GUIStates _gameState;

    // objects
    public GameObject TutText;
    private GameObject _tutText;

    public GameObject TargetText;
    private GameObject _targetText;

    public GameObject TitleTxt;
    private GameObject _titleTxt;

    private GameObject _ballToDestroy;

    // GUI
    private int _guiLevel;

    private GUIStyle _labelStyleSkip;
    private GUIStyle _labelStyleMenu;
    private GUIStyle _buttonStyle;
    private GUIStyle _boxStyle;

    public Color ButtonTextColor;
    public Color ButtonBgColor;
    public Color ButtonHoverBgColor;
    public Color ButtonOnNormalBgColor;
    public Color ButtonOnHoverBgColor;

    public Color BoxBgColor;
    public Color BoxHoverBgColor;


    /// <summary>
    ///     Starts this instance.
    /// </summary>
    internal void Start()
    {
        _gameState = GUIStates.Loading;
        _guiLevel = 1;

        StartCoroutine(ExternalData.LoadData());
    }

    /// <summary>
    ///     Updates this instance.
    /// </summary>
    internal void Update()
    {
        switch (_gameState)
        {
            case GUIStates.Loading:
                if (!ExternalData.Loading)
                {
                    if (ExternalData.ExtData["ShowIntro"].Equals("true"))
                    {
                        _gameState = GUIStates.Intro;
                        BroadcastMessage("StartIntro");
                    }
                    else
                    {
                        BroadcastMessage("SkipIntro");
                        ShowMainMenu();
                    }
                }

                break;

            case GUIStates.Intro:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    BroadcastMessage("SkipIntro");
                    ShowMainMenu();
                }

                break;

            case GUIStates.MainMenu:
                if (Input.GetKeyDown(KeyCode.Space))
                    StartNewGame();

                break;
        }
    }

    /// <summary>
    ///     Starts the game.
    /// </summary>
    private void StartNewGame()
    {
        _gameState = GUIStates.InGame;

        Destroy(_titleTxt);

        _tutText = (GameObject) Instantiate(TutText);
        _tutText.transform.parent = transform;

        // send info to the golfball and its script
        var data = new StartData
        {
            Level = _guiLevel,
            TutScript = _tutText.GetComponent<TutScript>()
        };

        BroadcastMessage("StartGame", data);
    }

    /// <summary>
    ///     Resets the game.
    /// </summary>
    private void ResetGame()
    {
        if (_titleTxt != null) Destroy(_titleTxt);
        if (_tutText != null) Destroy(_tutText);
        if (_targetText != null) Destroy(_targetText);

        if (_ballToDestroy != null) Destroy(_ballToDestroy);
    }

    /// <summary>
    ///     Shows the main menu.
    /// </summary>
    private void ShowMainMenu()
    {
        _gameState = GUIStates.MainMenu;

        _titleTxt = (GameObject) Instantiate(TitleTxt);
        _titleTxt.transform.parent = transform;
        _titleTxt.GetComponent<TextMesh>().text = "Eur cracy\nGolf!";

        BroadcastMessage("MainMenu");
    }

    /// <summary>
    ///     Set next state (SendMessageUpwards)
    /// </summary>
    internal void IntroEnd()
    {
        ShowMainMenu();
    }

    /// <summary>
    ///     Shows the TargetText (SendMessageUpwards)
    /// </summary>
    /// <param name="data">The data.</param>
    internal void ShowTargetText(StorageStruct data)
    {
        _gameState = GUIStates.Score;

        _targetText = (GameObject) Instantiate(TargetText);
        _targetText.transform.parent = transform;
        _targetText.GetComponent<TextMesh>().text = data.Hits + " Schläge";

        _ballToDestroy = data.Ball;
    }

    /// <summary>
    ///     Called when the GUI is drawn.
    /// </summary>
    internal void OnGUI()
    {
        InitStyles();

        var wh = Screen.width/2.0f;
        var hh = 2.1f*Screen.height/3.0f;

        switch (_gameState)
        {
            case GUIStates.Intro:
                var height = Screen.height;
                GUI.Label(new Rect(10, height - 45, 300, 30), "[Leertaste] zum Überspringen...", _labelStyleSkip);

                break;

            case GUIStates.MainMenu:
                GUI.BeginGroup(new Rect(wh - 200, hh - 150, 400, 300));
                GUI.Box(new Rect(0, 0, 400, 300), "", _boxStyle);

                GUI.Label(new Rect(25, 20, 270, 50), "Schwierigkeitsgrad:", _labelStyleMenu);

                var levels = new[] {ExternalData.ExtData["Level0"], "Mittel", "Schwer", "Overkill"};
                _guiLevel = GUI.SelectionGrid(new Rect(25, 75, 350, 100), _guiLevel, levels, 2, _buttonStyle);

                if (GUI.Button(new Rect(125, 230, 150, 50), "Spielen!", _buttonStyle))
                    StartNewGame();

                GUI.EndGroup();

                break;

            case GUIStates.Score:
                GUI.BeginGroup(new Rect(wh - 165, hh - 65, 390, 130));
                GUI.Box(new Rect(0, 0, 390, 130), "", _boxStyle);

                if (GUI.Button(new Rect(30, 25, 150, 75), "Hauptmenü!", _buttonStyle))
                {
                    ResetGame();
                    ShowMainMenu();
                }

                if (GUI.Button(new Rect(210, 25, 150, 75), "Nochmal!", _buttonStyle))
                {
                    ResetGame();
                    StartNewGame();
                }

                GUI.EndGroup();

                break;
        }
    }

    /// <summary>
    ///     Initializes the styles.
    /// </summary>
    private void InitStyles()
    {
        if (_labelStyleSkip != null) return;

        // styles
        _labelStyleSkip = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Normal,
            normal = {textColor = Color.white}
        };

        _labelStyleMenu = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = {textColor = Color.black}
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = ButtonTextColor,
                background = MakeTex(ButtonBgColor)
            },
            hover = {background = MakeTex(ButtonHoverBgColor)},
            onNormal = {background = MakeTex(ButtonOnNormalBgColor)},
            onHover = {background = MakeTex(ButtonOnHoverBgColor)},
        };

        _boxStyle = new GUIStyle(GUI.skin.button)
        {
            normal = {background = MakeTex(BoxBgColor)},
            hover = {background = MakeTex(BoxHoverBgColor)}
        };
    }

    /// <summary>
    ///     Makes a background tex.
    /// </summary>
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
}