using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Demo : MonoBehaviour
{
    private const float ScrollSpeed = 200;

    private string _info = "http://www.github.com/danbystrom/scamscatter";

    private static readonly string[] Texts =
    {
        "Welcome, brave coder, to the ScamScatter demo.",
        "ScamScatter lets you destruct meshes proceduarally in run-time, without the need to break them apart in a 3D modelling software beforehand.",
        "Keep an eye on this text too see if there are significant frame drops.",
        "You can configure approximately how many parts and/or the desired size of the debris.",
        "Some randomness is thrown into the process, so that a model won't break down in the same ways twice.",
        "Note that the \"explosion\" is just the attaching of rigidbodies to the debris and then Unity does the rest.",
        "You can attach scripts to each GameObject that overrides the default behavior.",
        "The red sphere says it it doesn't want to be scattered at all.",
        "The blue sphere says that it wants to break into fewer and larger pieces than the rest."
    };

    private class ScrollingTextElement
    {
        public readonly GUIContent Text;
        private readonly float _startPos;
        private readonly float _startTime;
        public readonly float Width;
        private readonly float _speed;

        public ScrollingTextElement(string text, float startPos, float speed, GUIStyle style)
        {
            Text = new GUIContent(text);
            _startPos = startPos;
            _startTime = Time.time;
            _speed = speed;
            Width = style.CalcSize(Text).x + 1;
        }

        public float Left => _startPos - (Time.time - _startTime) * _speed;
        public float Right => Left + Width;
    }

    private int _nextText;
    private readonly List<ScrollingTextElement> _scrollingTexts = new List<ScrollingTextElement>();

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
        var explosionRange = building ? 2 : 0.5f;
        var sw = Stopwatch.StartNew();

        new ScamScatter.Scatter { TargetArea = 0.8f, MaxTimeMs = 30 }
        .Run(this, _ =>
        {
            _info = _.NewGameObjects > 0
                ? $"Scattered {hit.transform.name} ({_.SourceTriangles} triangles) into {_.NewGameObjects} new game objects in {sw.ElapsedMilliseconds} ms."
                : "Nothing to scatter.";
            StartCoroutine(delayedExplosion(hit.point));
        },
        hit.transform.gameObject);
    }

    private IEnumerator delayedExplosion(Vector3 position)
    {
        yield return null;
        ScamScatter.Explode.Run(position, 1.5f, 2);
    }

    private void OnGUI()
    {
        var rect = Screen.safeArea;
        var bottom = rect.yMax;
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

        style.alignment = TextAnchor.LowerLeft;
        style.normal = new GUIStyleState { textColor = Color.black };

        if (!_scrollingTexts.Any() || _scrollingTexts.Last().Right < rect.xMax * 0.95f)
            _scrollingTexts.Add(new ScrollingTextElement(Texts[_nextText++ % Texts.Length], rect.xMax, ScrollSpeed, style));

        if (_scrollingTexts.Any() && _scrollingTexts.First().Right < 0)
            _scrollingTexts.RemoveAt(0);

        foreach (var st in _scrollingTexts)
            GUI.Label(new Rect(st.Left, 0, st.Width, bottom), st.Text, style);
    }

}
