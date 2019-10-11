using ScamScatter;
using UnityEngine;

/// <summary>
/// This is an example of how a GameObject can override its scattering behavior.
/// In the demo scene, this is attached to the blue sphere, with values declaring
/// a much more coarse scattering, with fewer and thicker parts than the default.
/// </summary>
public class ScatterOverrideExample : MonoBehaviour, IScatterInstruction
{
    public int TargetPartCount = 50;
    public float TargetArea = 0.4f;
    public float ThicknessMin = 0.3f;
    public float ThicknessMax = 0.35f;

    public void PrepareScatter(ScatterCommands commands)
    {
        commands.Add(new ScatterCommand(gameObject)
        {
            TargetPartCount = TargetPartCount,
            NewThicknessMin = ThicknessMin,
            NewThicknessMax = ThicknessMax,
            TargetArea = TargetArea
        });
    }
}
