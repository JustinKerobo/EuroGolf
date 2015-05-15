using UnityEngine;

public class TutScript : MonoBehaviour
{
    /// <summary>
    ///     Starts this instance.
    /// </summary>
    internal void Start()
    {
        SetText(1);
    }

    /// <summary>
    ///     Sets the text.
    /// </summary>
    public void SetText(int hit)
    {
        GetComponent<TextMesh>().text = hit + ". Schlag\n\n\n" +
                                        "[A]/[D]: Schlagwinkel\n" +
                                        "[W]/[S]: Schlagstärke\n" +
                                        "[STRG]/[SHIFT]: grob/fein\n\n" +
                                        "[Mausrad]: Zoom\n\n\n" +
                                        "dann [Leertaste] halten und\n" + 
                                        "im richtigen Moment loslassen";
    }
}