using UnityEngine;
using UnityEngine.UI;

namespace Solitaire
{
    public class GameManagerTest : MonoBehaviour
    {
        public void SimulateAutoWin(Toggle toggle)
        {
            Debug.Log("Turning " + (toggle.isOn ? "on" : "off") + " auto win state...");
            SimulateAutoWin(toggle.isOn);
        }

        /**
         * Find all object dragger instances and enable or disable them based
         * on the specified toggle flag.
         */
        private void SimulateAutoWin(bool enabled)
        {
            GameManager.Instance.SetDoingAutoWin(enabled);
        }
    }
}

