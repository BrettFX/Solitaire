using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Solitaire
{
    public class TouchManager : MonoBehaviour
    {
        public static TouchManager Instance { get; private set; }

        private const float CLICK_PROXIMITY_THRESHOLD = 100.0f; // Magnitude proximity threshold
        private const float CLICK_DIFF_TIME_THRESHOLD = 0.3f;   // Difference time threshold between clicks

        private Vector3 m_screenPoint;
        private Vector3 m_offset;

        private Vector3 m_startPos;

        // Keep track of the cards that are currently being dragged (can be 1 or many at a time)
        private Card[] m_draggedCards;

        private float m_timeSinceLastClick = -1.0f;
        bool m_isStockCard = false;
        bool m_isDoingAnimation = false;
        bool m_dragged = false;
        bool m_isCard = false;
        GameObject m_currentObject = null;

        private SnapManager m_originSnapManager;

        private int m_touchCount = 0;

        /**
         * Compare the distance between the starting position and current position
         * of a click by computing the square magnitude of the difference between the
         * two respective vectors. As long as the result of the computation is within
         * the globally defined click threshold then the move is deemed as a click.
         * 
         * @see https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
         * @param Vector3 currentPos the current pointer position to compare against the
         *                           start position.
         *                           
         * @return bool whether or not the move is a click.
         */
        private bool IsClick(Vector3 currentPos)
        {
            // Valid click if within +/- threshold
            float sqrMagnitude = Vector3.SqrMagnitude(m_startPos - currentPos);
            return sqrMagnitude <= CLICK_PROXIMITY_THRESHOLD;
        }

        private bool DraggingIsAllowed()
        {
            // Don't allow dragging if doing auto win or if already in a winning state
            return !GameManager.Instance.IsDoingAutoWin() &&
                   !GameManager.Instance.IsWinningState() &&
                   !GameManager.Instance.IsPaused();
        }

        /**
        * Ensure this class remains a singleton instance
        * */
        void Awake()
        {
            // If the instance variable is already assigned...
            if (Instance != null)
            {
                // If the instance is currently active...
                if (Instance.gameObject.activeInHierarchy == true)
                {
                    // Warn the user that there are multiple Game Managers within the scene and destroy the old manager.
                    Debug.LogWarning("There are multiple instances of the " + this + " script. Removing the old manager from the scene.");
                    Destroy(Instance.gameObject);
                }

                // Remove the old manager.
                Instance = null;
            }

            // Assign the instance variable as the Game Manager script on this object.
            Instance = GetComponent<TouchManager>();
        }

        private void Init()
        {
            m_isCard = false;
            m_currentObject = null;
        }

        // Update is called once per frame
        void Update()
        {
            m_touchCount = Input.touchCount;
            if (m_touchCount > 0)
            {
                // Getting the first touch point (prohibits multi-touch)
                Touch touch = Input.GetTouch(0);

                // Determine touch type and process accordingly
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchDown(touch);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        HandleTouchDrag(touch);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchUp(touch);
                        break;
                }
            }
        }

        /**
         * Get the current touch count to determin how many fingers are
         * presently touching the screen.
         * 
         * @return int the current touch count.
         */
        public int GetTouchCount()
        {
            return m_touchCount;
        }

        /**
         * 
         */
        private void HandleTouchDown(Touch touch)
        {
            // Don't process if dragging isn't currently allowed
            if (!DraggingIsAllowed())
            {
                return;
            }

            Ray raycast = Camera.main.ScreenPointToRay(touch.position);
            if (Physics.Raycast(raycast, out RaycastHit raycastHit))
            {
                m_isCard = raycastHit.collider.CompareTag("Card");
                if (m_isCard)
                {
                    m_currentObject = raycastHit.collider.gameObject;
                }
            }

            // If card, we need to get a list of the cards that are to be dragged (through the use of the Snap Manager)
            if (m_isCard && m_currentObject.transform.parent != null)
            {
                //m_screenPoint = Camera.main.WorldToScreenPoint(m_currentObject.transform.position);
                //Vector3 curScreenPoint = new Vector3(touch.position.x, touch.position.y, m_screenPoint.z);
                //m_offset = m_currentObject.transform.position - Camera.main.ScreenToWorldPoint(curScreenPoint);

                //// Keep track of the starting position for future validation
                //m_startPos = Camera.main.ScreenToWorldPoint(curScreenPoint) + m_offset;

                m_screenPoint = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10));
                //m_offset = m_currentObject.transform.position - Camera.main.ScreenToWorldPoint(m_screenPoint);
                m_startPos = m_screenPoint;

                Card targetCard = m_currentObject.GetComponent<Card>();
                m_isDoingAnimation = targetCard.IsTranslating() || targetCard.IsFlipping();
                if (m_isDoingAnimation)
                {
                    if (GameManager.DEBUG_MODE) Debug.Log("Card is doing an animation and cannot be dragged!");
                    return;
                }

                // Initialize the dragged cards list by referencing the set of cards that are attached to the
                // respective snap that one or many cards are to be dragged from.
                m_draggedCards = m_currentObject.GetComponentInParent<SnapManager>().GetCardSet(targetCard);
                if (m_draggedCards != null && m_draggedCards.Length > 0)
                {
                    // Check the first card to see if it's a stock card
                    if (GameManager.DEBUG_MODE) Debug.Log("Tag Clicked: " + m_draggedCards[0].transform.parent.parent.tag);
                    m_isStockCard = m_draggedCards[0].transform.parent.parent.CompareTag("Stock");
                    if (GameManager.DEBUG_MODE) Debug.Log("Is Stock Card: " + m_isStockCard);

                    // Set each dragged card's start position and parent
                    int i = 0;
                    m_originSnapManager = m_draggedCards[0].GetComponentInParent<SnapManager>();
                    m_originSnapManager.SetWaiting(true); // Wait until cards are dropped and validated before flipping any cards in tableau

                    foreach (Card card in m_draggedCards)
                    {
                        card.SetStartPos(card.transform.position);
                        card.SetStartParent(card.transform.parent);

                        // Temporarily disable the mesh collider for all cards except the first one in the set of dragged cards.
                        if (i != 0)
                            card.GetComponent<MeshCollider>().enabled = false;
                        i++;
                    }
                }
            }
        }

        /**
         * Handle dragging the game object associated with this ObjectDraggerTouch
         * instance. Updates the respective transform with the current touch location.
         * 
         * @param Touch touch the current touch point.
         */
        private void HandleTouchDrag(Touch touch)
        {
            // Don't process if dragging isn't currently allowed
            if (!DraggingIsAllowed())
            {
                return;
            }

            // Don't process if target card is performing an animation
            if (m_isDoingAnimation)
            {
                return;
            }

            // Only allow dragging cards
            if (m_isCard)
            {
                Card topCard = m_currentObject.GetComponent<Card>();

                m_screenPoint = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10));
                //Vector3 curPosition = Camera.main.ScreenToWorldPoint(m_screenPoint) + m_offset;
                Vector3 curPosition = m_screenPoint;
                //Vector3 curPosition = new Vector3(touch.position.x, touch.position.y, 10);

                // Don't animate dragging if within click threshold (smooths up animations)
                if (IsClick(curPosition))
                {
                    return;
                }

                if (m_isStockCard || topCard.IsFaceDown())
                {
                    // Don't allow dragging stock cards or face-down cards
                    return;
                }

                // Set temporary block on actions and events while dragging cards(s)
                if (!GameManager.Instance.IsBlocked())
                {
                    GameManager.Instance.SetBlocked(true);
                    m_dragged = true; // Denote that there was a valid dragging action
                }

                // Set z-value to a large negative number so that the card that is being dragged always appears on top
                curPosition.z = -GameManager.Z_OFFSET_DRAGGING;

                // Need to iterate the set of dragged cards and adjust the position accordingly
                float yOffset = GameManager.FOUNDATION_Y_OFFSET;
                int i = 0;
                try
                {
                    foreach (Card card in m_draggedCards)
                    {
                        // Need to temporarily remove the game object from its stack so that snap manager can update cards in each stack
                        if (card.transform.parent)
                            card.transform.parent = null;

                        Vector3 cardPosition = card.transform.position;
                        Vector3 newCardPos = new Vector3(curPosition.x, curPosition.y - (yOffset * i), curPosition.z - i);
                        card.transform.position = newCardPos;

                        i++;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Unexpected issue with dragging occurred but handled (" + e.GetType() + ")");
                }

                //if (m_currentObject != null)
                //{
                //    Transform t = m_currentObject.transform;
                //    t.position = new Vector3(m_screenPoint.x, m_screenPoint.y, t.position.z);
                //}
            }
        }

        /**
         * 
         */
        private void HandleTouchUp(Touch touch)
        {
            // Don't process if dragging isn't currently allowed
            if (!DraggingIsAllowed())
            {
                Init();
                return;
            }

            // Don't process if target card is performing an animation
            if (m_isDoingAnimation)
            {
                Init();
                return;
            }

            if (m_isCard || (m_currentObject != null && m_currentObject.CompareTag("Snap")))
            {
                m_screenPoint = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10));
                //Vector3 curPosition = Camera.main.ScreenToWorldPoint(m_screenPoint) + m_offset;
                Vector3 curPosition = m_screenPoint;
                if (GameManager.DEBUG_MODE)
                {
                    Debug.Log("Stopped dragging at: " + curPosition);
                    Debug.Log("Starting point was: " + m_startPos);
                }

                // Mouse up will be determined as a click if the current position is the same as the start position
                if (IsClick(curPosition))
                {
                    if (m_currentObject.CompareTag("Snap"))
                    {
                        if (m_currentObject.transform.parent.CompareTag("Stock"))
                        {
                            // Only way to get to this point is if the stock was clicked and there are no cards on it
                            // Transfer all cards attached to talon back to stock
                            GameManager.Instance.SetBlocked(true);
                            GameManager.Instance.ReplinishStock();
                        }
                    }
                    else
                    {
                        Card cardOfInterest = m_currentObject.GetComponent<Card>();
                        if (cardOfInterest.GetStartParent() != null)
                        {
                            string cardParentSetTag = cardOfInterest.GetStartParent().parent.tag;

                            // Determine if double click
                            float timeDiff = Time.timeSinceLevelLoad - m_timeSinceLastClick;
                            if (GameManager.DEBUG_MODE) { Debug.Log("Time difference since last click: " + timeDiff); }
                            bool doubleClick = (m_timeSinceLastClick >= 0) && timeDiff <= CLICK_DIFF_TIME_THRESHOLD;

                            Transform nextMove = null;

                            // Don't apply double-click logic to stock
                            // Also don't permit click spamming for drawing cards (use game manager action/event block)
                            if (cardParentSetTag.Equals("Stock") && !GameManager.Instance.IsBlocked())
                            {
                                // Move the card to the talon pile once it has been clicked on the stock (draw card)
                                GameManager.Instance.SetBlocked(true); // Place temporary lock to prevent concurrent actions/events
                                cardOfInterest.MoveTo(GameManager.Instance.GetTalonPile());
                            }
                            else if (doubleClick)
                            {
                                // Determine the next valid move. Supply the dragged cards count so prevent the scenario in which
                                // more than one card is dragged to a foundation in one event.
                                nextMove = GameManager.Instance.GetNextAvailableMove(cardOfInterest, m_draggedCards.Length);
                                if (GameManager.DEBUG_MODE)
                                {
                                    Debug.Log("Double clicked!");
                                    Debug.Log("Next Move: " + nextMove);
                                }

                                // If double click and there is a valid next move
                                // Then, automatically move the double clicked card to the most appropriate location.
                                if (nextMove)
                                {
                                    // Move all cards in set of dragged cards (can be 1)
                                    GameManager.Instance.SetBlocked(true); // Place temporary lock to prevent concurrent actions/events
                                    cardOfInterest.MoveTo(nextMove, m_draggedCards);

                                    // Have to notify that waiting is complete for destination snap manager
                                    m_originSnapManager.GetComponent<SnapManager>().SetWaiting(false);
                                }
                            }

                            // Handle cleanup case
                            if (!cardParentSetTag.Equals("Stock") && !nextMove)
                            {
                                // Need to set the parent back to the original parent for card(s) in the set of dragged cards.
                                foreach (Card card in m_draggedCards)
                                {
                                    card.transform.parent = card.GetStartParent();

                                    // Ensure all mesh colliders are re-enabled (corner case when double clicking a face down card)
                                    card.GetComponent<MeshCollider>().enabled = true;
                                }

                                // Unblock actions/events since invalid click
                                GameManager.Instance.SetBlocked(false);
                            }
                        }
                    }

                    // Keep track of the time of last click
                    m_timeSinceLastClick = Time.timeSinceLevelLoad;
                    return;
                }

                // Don't allow dropping stock cards or face-down cards
                Card cardCheck = m_currentObject.GetComponent<Card>();
                bool valid = !m_isStockCard && cardCheck != null && !cardCheck.IsFaceDown();

                // Validate the dragged location to determine if the card should be snapped back to original location
                // or snapped to the respective target (e.g., attempted drag location)
                Vector3 collisionVector = new Vector3(10.0f, 10.0f, 1000.0f);
                bool collides = Physics.CheckBox(curPosition, collisionVector);

                if (collides && valid)
                {
                    Collider[] hitColliders = Physics.OverlapBox(curPosition, collisionVector);
                    int i = 0;
                    while (i < hitColliders.Length)
                    {
                        Transform collidedTransform = hitColliders[i].GetComponent<Transform>();
                        if (GameManager.DEBUG_MODE) { Debug.Log("Would collide with object: " + collidedTransform); }

                        // Snap to the snap location if there is one
                        if (collidedTransform.CompareTag("Snap"))
                        {
                            // Don't allow dropping dragged cards in prohibited locations
                            SnapManager snapManager = collidedTransform.GetComponent<SnapManager>();
                            if (GameManager.PROHIBITED_DROP_LOCATIONS.Contains(collidedTransform.parent.tag))
                            {
                                if (GameManager.DEBUG_MODE) Debug.Log("Can't manually drop card in " + collidedTransform.parent.tag);
                                valid = false;
                                break;
                            }

                            // Make sure there isn't already a card attached to the snap (otherwise need to search for card)
                            if (snapManager.HasCard())
                            {
                                if (GameManager.DEBUG_MODE) Debug.Log("Snap already has a card, skipping...");
                            }
                            else
                            {
                                if (GameManager.DEBUG_MODE) Debug.Log("Placing card(s) in: " + collidedTransform.parent.tag);

                                // Set the new position relative to the snap, adjusting the z value appropriately
                                Vector3 newPos = new Vector3(
                                    collidedTransform.position.x,
                                    collidedTransform.position.y,
                                                           -1.0f // Set to a z value of -1 for initial card in stack
                                );

                                // Need to iterate the set of dragged cards and adjust the position accordingly
                                bool isFoundation = collidedTransform.parent.CompareTag("Foundations");

                                // Assert that there is only one card being placed if target is foundation
                                if (isFoundation && m_draggedCards.Length > 1)
                                {
                                    if (GameManager.DEBUG_MODE) Debug.Log("Cannot move more than one card at once to a foundation.");
                                    valid = false;
                                    break;
                                }

                                // General validation step to take card value and suit into consideration
                                valid = snapManager.IsValidMove(m_draggedCards[0]);
                                if (valid)
                                {
                                    float yOffset = isFoundation ? 0.0f : GameManager.FOUNDATION_Y_OFFSET;
                                    int j = 0;
                                    foreach (Card card in m_draggedCards)
                                    {
                                        Vector3 cardPosition = card.transform.position;
                                        Vector3 newCardPos = new Vector3(newPos.x, newPos.y - (yOffset * j), newPos.z - j);
                                        card.transform.position = newCardPos;

                                        // Add the card to the stack
                                        card.transform.parent = collidedTransform;

                                        // Re-enable the mesh colliders on the cards
                                        card.GetComponent<MeshCollider>().enabled = true;

                                        j++;
                                    }
                                }

                                break;
                            }
                        }
                        else if (collidedTransform.CompareTag("Card"))
                        {
                            // Determine if the card was the same one that is being dragged/dropped
                            if (collidedTransform.Equals(m_currentObject.transform))
                            {
                                if (GameManager.DEBUG_MODE) Debug.Log("Collided object is self, skipping...");
                            }
                            else
                            {
                                // Get the card object to determine if the respective card is stackable
                                Card targetCard = collidedTransform.GetComponent<Card>();
                                if (!targetCard.IsStackable())
                                {
                                    if (GameManager.DEBUG_MODE) Debug.Log("Card is not stackable, skipping...");
                                }
                                else
                                {
                                    // Reference the snap manager the card is attached to
                                    SnapManager snapManager = targetCard.GetComponentInParent<SnapManager>();

                                    if (GameManager.DEBUG_MODE) Debug.Log("Placing card(s) in: " + collidedTransform.parent.parent.tag);
                                    bool isFoundation = collidedTransform.parent.parent.CompareTag("Foundations");

                                    // Assert that there is only one card being placed if target is foundation
                                    if (isFoundation && m_draggedCards.Length > 1)
                                    {
                                        if (GameManager.DEBUG_MODE) Debug.Log("Cannot move more than one card at once to a foundation.");
                                        valid = false;
                                        break;
                                    }

                                    // General validation step to take card value and suit into consideration
                                    valid = snapManager.IsValidMove(m_draggedCards[0]);
                                    if (valid)
                                    {
                                        float yOffset = isFoundation ? 0.0f : GameManager.FOUNDATION_Y_OFFSET;

                                        // Offset y position by specified foundation y-offset
                                        // so that the card that is below is still shown
                                        Vector3 newPos = new Vector3(
                                            collidedTransform.position.x,
                                            collidedTransform.position.y - yOffset,
                                            collidedTransform.position.z - 1.0f
                                        );

                                        // Need to iterate the set of dragged cards and adjust the position accordingly
                                        int j = 0;
                                        foreach (Card card in m_draggedCards)
                                        {
                                            Vector3 cardPosition = card.transform.position;
                                            Vector3 newCardPos = new Vector3(newPos.x, newPos.y - (yOffset * j), newPos.z - j);
                                            card.transform.position = newCardPos;

                                            // Add the card to the stack (note that the parent is not the collided transform)
                                            card.transform.parent = collidedTransform.parent;

                                            // Re-enable the mesh colliders on the cards
                                            card.GetComponent<MeshCollider>().enabled = true;

                                            j++;
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            // If collided with anything else other than a card or a snap then deemed as invalid
                            valid = false;
                        }

                        i++;
                    }
                }

                if (!valid)
                {
                    if (GameManager.DEBUG_MODE) Debug.Log("Invalid Move.");

                    // If the drag location is deemed invalid then we should snap back to starting position
                    // Need to iterate the list of dragged cards and set each card back to their respective 
                    // starting position and starting parent
                    if (m_draggedCards != null)
                    {
                        foreach (Card card in m_draggedCards)
                        {
                            // Hanle corner case for when origin was the stock pile
                            if (m_originSnapManager.BelongsTo(GameManager.Sections.STOCK))
                            {
                                Vector3 startPos = card.GetStartPos();
                                bool samePos = card.transform.position.x == startPos.x &&
                                               card.transform.position.y == startPos.y;
                                // Only handle this corner case if the card's curren position isn't the same
                                // as it's starting position.
                                if (!samePos)
                                {
                                    Transform talonTransform = GameManager.Instance.GetTalonPile();
                                    card.transform.position = talonTransform.position;
                                    card.transform.parent = talonTransform;
                                }
                            }
                            else
                            {
                                card.transform.position = card.GetStartPos();
                                card.transform.parent = card.GetStartParent();
                            }

                            // Re-enable the mesh colliders on the cards
                            card.GetComponent<MeshCollider>().enabled = true;
                        }
                    }
                }
                else
                {
                    // Register the manual move if it was valid
                    Move move = new Move();
                    move.SetCards(m_draggedCards);
                    move.SetPreviousParent(m_originSnapManager.transform);
                    move.SetNextParent(m_draggedCards[0].transform.parent);
                    GameManager.Instance.AddMove(move, Move.MoveTypes.NORMAL);

                    // Play the card set sound one shot so that other clips can play at the same time
                    AudioSource cardSetSound = SettingsManager.Instance.cardSetSound;
                    cardSetSound.PlayOneShot(SettingsManager.Instance.cardSetSoundClip);

                    // Track the total number of moves with stats manager
                    StatsManager.Instance.TallyMove();
                }

                // Can stop waiting now that the move is complete
                // Evaluate only if origin snap manager isn't null
                if (m_originSnapManager != null)
                    m_originSnapManager.SetWaiting(false);

                // Remove temporary locks set during dragging
                if (m_dragged)
                {
                    GameManager.Instance.SetBlocked(false);
                    m_dragged = false;
                }
            }

            // Always initialize as a last step
            Init();
        }
    }
}


