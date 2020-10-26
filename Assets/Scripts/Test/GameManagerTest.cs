using UnityEngine;
using UnityEngine.UI;

namespace Solitaire
{
    public class GameManagerTest : MonoBehaviour
    {
        [Header("Card Flip Animation Unit Test")]
        public Card[] testCards;

        public void Start()
        {
            foreach (Card card in testCards)
            {
                Animator animator = card.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/Card");
                animator.applyRootMotion = true; // Do animation on root game object only
                animator.Rebind();
            }
        }

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

        public void FlipCardTest()
        {
            foreach(Card card in testCards)
            {
                card.Flip();
            }
        }
    }
}

