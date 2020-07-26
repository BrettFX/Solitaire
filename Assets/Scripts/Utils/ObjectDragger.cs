using UnityEngine;

namespace Solitaire
{
    public class ObjectDragger : MonoBehaviour
    {
        private Vector3 screenPoint;
        private Vector3 offset;

        private Vector3 startPos;

        void OnMouseDown()
        {
            if (GameManager.DEBUG_MODE) { Debug.Log("Clicked on " + gameObject.name); }

            screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(curScreenPoint);

            // Keep track of the starting position for future validation
            startPos = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        }

        void OnMouseDrag()
        {
            // Only allow dragging cards
            if (gameObject.tag.Equals("Card"))
            {
                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

                // Need to temporarily remove the game object from its stack so that snap manager can update cards in each stack
                transform.parent = null;

                // Set z-value to a large negative number so that the card that is being dragged always appears on top
                curPosition.z = -50.0f;

                transform.position = curPosition;
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
                            // Need to keep the original card's z value so it can be moved again once snapped
                            Vector3 newPos = new Vector3(
                                collidedTransform.position.x, 
                                collidedTransform.position.y, 
                                                       -1.0f
                            );

                            // Add the card to the stack
                            transform.parent = collidedTransform;
                            Debug.Log("Set transform parent to " + transform.parent);

                            transform.position = newPos;
                            valid = true;
                            break;
                        }
                        else if (collidedTransform.tag.Equals("Card"))
                        {
                            // Determine if the card was the same one that is being dragged/dropped
                            if (collidedTransform.Equals(transform))
                            {
                                Debug.Log("Collided object is self, skippig...");
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
