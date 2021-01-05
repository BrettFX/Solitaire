using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Solitaire
{
    public class OrientationManager : MonoBehaviour
    {
        public static OrientationManager Instance { get; private set; }

        // Threshold time in milliseconds allowed between screen orientation change events
        private const float ORIENTATION_CHANGE_TIME_THRESHOLD = 250.0f;

        public enum Orientations
        {
            PORTRAIT,
            LANDSCAPE,
            UNKNOWN
        };

        [Header("References")]
        public CanvasScaler scaler;

        [Header("Resolution Configuration")]
        public Vector2 portraitRes;
        public Vector2 landscapeRes;

        private static Orientations m_currentOrientation = Orientations.UNKNOWN;

        /**
         * IMPORTANT: Attach this script to an empty game object within a Canvas and
         * ensure that the RectTransform is configured to stretch about the
         * x and y axes. Set the Left, Top, Pos Z, Right, and Bottom should all
         * to 0 so that the RectTransform stretches to fit the screen.
         */
        private RectTransform m_rectTransform; // Determines the current screen res

        private System.Diagnostics.Stopwatch m_stopwatch;

        private bool m_hasLoaded = false;

        private void Awake()
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
            Instance = GetComponent<OrientationManager>();
        }

        private void Start()
        {
            m_hasLoaded = true;
            Init();
            m_stopwatch.Start();
            HandleOrientationChange(true);
        }

        /**
         * Initialize members only if they are null.
         */
        private void Init()
        {
            // Initialize the stop watch if it hasn't been initalized yet
            if (m_stopwatch == null)
            {
                m_stopwatch = new System.Diagnostics.Stopwatch();

                // Stop and reset to prevent from missing first frame
                m_stopwatch.Reset();
            }

            // Initialize the rect transform if it hasn't been initialized yet
            if (m_rectTransform == null)
            {
                m_rectTransform = GetComponent<RectTransform>();
                if (GameManager.DEBUG_MODE) Debug.Log("Initial screen dimensions are: " + m_rectTransform.rect);
            }
        }

        public static void SetCurrentOrientation(Orientations orientation)
        {
            m_currentOrientation = orientation;
        }

        public static Orientations GetCurrentOrientation()
        {
            return m_currentOrientation;
        }

        public static bool IsPortraitOrientation()
        {
            return m_currentOrientation.Equals(Orientations.PORTRAIT);
        }

        public static bool IsLandscapeOrientation()
        {
            return m_currentOrientation.Equals(Orientations.LANDSCAPE);
        }

        /**
         * Compute the scale for the cards and snaps relative to the given screen orientation's
         * resolution. This ensures that the width and height of the cards and snaps maintain the
         * same dimensions on devices of varying screen sizes.
         * 
         * NOTE: This will only compute the correct relative x and y values if the rect transform 
         * associated with this orientation manager changes with respect to the given orientation.
         * In other words, this will not work if this function is tested via a simulated unit test
         * in which the screen resolution doesn't actually change.
         * 
         * @param Orientations orientation the screen orientation used to determine the relative 
         *                     x and y values for the cards and snaps.
         *                     
         * @return Vector3 the computed scale of the cards and snaps.
         */
        public Vector3 GetCardAndPortraitScaleByOrientation(Orientations orientation)
        {
            float relX;
            float relY;

            m_rectTransform.ForceUpdateRectTransforms();

            float w = m_rectTransform.rect.width;
            float h = m_rectTransform.rect.height;

            if (GameManager.DEBUG_MODE)
            {
                Debug.Log("Current rect transform width: " + w);
                Debug.Log("Current rect transform height: " + h);

                Debug.Log("Processing orientation: " + orientation);
            }

            // Compute values based on a percentage relative to the current screen dimensions
            switch (orientation)
            {
                case Orientations.PORTRAIT:
                    relX = w * (55.0f / portraitRes.x);    // Should yield value relative to 55.0f
                    relY = h * (100.0f / portraitRes.y);   // Should yield value relative to 100.0f
                    break;

                case Orientations.LANDSCAPE:
                    relX = w * (101.25f / landscapeRes.x); // Should yield value relative to 101.25f
                    relY = h * (146.25f / landscapeRes.y); // Should yield value relative to 146.25f
                    break;

                default:
                    relX = 0.0f;
                    relY = 0.0f;
                    break;
            }

            return new Vector3(relX, relY, 1.0f);
        }

        /**
         * Detect the new screen orientation and toggle the visibility of the 
         * associated portrait or landscape game objects based on the new 
         * screen orientation. Also, configure the CanvasScaler to conform to the
         * new screen resolution.
         */
        private void HandleOrientationChange(bool overrideSpamGuard=false)
        {
            // Return if this instance hasn't fully loaded yet
            if (!m_hasLoaded) return;


            if (m_stopwatch != null && m_stopwatch.IsRunning)
            {
                // Add spam guard with stopwatch (only allow orientation change events every n second(s))
                // Enable ability to override spam guard
                if (!overrideSpamGuard) 
                    if (m_stopwatch.ElapsedMilliseconds < ORIENTATION_CHANGE_TIME_THRESHOLD) return;
            }

            m_stopwatch.Reset();
            m_stopwatch.Start();

            if (m_rectTransform == null) return;

            m_rectTransform.ForceUpdateRectTransforms();

            if (GameManager.DEBUG_MODE) Debug.Log("Current screen dimensions are: " + m_rectTransform.rect);

            bool verticalOrientation = m_rectTransform.rect.width < m_rectTransform.rect.height;
            m_currentOrientation = verticalOrientation ? Orientations.PORTRAIT : Orientations.LANDSCAPE;

            float newX = verticalOrientation ? portraitRes.x : landscapeRes.x;
            float newY = verticalOrientation ? portraitRes.y : landscapeRes.y;
            Vector2 newRes = new Vector2(newX, newY);

            // Set the canvas scaler reference resolution based on new orientation
            scaler.referenceResolution = newRes;

            // Notify game manager of orientation change
            // Game manager is only null when this function is invoked from the OnRectTransformDimensionsChange function when the app launches.
            try
            {
                GameManager.Instance.RescaleGameObjectsByOrientation(m_currentOrientation);
                GameManager.Instance.RepositionGameObjectsByOrientation(m_currentOrientation);
                GameManager.Instance.SetTargetCanvasObjectsByOrientation(m_currentOrientation);
            }
            catch (Exception e)
            {
                if (GameManager.DEBUG_MODE)
                    Debug.Log("Broadcasing to GameManager to process rescaling and repositioning actions later due to " + e.GetType());

                // Need to broadcast that rescaling and reposition is needed once the game manager instance is not null
                GameManager.ProcessLater(() =>
                {
                    GameManager.Instance.RescaleGameObjectsByOrientation(m_currentOrientation);
                    GameManager.Instance.RepositionGameObjectsByOrientation(m_currentOrientation);
                    GameManager.Instance.SetTargetCanvasObjectsByOrientation(m_currentOrientation);
                });
            }
        }

        private void OnApplicationQuit()
        {
            // Need to set loaded flag to false so that the rescaling and repositioning functions are not triggered
            m_hasLoaded = false;
        }

        /**
         * Event triggered by UnityEngine.EventSystems (UIBehaviour) on mobile
         * devices when the screen orientation changes.
         * 
         * Implementation invokes a custom handle function for when the orientation
         * changes to determine the new orientation and process accordingly.
         */
        private void OnRectTransformDimensionsChange()
        {
            Init(); // Only initializes members if they are null (safe to invoke multiple times)
            HandleOrientationChange(m_hasLoaded);
        }

    }
}

