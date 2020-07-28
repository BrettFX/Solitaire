using UnityEngine;
using static Solitaire.GameManager;

namespace Solitaire
{
    public class Card : MonoBehaviour
    {
        public int value;
        public CardSuit suit;

        private bool m_stackable = true;
        private Vector3 m_startPos;
        private Transform m_startParent;

        public void SetStackable(bool stackable)
        {
            m_stackable = stackable;
        }

        public bool IsStackable()
        {
            return m_stackable;
        }

        public void SetStartPos(Vector3 pos)
        {
            m_startPos = pos;
        }

        public Vector3 GetStartPos()
        {
            return m_startPos;
        }

        public void SetStartParent(Transform parent)
        {
            m_startParent = parent;
        }

        public Transform GetStartParent()
        {
            return m_startParent;
        }
    }
}
