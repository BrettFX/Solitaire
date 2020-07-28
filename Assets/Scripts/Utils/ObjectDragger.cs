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

        void OnMouseDown()
        {
            if (GameManager.DEBUG_MODE) { Debug.Log("Clicked on " + gameObject.name); }

            screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(curScreenPoint);

            // Keep track of the starting position for future validation
            startPos = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

            // If card, we need to get a list of the cards that are to be dragged (through the use of the Snap Manager)
            if (gameObject.tag.Equals("Card"))
            {
                // TODO initialize the dragged cards list by referencing the set of cards that are attached to the
                // respective snap that one or many cards are to be dragged from.

                m_draggedCards = GetComponentInParent<SnapManager>().GetCardSet(GetComponent<Card>());
            }
        }

        void OnMouseDrag()
        {
            // Only allow dragging cards
            if (gameObject.tag.Equals("Card"))
            {
                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

                // Need to temporarily remove the game object from its stack so that snap manager can update cards in each stack
                //transform.parent = null;

                // Set z-value to a large negative number so that the card that is being dragged always appears on top
                curPosition.z = -50.0f;

                //transform.position = curPosition;

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
            if (gameObject.tag.Equals("Card"))
            {
                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

                if (GameManager.DEBUG_MODE)
                {
                    Debug.Log("Stopped dragging at: " + curPosition);
                    Debug.Log("Starting point was: " + startPos);
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
                        if (collidedTransform.tag.Equals("Snap"))
                        {
                            // Make sure there isn't already a card attached to the snap (otherwise need to search for card)
                            SnapManager snapManager = collidedTransform.GetComponent<SnapManager>();
                            if (snapManager.HasCard())
                            {
                                Debug.Log("Snap already has a card, skipping...");
                            }
                            else
                            {
                                // Need to keep the original card's z value so it can be moved again once snapped
                                Vector3 newPos = new Vector3(
                                    collidedTransform.position.x,
                                    collidedTransform.position.y,
                                                           -1.0f // Set to a z value of -1 for initial card in stack
                                );

                                // Add the card to the stack
                                transform.parent = collidedTransform;
                                Debug.Log("Set transform parent to " + transform.parent);

                                transform.position = newPos;
                                valid = true;
                                break;
                            }
                        }
                        else if (collidedTransform.tag.Equals("Card"))
                        {
                            // Determine if the card was the same one that is being dragged/dropped
                            if (collidedTransform.Equals(transform))
                            {
                                Debug.Log("Collided object is self, skipping...");
                            }                            
                            else
                            {
                                // Get the card object to determine if the respective card is stackable
                                Card card = collidedTransform.GetComponent<Card>();
                                if (!card.IsStackable())
                                {
                                    Debug.Log("Card is not stackable, skipping...");
                                }
                                else
                                {
                                    // Offset y position by 30 so that the card that is below is still shown
                                    Vector3 newPos = new Vector3(
                                        collidedTransform.position.x,
                                        collidedTransform.position.y - 30.0f,
                                        collidedTransform.position.z - 1.0f
                                    );

                                    // Add the card to the stack
                                    transform.parent = collidedTransform.parent;
                                    Debug.Log("Set transform parent to " + transform.parent);

                                    transform.position = newPos;
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
                    transform.position = startPos;
                }
            }
        }
    }
}
