using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Demo : MonoBehaviour
{
    private string _info = "http://www.github.com/danbystrom/scamscatter";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            SceneManager.LoadScene(0);
        RaycastHit hit;
        if (!Input.GetMouseButtonDown(0) || !Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            return;
        var building = hit.transform.name == "Cottage"
            || hit.transform.name == "house1"
            || hit.transform.name == "house2";
        var area = building ? 1.5f : 0.7f;
        var explosionRange = building ? 2 : 0.5f;
        var sw = Stopwatch.StartNew();

        var commands = new ScamScatter.ScatterCommands();
        commands.Add(hit.transform.gameObject);
        new ScamScatter.Scatter { TargetArea = 0.8f, MaxTimeMs = 40 }
        .Run(this, _ =>
        {
            _info = _.NewGameObjects > 0
                ? $"Scattered {hit.transform.name} ({_.SourceTriangles} triangles) into {_.NewGameObjects} new game objects in {sw.ElapsedMilliseconds} ms."
                : "Hit already scattered object.";
            ScamScatter.Explode.Run(hit.point, 1.5f, 2);
        },
        commands);
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
