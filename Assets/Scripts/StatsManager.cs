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

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

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

