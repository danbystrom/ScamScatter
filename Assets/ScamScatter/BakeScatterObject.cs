using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

#if (UNITY_EDITOR)

namespace ScamScatter
{
    public class BakeScatterObject : ScriptableWizard
    {
        public GameObject objectToBakeSplit;
        public int targetPartCount = 50;
        public float TargetArea = 0.4f;
        public float NewThicknessMin = 0.3f;
        public float NewThicknessMax = 0.35f;

        [MenuItem("Procedural/BakeScamScatterObject")]
        static void CreateWizard()
        {
            DisplayWizard<BakeScatterObject>("Bake ScamScatter object");
        }

        public void OnWizardCreate()
        {
            if (objectToBakeSplit == null)
            {
                EditorUtility.DisplayDialog("Scam Scatter", "You must first select an object to scatter!", "OK");
                return;
            }
            var newObject = new GameObject {name = "BakedScamScatter_" + objectToBakeSplit.name};
            newObject.transform.position = objectToBakeSplit.transform.position;
            newObject.transform.rotation = objectToBakeSplit.transform.rotation;
            newObject.transform.localScale = objectToBakeSplit.transform.localScale;
            ScamScatter.Scatter.Run(
                new ScatterCommands
                {
                    new ScatterCommand(objectToBakeSplit, newObject.transform)
                    {
                        Destroy = false,
                        TargetPartCount = targetPartCount,
                        TargetArea = TargetArea,
                        NewThicknessMin = NewThicknessMin,
                        NewThicknessMax = NewThicknessMax
                    }
                });
        }

    }

}

#endif
