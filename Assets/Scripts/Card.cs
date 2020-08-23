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
                    // Only flip card if it's face down
                    if (currentState.Equals(CardState.FACE_DOWN))
                    {
                        // Flip the card without an animation
                        Flip(false);
                    }

                    // Place the card in the respective snap parent
                    transform.parent = m_targetTranslateSnap;
                }
            }
        }

        /**
         * Move this card to specified the target snap transform
         * @param Transform snap the target snap transform to move this card to
         * @param float yOffset the y-offset for handling multiple card translations at once
         *                      defaults to the GameManager FOUNDATION_Y_OFFSET.
         */
        public void MoveTo(Transform snap, float yOffset = FOUNDATION_Y_OFFSET)
        {
            // Need to get what the snap belongs to so that the card is placed in the correct location
            SnapManager snapManager = snap.GetComponent<SnapManager>();
            Sections targetSection = snapManager.belongsTo;

            // Setting for reference to new parent snap
            m_targetTranslateSnap = snap;
            Debug.Log("Target translate snap: " + m_targetTranslateSnap);

            // Keep track of the starting position
            m_startPos = transform.position;

            // transform position is a special case for the Tableau cards due to y-offset in addition to z-offset
            if (targetSection.Equals(Sections.TABLEAU) && snapManager.HasCard())
            {
                // Need to target the top card in the respective tableau pile and offset the y and z positions
                Transform newBaseTarget = snapManager.GetTopCard().transform;
                Vector3 newTargetPos = new Vector3(
                   newBaseTarget.position.x,
                   newBaseTarget.position.y - yOffset,
                   newBaseTarget.position.z - 1
               );

               m_targetTranslatePos = newTargetPos;
            }
            else
            {
                m_targetTranslatePos = m_targetTranslateSnap.position;
            }

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

        public bool IsFaceDown()
        {
            return currentState.Equals(CardState.FACE_DOWN);
        }

        public CardState Flip(bool animate)
        {
            m_flipped = !m_flipped;
            currentState = m_flipped ? CardState.FACE_UP : CardState.FACE_DOWN;

            // Keep track of original parent so that it can be restored after flipping
            Transform originalParent = transform.parent;
            transform.parent = null; // Temporarily detach from parent

            if (animate)
            {
                // TODO implement flip animation

            }

            // Need to temporarily disable mesh collider and remove from parent when doing rotation
            MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
            meshCollider.enabled = false;

            int degrees = m_flipped ? 180 : 0;
            transform.rotation = Quaternion.Euler(0, degrees, 0);   // Flip the card 180 degrees about the y axis
            meshCollider.enabled = true;                            // Re-enable the mesh collider

            // Re-attach to parent
            transform.parent = originalParent;
            return currentState;
        }
    }
}
