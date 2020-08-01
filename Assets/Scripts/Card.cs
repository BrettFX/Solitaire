using UnityEngine;
using static Solitaire.GameManager;

namespace Solitaire
{
    public class Card : MonoBehaviour
    {
        public CardState currentState;

        public int value;
        public CardSuit suit;

        private bool m_stackable = true;
        private bool m_flipped = false;
        private Vector3 m_startPos;
        private Transform m_startParent;

        private bool m_translating = false;

        private Transform m_targetTranslateSnap;
        private Vector3 m_targetTranslatePos;

        // Only used for dynamic card translation animations
        private void Update()
        {
            if (m_translating)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    m_targetTranslatePos,
                    CARD_TRANSLATION_SPEED * Time.deltaTime
                );

                // Stop translating once the x and y values match the translation target
                m_translating = !(
                    transform.position.x == m_targetTranslatePos.x &&
                    transform.position.y == m_targetTranslatePos.y
                );

                // Perform final steps after translation is complete
                if (!m_translating)
                {
                    // Set the z-value to high value to prevent clipping
                    transform.position = new Vector3(transform.position.x, transform.position.y, -50);

                    // Play the respective card's flip animation
                    Animator animator = gameObject.GetComponent<Animator>();
                    animator.SetTrigger(m_flipped ? "FlipForward" : "FlipBackward");

                    // Put the z value of the card to it's original z-value after the animation completes
                    //transform.position = new Vector3(transform.position.x, transform.position.y, m_startPos.z);

                    // Place the card in the respective snap parent
                    //transform.parent = m_targetTranslateSnap;
                }
            }
        }

        public void MoveTo(Transform snap)
        {
            m_targetTranslateSnap = snap;
            m_targetTranslatePos = m_targetTranslateSnap.position;
            m_startPos = transform.position;
            m_flipped = !m_flipped;

            Debug.Log("Card belongs to: " + m_startParent.parent.tag);
            Debug.Log("Translating to: " + m_targetTranslatePos);

            m_translating = true;
        }

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

        public bool IsFlipped()
        {
            return m_flipped;
        }

        public void SetFlipped(bool flip)
        {
            m_flipped = flip;
            currentState = m_flipped ? CardState.FACE_DOWN : CardState.FACE_UP;
        }
    }
}
