using ScamScatter;
using UnityEngine;

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
