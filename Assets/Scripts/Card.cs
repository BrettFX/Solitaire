using UnityEngine;
using static Solitaire.GameManager;

namespace Solitaire
{
    public struct CardTpl
    {
        public int value;
        public CardSuit suit;
    }

    public class Card : MonoBehaviour
    {
        [Header("Card Settings")]
        public CardState currentState;
        public int value;
        public CardSuit suit;
        public GameObject frontFace;
        public GameObject backFace;

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
                    // Need to temporarily disable mesh collider and remove from parent when doing rotation
                    MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
                    meshCollider.enabled = false;
                    
                    transform.rotation = Quaternion.Euler(0, 180, 0);   // Flip the card 180 degrees about the y axis
                    meshCollider.enabled = true;                        // Re-enable the mesh collider

                    // Place the card in the respective snap parent
                    transform.parent = m_targetTranslateSnap;
                }
            }
        }

        /**
         * Move this card to specified the target snap transform
         * @param Transform snap the target snap transform to move this card to
         */
        public void MoveTo(Transform snap)
        {
            m_targetTranslateSnap = snap;
            m_targetTranslatePos = m_targetTranslateSnap.position;
            m_startPos = transform.position;
            m_flipped = !m_flipped;

            // Set the z-value of the transform to move to be high enough to hover over all other cards
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                -Z_OFFSET
            );
           
            transform.parent = null;     // Temporarily detatch from the parent
            m_translating = true;        // Begin the translating animation
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
