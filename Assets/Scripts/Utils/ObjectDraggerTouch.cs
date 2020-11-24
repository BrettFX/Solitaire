using UnityEngine;

namespace Solitaire
{
    public class ObjectDraggerTouch : MonoBehaviour
    {
        private const float CLICK_PROXIMITY_THRESHOLD = 100.0f; // Magnitude proximity threshold
        private const float CLICK_DIFF_TIME_THRESHOLD = 0.3f;   // Difference time threshold between clicks

        private Vector3 screenPoint;
        private Vector3 offset;

        private Vector3 startPos;

        // Keep track of the cards that are currently being dragged (can be 1 or many at a time)
        private Card[] m_draggedCards;

        private float m_timeSinceLastClick = -1.0f;
        bool m_isStockCard = false;
        bool m_isDoingAnimation = false;
        bool m_dragged = false;

        private SnapManager m_originSnapManager;

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
            float sqrMagnitude = Vector3.SqrMagnitude(startPos - currentPos);
            return sqrMagnitude <= CLICK_PROXIMITY_THRESHOLD;
        }

        private bool DraggingIsAllowed()
        {
            // Don't allow dragging if doing auto win or if already in a winning state
            return !GameManager.Instance.IsDoingAutoWin() &&
                   !GameManager.Instance.IsWinningState() &&
                   !GameManager.Instance.IsPaused();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                // Determine touch type and process accordingly
                switch(touch.phase)
                {
                    case TouchPhase.Began:
                        Debug.Log("Touch began");
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        // Temporary implementation for testing
                        // See: https://answers.unity.com/questions/1223838/drag-gameobject-with-finger-touch-in-smartphone.html
                        // Get the touch position from the screen touch to world point
                        Vector3 touchedPos = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10));

                        // Lerp and set the position of the current object to that of the touch, but smoothly over time.
                        transform.position = Vector3.Lerp(transform.position, touchedPos, Time.deltaTime);
                        break;
                    case TouchPhase.Ended:
                        Debug.Log("Touch ended");
                        break;
                    case TouchPhase.Canceled:
                        Debug.Log("Touch canceled");
                        break;
                }
            }
        }
    }
}

