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

        // Start is called before the first frame update
        void Start()
        {

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
                        HandleTouchDown(touch);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        HandleTouchDrag(touch);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchUp();
                        break;
                }
            }
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

            //m_screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            //Vector3 curScreenPoint = new Vector3(touch.position.x, touch.position.y, m_screenPoint.z);
            //m_offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(curScreenPoint);

            //// Keep track of the starting position for future validation
            //m_startPos = Camera.main.ScreenToWorldPoint(curScreenPoint) + m_offset;

            //Vector3 collisionVector = new Vector3(10.0f, 10.0f, 1000.0f);
            //bool collides = Physics.CheckBox(m_startPos, collisionVector);
            //if (collides)
            //{
            //    m_isCard = gameObject.CompareTag("Card");

            //}

            // Register this object dragger instance
            //GameManager.Instance.RegisterObjectDragger(this);
        }

        /**
         * Handle dragging the game object associated with this ObjectDraggerTouch
         * instance. Updates the respective transform with the current touch location.
         * 
         * @param Touch touch the current touch point.
         */
        private void HandleTouchDrag(Touch touch)
        {
            // Only allow dragging cards
            if (m_isCard)
            {
                m_screenPoint = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10));
                if (m_currentObject != null)
                {
                    Transform t = m_currentObject.transform;
                    t.position = new Vector3(m_screenPoint.x, m_screenPoint.y, t.position.z);
                }
            }
        }

        /**
         * 
         */
        private void HandleTouchUp()
        {
            m_isCard = false;
            m_currentObject = null;
        }
    }
}


