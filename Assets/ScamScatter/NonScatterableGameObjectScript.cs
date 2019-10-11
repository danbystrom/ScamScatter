using UnityEngine;

namespace ScamScatter
{
    /// <summary>
    /// Just add this script to a GameObject and ScamScatter will refuse to scatter it
    /// </summary>
    public class NonScatterableGameObjectScript : MonoBehaviour, IScatterInstruction
    {
        public void PrepareScatter(ScatterCommands commands)
        {
        }
    }
}
