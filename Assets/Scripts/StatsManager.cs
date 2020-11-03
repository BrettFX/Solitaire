using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Solitaire
{
    /**
     * Keep track of gameplay statistics relative to wins
     * 
     * TODO Track the following with PlayerPrefs
     * - Fastest time played
     * - Longest time played
     * - Average time played
     * - Least amount of moves
     * - Most amount of moves
     * - Average moves
     * - Total moves over time
     * - Total wins
     * - Total losses
     * - Win-Loss ratio
     */
    public class StatsManager : MonoBehaviour
    {
        public static StatsManager Instance { get; private set; }

        [Header("Statistics Containers")]
        public GameObject fastestTimeStat;
        public GameObject longestTimeStat;
        public GameObject averageTimeStat;
        public GameObject leastMovesStat;
        public GameObject mostMovesStat;
        public GameObject averageMovesStat;
        public GameObject totalMovesStat;
        public GameObject totalWinsStat;
        public GameObject totalLossesStat;
        public GameObject winLossRatioStat;

        private float m_fastestTimeMillis;
        private float m_longestTimeMillis;
        private float m_averageTimeMillis;
        private uint m_leastMoves;
        private uint m_mostMoves;
        private float m_averageMoves;
        private ulong m_totalMoves;
        private ulong m_totalWins;
        private ulong m_totalLoss;
        private float winLossRatio;

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
            Instance = GetComponent<StatsManager>();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        /**
         * Invoked when the player wins the game. All statistics related
         * to winning the game will be saved to player prefs.
         */
        public void OnWin()
        {
            Debug.Log("Game has been won. Saving statistics (not yet implemented)...");
        }

        /**
         * Invoked when the player loses the game. All statistics related
         * to losing the game will be saved to player prefs.
         */
        public void OnLose()
        {
            Debug.Log("Game has been lost. Saving statistics (not yet implemented)...");
        }

        /**
         *  Function anticipated to be triggered by button event to
         *  reset the statistics. Clears the PlayerPrefs entries for
         *  each of the statistics.
         */
        public void ResetStatistics()
        {
            Debug.Log("Resetting statistics (not yet implemented)...");
        }
    }
}

