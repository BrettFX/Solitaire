using UnityEngine;

namespace Solitaire
{
    public struct CardTpl
    {
        public int value;
        public Card.CardSuit suit;
    }

    public class Card : MonoBehaviour
    {
        public enum CardState
        {
            FACE_UP, FACE_DOWN
        };

        public enum CardSuit
        {
            HEARTS,
            DIAMONDS,
            CLUBS,
            SPADES,
            NONE
        };

        public enum CardSuitColor
        {
            RED,
            BLACK,
            NONE
        }

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

        private volatile bool m_translating = false;

        private Transform m_targetTranslateSnap;
        private Vector3 m_targetTranslatePos;

        private Card[] m_draggedCards;

        private float m_totalTime = 0.0f;

        private Move m_move;
        private Move.MoveTypes m_moveType = Move.MoveTypes.NORMAL;

        // Only used for dynamic card translation animations
        private void Update()
        {
            if (m_translating)
            {
                if (m_draggedCards != null && m_draggedCards.Length > 1)
                {
                    foreach (Card card in m_draggedCards)
                    {
                        Vector3 modTargetPos = card.GetTargetTranslatePosition();

                        // Use linear interpolation (lerp) to move from point a to point b within a specific amount of time
                        card.transform.position = Vector3.Lerp(
                            card.GetStartPos(),
                            modTargetPos,
                            m_totalTime
                        );

                        // Stop translating once the total time has elapsed for linear interpolation
                        m_translating = m_totalTime < 1.0f;
                    }
                }
                else
                {
                    // Use linear interpolation (lerp) to move from point a to point b within a specific amount of time
                    transform.position = Vector3.Lerp(
                        GetStartPos(),
                        m_targetTranslatePos,
                        m_totalTime
                     );

                    // Stop translating once the total time has elapsed for linear interpolation
                    m_translating = m_totalTime < 1.0f;
                }

                // Accumulate total time with respect to the card translation speed
                m_totalTime += Time.deltaTime / GameManager.CARD_TRANSLATION_SPEED;

                // Perform final steps after translation is complete
                if (!m_translating)
                {
                    SnapManager targetSnapManager = m_targetTranslateSnap.GetComponent<SnapManager>();

                    if (m_draggedCards != null && m_draggedCards.Length > 1)
                    {
                        // Don't need to check current state when dragging more than one card
                        // since the only case when the card is face down is if it came from the
                        // stock pile.
                        foreach (Card card in m_draggedCards)
                        {
                            // Place the card in the respective snap parent (this ensures proper card order)
                            card.transform.parent = m_targetTranslateSnap;

                            // Re-enable the mesh colliders on the cards
                            card.GetComponent<MeshCollider>().enabled = true;
                        }
                    }
                    else
                    {
                        // Only flip card if it's face down and processing normal move
                        if (currentState.Equals(CardState.FACE_DOWN) && m_moveType.Equals(Move.MoveTypes.NORMAL))
                        {
                            // Flip the card without an animation
                            Flip();

                            // Stage the event
                            Event evt = new Event();
                            evt.SetType(Event.EventType.FLIP);
                            evt.SetCard(this);
                            // Setting relative snap manager to this instance for locking when reversing event
                            evt.SetRelativeSnapManager(targetSnapManager);
                            GameManager.Instance.AddEventToLastMove(evt);
                        }

                        // Place the card in the respective snap parent
                        transform.parent = m_targetTranslateSnap;

                        // Re-enable the mesh colliders on this card
                        GetComponent<MeshCollider>().enabled = true;
                    }

                    // Need to remove any locks and blocks on parent snap manager and game manager instance caused by events
                    targetSnapManager.SetWaiting(false);
                    GameManager.Instance.SetBlocked(false);

                    // Reset the total time for correct linear interpolation (lerp)
                    m_totalTime = 0.0f;

                    // Play the card set sound
                    GameManager.Instance.cardSetSound.Play();
                }
            }
        }

        /**
         * Move this card instance and/or other cards in it's card set to the specified target snap transform
         * @param Transform snap the target snap transform to move this card to
         * @param Card[] cardSet the set of dragged cards to handle translation all at once
         *                       with respect to this card instance. Defaults to null if a card set is not provided.
         *                       If the card set has only one card in it then it's assumed that the one card is
         *                       this card instance and will be processed as such.
         * @param MoveTypes moveType the type of move that determines how the move should be tracked in the GameManager.
         */
        public void MoveTo(Transform snap, Card[] cardSet = null, Move.MoveTypes moveType = Move.MoveTypes.NORMAL)
        {
            // Prepare the move object
            m_move = new Move();
            m_moveType = moveType;

            // Set the target card based on value of card set
            // (if card set is null then create a new card set with this card as the only element)
            m_move.SetCards(cardSet ?? (new Card[] { this }));

            // We know that the card has/had a parent
            m_move.SetPreviousParent(m_startParent);

            // Need to get what the snap belongs to so that the card is placed in the correct location
            SnapManager snapManager = snap.GetComponent<SnapManager>();
            bool faceDownTarget = snapManager.HasCard() && snapManager.GetTopCard().IsFaceDown();

            GameManager.Sections targetSection = snapManager.belongsTo;

            // Set the next parent in the move
            m_move.SetNextParent(snapManager.transform);

            // Need to target the top card in the respective tableau pile and offset the y and z positions
            Transform tableauHasCardTarget = targetSection.Equals(GameManager.Sections.TABLEAU) && snapManager.HasCard() ?
                                                                     snapManager.GetTopCard().transform : snap;

            // Setting for reference to new parent snap
            m_targetTranslateSnap = snap;
            m_targetTranslatePos = m_targetTranslateSnap.position; // Defaults to target snap position

            // Keep track of the starting position
            m_startPos = transform.position;

            m_draggedCards = cardSet;

            // Process a bit differently if a card set has been provided
            if (m_draggedCards != null && m_draggedCards.Length > 1)
            {
                // Do a first pass-through to remove parent and bring z-value of each of the dragged cards
                // to the z-offset dragging value
                for (int i = 0; i < m_draggedCards.Length; i++)
                {
                    Card draggedCard = m_draggedCards[i];
                    draggedCard.transform.parent = null;

                    // Keep track of each card's starting position
                    draggedCard.SetStartPos(draggedCard.transform.position);

                    float yOffset;
                    // Apply y-offset when dragging multiple cards (start without y-offset if there isn't a card on the snap)
                    // Handle case when action was an undo and the top card in the target snap is facedown
                    // (only the first card in the the set of dragged cards should have the face down y-offset applied in this case).
                    if (faceDownTarget && i == 0)
                    {
                        yOffset = GameManager.FACE_DOWN_Y_OFFSET;
                    }
                    else
                    {
                        if (faceDownTarget)
                        {
                            // Need to compensate for the fact that the first card applied face down y-offset
                            yOffset = (GameManager.FOUNDATION_Y_OFFSET * i) + GameManager.FACE_DOWN_Y_OFFSET;
                        }
                        else
                        {
                            // Process normally (e.g., not undoing a flip event)
                            yOffset = GameManager.FOUNDATION_Y_OFFSET * (snapManager.HasCard() ? i + 1 : i);
                        }
                    }

                    Vector3 newTargetPos = new Vector3(
                       tableauHasCardTarget.position.x,
                       tableauHasCardTarget.position.y - yOffset,
                       tableauHasCardTarget.position.z - (i + 1)
                    );

                    // Set the new translate position for the dragged card
                    draggedCard.SetTargetTranslatePosition(newTargetPos);

                    // Set the z-value of the transform to move to be high enough to hover over all other cards
                    draggedCard.transform.position = new Vector3(
                        draggedCard.transform.position.x,
                        draggedCard.transform.position.y,
                        -GameManager.Z_OFFSET_DRAGGING - i
                    );
                }
            }
            else // Otherwise, process on one card
            {
                // transform position is a special case for the Tableau cards due to y-offset in addition to z-offset
                if (targetSection.Equals(GameManager.Sections.TABLEAU) && snapManager.HasCard())
                {

                    Vector3 newTargetPos = new Vector3(
                       tableauHasCardTarget.position.x,
                       tableauHasCardTarget.position.y - (faceDownTarget ? GameManager.FACE_DOWN_Y_OFFSET : GameManager.FOUNDATION_Y_OFFSET),
                       tableauHasCardTarget.position.z - 1
                    );

                    m_targetTranslatePos = newTargetPos;
                }

                transform.parent = null;         // Temporarily detatch from the parent
                SetStartPos(transform.position); // Keep track of this card instance start position

                // Set the z-value of the transform to move to be high enough to hover over all other cards
                transform.position = new Vector3(
                    transform.position.x,
                    transform.position.y,
                    -GameManager.Z_OFFSET_DRAGGING
                );
            }

            // Add the move to the game manager instance (only if normal move and not undone or redone)
            if (moveType == Move.MoveTypes.NORMAL)
                GameManager.Instance.AddMove(m_move, moveType);

            // Begin the translating animation
            m_translating = true;                           
        }

        public void SetTargetTranslatePosition(Vector3 targetPos)
        {
            m_targetTranslatePos = targetPos;
        }

        public Vector3 GetTargetTranslatePosition()
        {
            return m_targetTranslatePos;
        }

        public void SetStackable(bool stackable)
        {
            m_stackable = stackable;
        }

        public bool IsStackable()
        {
            return m_stackable;
        }

        public bool IsTranslating()
        {
            return m_translating;
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

        public CardState Flip()
        {
            m_flipped = !m_flipped;
            currentState = m_flipped ? CardState.FACE_UP : CardState.FACE_DOWN;

            // Keep track of original parent so that it can be restored after flipping
            Transform originalParent = transform.parent;
            transform.parent = null; // Temporarily detach from parent

            if (GameManager.ANIMATIONS_ENABLED)
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
