using System.Collections;
using System.Collections.Generic;
using ScamScatter;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HalfBakedDemoSceneScript : MonoBehaviour
{
    private enum Method
    {
        Pure,
        HalfBakedGeometry,
        HalfBakedGameObjects
    }

    private static Method _method;

    private string _feedback = "";

    void Start()
    {
        foreach (var x in FindObjectsOfType<ExplodeOnImpactScript>())
        {
            x.StatsReport = (stats, o) => _feedback += $"Scattered {o.name} in {stats.RunningTime} ms\r\n";
            if (_method == Method.Pure)
                continue;
            var sshbo = x.gameObject.AddComponent<ScamScatterHalfBakedObject>();
            if (_method == Method.HalfBakedGameObjects)
                sshbo.bakingMethod = ScamScatterHalfBakedObject.BakingMethod.GameObjects;
        }
    }

    void OnGUI()
    {
        var w = Screen.width / 100f;
        var h = Screen.height / 100f;

        GUI.skin.button.fontSize = (int) (w * 2);
        GUI.skin.box.fontSize = (int)(w * 2);
        GUI.backgroundColor = Color.black;

        var r = new Rect(w, h, w * 30, h * 6);
        drawAndCheckButton(r, Method.Pure, "Real-time");
        r.y += h * 8;
        drawAndCheckButton(r, Method.HalfBakedGeometry, "Half Baked Geometry");
        r.y += h * 8;
        drawAndCheckButton(r, Method.HalfBakedGameObjects, "Half Baked GameObjects");

        r = new Rect(w, h * 80, w * 98, h * 19);
        GUI.Label(r, _feedback);
    }

    private void drawAndCheckButton(Rect r, Method method, string text)
    {
        GUI.backgroundColor = method == _method ? Color.blue : Color.white;
        if (!GUI.Button(r, text))
            return;
        _method = method;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
