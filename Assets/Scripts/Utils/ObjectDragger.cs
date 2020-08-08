using UnityEngine;

namespace Solitaire
{
    public class ObjectDragger : MonoBehaviour
    {
        private Vector3 screenPoint;
        private Vector3 offset;

        private Vector3 startPos;

        // Keep track of the cards that are currently being dragged (can be 1 or many at a time)
        private Card[] m_draggedCards;

        private int m_clickCount = 0;
        bool m_isStockCard = false;

        void OnMouseDown()
        {
            if (GameManager.DEBUG_MODE) { Debug.Log("Clicked on " + gameObject.name); }

            screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(curScreenPoint);

            // Keep track of the starting position for future validation
            startPos = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

            // If card, we need to get a list of the cards that are to be dragged (through the use of the Snap Manager)
            if (gameObject.CompareTag("Card"))
            {

                // Initialize the dragged cards list by referencing the set of cards that are attached to the
                // respective snap that one or many cards are to be dragged from.
                m_draggedCards = GetComponentInParent<SnapManager>().GetCardSet(GetComponent<Card>());

                // Check the first card to see if it's a stock card
                Debug.Log("Tag Clicked: " + m_draggedCards[0].transform.parent.parent.tag);
                m_isStockCard = m_draggedCards[0].transform.parent.parent.CompareTag("Stock");
                Debug.Log("Is Stock Card: " + m_isStockCard);

                // Set each dragged card's start position and parent
                int i = 0;
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

        void OnMouseDrag()
        {
            // Only allow dragging cards
            if (gameObject.tag.Equals("Card"))
            {
                if (m_isStockCard)
                {
                    // Don't allow dragging stock cards
                    return;
                }

                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

                // Set z-value to a large negative number so that the card that is being dragged always appears on top
                curPosition.z = -GameManager.Z_OFFSET;

                // Need to iterate the set of dragged cards and adjust the position accordingly
                float yOffset = 30.0f;
                int i = 0;
                foreach (Card card in m_draggedCards)
                {
                    // Need to temporarily remove the game object from its stack so that snap manager can update cards in each stack
                    card.transform.parent = null;

                    Vector3 cardPosition = card.transform.position;
                    Vector3 newCardPos = new Vector3(curPosition.x, curPosition.y - (yOffset * i), curPosition.z - i);
                    card.transform.position = newCardPos;
                    i++;
                }
            }
        }

        private void OnMouseUp()
        {
            // Only process if it was a card being dragged
            if (gameObject.CompareTag("Card") || gameObject.CompareTag("Snap"))
            {
                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

                if (GameManager.DEBUG_MODE)
                {
                    //Debug.Log("Stopped dragging at: " + curPosition);
                    //Debug.Log("Starting point was: " + startPos);
                }

                // Mouse up will be determined as a click if the current position is the same as the start position
                if (curPosition.Equals(startPos))
                {
                    if (gameObject.tag.Equals("Snap"))
                    {
                        if (transform.parent.CompareTag("Stock"))
                        {
                            // Only way to get to this point is if the stock was clicked and there are no cards on it
                            // Transfer all cards attached to talon back to stock
                            GameManager.Instance.ReplinishStock();
                        }
                    }
                    else
                    {
                        Card cardOfInterest = gameObject.GetComponent<Card>();
                        string cardParentSetTag = cardOfInterest.GetStartParent().parent.tag;

                        if (cardParentSetTag.Equals("Stock"))
                        {
                            m_clickCount++;
                            Debug.Log("Click count: " + m_clickCount);

                            // Move the card to the talon pile once it has been clicked on the stock
                            cardOfInterest.MoveTo(GameManager.Instance.GetTalonPile());
                        }
                        else
                        {
                            // Need to set the parent back to the original parent for card(s) in the set of dragged cards.
                            foreach (Card card in m_draggedCards)
                            {
                                card.transform.parent = card.GetStartParent();
                            }
                        }
                    }

                    return;
                }

                if (m_isStockCard)
                {
                    // Don't allow dropping stock cards
                    return;
                }

                // Validate the dragged location to determine if the card should be snapped back to original location
                // or snapped to the respective target (e.g., attempted drag location)
                //bool collides = Physics.CheckSphere(curPosition, 5.0f);
                Vector3 collisionVector = new Vector3(10.0f, 10.0f, 1000.0f);
                bool collides = Physics.CheckBox(curPosition, collisionVector);
                bool valid = false;
                
                if (collides)
                {
                    //Collider[] hitColliders = Physics.OverlapSphere(curPosition, 5.0f);
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
                            if (GameManager.PROHIBITED_DROP_LOCATIONS.Contains(collidedTransform.parent.tag))
                            {
                                Debug.Log("Can't manually drop card in " + collidedTransform.parent.tag);
                                valid = false;
                                break;
                            }

                            // Make sure there isn't already a card attached to the snap (otherwise need to search for card)
                            SnapManager snapManager = collidedTransform.GetComponent<SnapManager>();
                            if (snapManager.HasCard())
                            {
                                Debug.Log("Snap already has a card, skipping...");
                            }
                            else
                            {
                                Debug.Log("Placing card(s) in: " + collidedTransform.parent.tag);

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
                                    Debug.Log("Cannot move more than one card at once to a foundation.");
                                    valid = false;
                                    break;
                                }

                                float yOffset = isFoundation ? 0.0f : 30.0f;
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

                                valid = true;
                                break;
                            }
                        }
                        else if (collidedTransform.CompareTag("Card"))
                        {
                            // Determine if the card was the same one that is being dragged/dropped
                            if (collidedTransform.Equals(transform))
                            {
                                Debug.Log("Collided object is self, skipping...");
                            }                            
                            else
                            {
                                // Get the card object to determine if the respective card is stackable
                                Card targetCard = collidedTransform.GetComponent<Card>();
                                if (!targetCard.IsStackable())
                                {
                                    Debug.Log("Card is not stackable, skipping...");
                                }
                                else
                                {
                                    Debug.Log("Placing card(s) in: " + collidedTransform.parent.parent.tag);
                                    bool isFoundation = collidedTransform.parent.parent.CompareTag("Foundations");

                                    // Assert that there is only one card being placed if target is foundation
                                    if (isFoundation && m_draggedCards.Length > 1)
                                    {
                                        Debug.Log("Cannot move more than one card at once to a foundation.");
                                        valid = false;
                                        break;
                                    }

                                    float yOffset = isFoundation ? 0.0f : 30.0f;

                                    // Offset y position by 30 so that the card that is below is still shown
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

                                    valid = true;
                                    break;
                                }
                            }
                        }

                        i++;
                    }
                }

                if (!valid)
                {
                    // If the drag location is deemed invalid then we should snap back to starting position
                    // Need to iterate the list of dragged cards and set each card back to their respective 
                    // starting position and starting parent
                    foreach(Card card in m_draggedCards)
                    {
                        card.transform.position = card.GetStartPos();
                        card.transform.parent = card.GetStartParent();

                        // Re-enable the mesh colliders on the cards
                        card.GetComponent<MeshCollider>().enabled = true;
                    }
                }
            }
        }
    }
}
