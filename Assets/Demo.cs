using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Demo : MonoBehaviour
{
    private string _info = "http://www.github.com/scamscatter";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            SceneManager.LoadScene(0);
        RaycastHit hit;
        if (!Input.GetMouseButtonDown(0) || !Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            return;
        var sw = Stopwatch.StartNew();
        var objCount = ScamScatter.Scatter.Run(hit.transform.gameObject, 50, 1.5f);
        ScamScatter.Explode.Run(hit.point, 1, 2);
        _info = objCount > 0
            ? $"Scattered {hit.transform.name} into {objCount} new game objects in {sw.ElapsedMilliseconds} ms."
            : "Hit already scattered object.";
    }

    private void OnGUI()
    {
        var rect = Screen.safeArea;
        var style = new GUIStyle
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = Screen.height / 30,
            normal = new GUIStyleState {textColor = Color.white}
        };
        GUI.Label(rect,
            "ScamScatter by Dan Byström. Click an object to scatter. Hit Z to reset.", style);
        rect.y += Screen.height / 30f;
        GUI.Label(rect,
            "Note: Compiled ver approx twice as fast as editor. First scatter is always slower that the rest", style);
        rect.y += 2 * Screen.height / 30f;
        GUI.Label(rect, _info, style);

    }

}
