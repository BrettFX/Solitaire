using UnityEngine;

namespace Solitaire
{
    public class ObjectDraggerTouch : MonoBehaviour
    {
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

        // Update is called once per frame
        void Update()
        {
            if (Input.touchCount > 0)
            {
                // Getting the first touch point (prohibits multi-touch)
                Touch touch = Input.GetTouch(0);

                // Determine touch type and process accordingly
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        Ray raycast = Camera.main.ScreenPointToRay(touch.position);
                        if (Physics.Raycast(raycast, out RaycastHit raycastHit))
                        {
                            m_isCard = raycastHit.collider.CompareTag("Card");
                        }

                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        // Only allow dragging cards
                        if (m_isCard)
                        {
                            HandleDrag(touch);
                        }
                        
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        m_isCard = false;
                        break;
                }
            }
        }

        /**
         * Handle dragging the game object associated with this ObjectDraggerTouch
         * instance. Updates the respective transform with the current touch location.
         * 
         * @param Touch touch the current touch point.
         */
        private void HandleDrag(Touch touch)
        {
            m_screenPoint = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10));
            transform.position = new Vector3(m_screenPoint.x, m_screenPoint.y, transform.position.z);
        }
    }
}

