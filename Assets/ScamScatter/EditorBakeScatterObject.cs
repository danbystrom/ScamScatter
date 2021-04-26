using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace ScamScatter
{
    public class EditorBakeScatterObject : ScriptableWizard
    {
        public GameObject objectToBake;
        public float targetArea = 2f;
        public float thicknessMin = 0.3f;
        public float thicknessMax = 0.4f;

        [MenuItem("Procedural/Bake ScamScatter Object")]
        static void CreateWizard()
        {
            var me = DisplayWizard<EditorBakeScatterObject>("Bake ScamScatter object");
            me.objectToBake = Selection.activeTransform.gameObject;
        }

        public void OnWizardCreate()
        {
            if (objectToBake== null)
            {
                errorString = "You must first select an object to scatter!";
                return;
            }

            var newObject = Scatter2.CreateEmptyBakedGameObject(objectToBake);
            Scatter2.Run(
                new ScatterCommand(objectToBake, newObject.transform)
                {
                    Destroy = false,
                    DontCheckForBaking = true,
                    TargetPartCount = 50,
                    TargetArea = targetArea,
                    NewThicknessMin = thicknessMin,
                    NewThicknessMax = thicknessMax
                });
            Selection.activeTransform = newObject.transform;
        }

    }

}

#endif
