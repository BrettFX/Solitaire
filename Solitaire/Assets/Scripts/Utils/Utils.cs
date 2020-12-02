using UnityEngine;

namespace Solitaire
{
    public class Utils
    {
        public static string GetTimestamp(float milliseconds)
        {
            float t = milliseconds / 1000.0f;
            float remainderMillis = (Mathf.Floor(t * 100) % 100); // calculate the milliseconds for the timer

            int seconds = (int)(t % 60); // return the remainder of the seconds divide by 60 as an int
            t /= 60; // divide current time y 60 to get minutes
            int minutes = (int)(t % 60); //return the remainder of the minutes divide by 60 as an int
            t /= 60; // divide by 60 to get hours
            int hours = (int)(t % 24); // return the remainder of the hours divided by 60 as an int

            return string.Format("{0}:{1}:{2}.{3}", hours.ToString("00"), minutes.ToString("00"), seconds.ToString("00"), remainderMillis.ToString("00"));
        }

        /**
         * Get the nearest snap manager to a given card. This function assumes that
         * the given card is not translating and not flipping (e.g., not performing any
         * animations). This function uses a collision vector that works like a 
         * laser pointer from the origin of the card snap about the y-axis and z-axis to determine
         * if there are any snap managers nearby vertically. This
         * function is intended to be used only if the card has been double clicked.
         * 
         * @param card the card to use as reference in determining the nearest snap manager.
         * @return the nearest snap manager. Returns null if no nearest snap manager exists.
         */
        public static SnapManager GetNearestSnapManager(Card card)
        {
            SnapManager nearestSnap = null;

            if (card != null)
            {
                Vector3 collisionVector = new Vector3(10.0f, 10.0f, 1000.0f);
                Collider[] hitColliders = Physics.OverlapBox(card.transform.position, collisionVector);

                // First pass will be to find the snap manager
                // Iterate backwards to start with the nearest collisions relative to the card
                for (int i = hitColliders.Length - 1; i >= 0; i--)
                {
                    Collider hitCollider = hitColliders[i];
                    if (hitCollider.transform.CompareTag("Snap"))
                    {
                        // No need to continue once the snap has been found
                        nearestSnap = hitCollider.transform.GetComponent<SnapManager>();
                        break;
                    }
                }

                // If a snap couldn't be found then attempt to get the parent snap off of a collided card
                if (nearestSnap == null)
                {
                    // Iterate backwards to start with the nearest collisions relative to the card
                    for (int i = hitColliders.Length - 1; i >= 0; i--)
                    {
                        Collider hitCollider = hitColliders[i];
                        if (hitCollider.transform.CompareTag("Card"))
                        {
                            // Attempt to get snap from collided card
                            Card collidedCard = hitCollider.transform.GetComponent<Card>();
                            nearestSnap = collidedCard.GetComponentInParent<SnapManager>();

                            // No need to continue once the snap has been found
                            if (nearestSnap != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return nearestSnap;
        }
    }
}

