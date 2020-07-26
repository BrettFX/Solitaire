using UnityEngine;
using static Solitaire.GameManager;

namespace Solitaire
{
    public class Card : MonoBehaviour
    {
        public int value;
        public CardSuit suit;

        private bool m_stackable = true;

        public void SetStackable(bool stackable)
        {
            m_stackable = stackable;
        }

        public bool IsStackable()
        {
            return m_stackable;
        }
    }
}
