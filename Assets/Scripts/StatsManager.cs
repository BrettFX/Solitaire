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

        private const string FASTEST_TIME_KEY = "FastestTime";
        private const string LONGEST_TIME_KEY = "LongestTime";
        private const string AVERAGE_TIME_KEY = "AverageTime";
        private const string LEAST_MOVES_KEY = "LeastMoves";
        private const string MOST_MOVES_KEY = "MostMoves";
        private const string AVERAGE_MOVES_KEY = "AverageMoves";
        private const string TOTAL_MOVES_KEY = "TotalMoves";
        private const string TOTAL_WINS_KEY = "TotalWins";
        private const string TOTAL_LOSSES_KEY = "TotalLosses";
        private const string WIN_LOSS_RATIO_KEY = "WinLossRatio";

        [Header("Statistics Containers")]
        public Statistic fastestTimeStat;
        public Statistic longestTimeStat;
        public Statistic averageTimeStat;
        public Statistic leastMovesStat;
        public Statistic mostMovesStat;
        public Statistic averageMovesStat;
        public Statistic totalMovesStat;
        public Statistic totalWinsStat;
        public Statistic totalLossesStat;
        public Statistic winLossRatioStat;

        private float m_fastestTimeMillis;
        private float m_longestTimeMillis;
        private float m_averageTimeMillis;
        private uint m_leastMoves;
        private uint m_mostMoves;
        private float m_averageMoves;
        private ulong m_totalMoves;
        private ulong m_totalWins;
        private ulong m_totalLoss;
        private float m_winLossRatio;

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
            // Load everything from PlayerPrefs to the respective stat
            m_fastestTimeMillis = PlayerPrefs.GetFloat(FASTEST_TIME_KEY, 0.0f);
            DisplayStatistic(fastestTimeStat, m_fastestTimeMillis.ToString(), true);

            m_longestTimeMillis = PlayerPrefs.GetFloat(LONGEST_TIME_KEY, 0.0f);
            DisplayStatistic(longestTimeStat, m_longestTimeMillis.ToString(), true);

            m_averageTimeMillis = PlayerPrefs.GetFloat(AVERAGE_TIME_KEY, 0.0f);
            DisplayStatistic(averageTimeStat, m_averageTimeMillis.ToString(), true);

            m_leastMoves = (uint)PlayerPrefs.GetInt(LEAST_MOVES_KEY, 0);
            DisplayStatistic(leastMovesStat, m_leastMoves.ToString(), false);

            m_mostMoves = (uint)PlayerPrefs.GetInt(MOST_MOVES_KEY, 0);
            DisplayStatistic(mostMovesStat, m_mostMoves.ToString(), false);

            m_averageMoves = PlayerPrefs.GetFloat(AVERAGE_MOVES_KEY, 0.0f);
            DisplayStatistic(averageMovesStat, m_averageMoves.ToString(), false);

            m_totalMoves = ulong.Parse(PlayerPrefs.GetString(TOTAL_MOVES_KEY, "0"));
            DisplayStatistic(totalMovesStat, m_totalMoves.ToString(), false);

            m_totalWins = ulong.Parse(PlayerPrefs.GetString(TOTAL_WINS_KEY, "0"));
            DisplayStatistic(totalWinsStat, m_totalWins.ToString(), false);

            m_totalLoss = ulong.Parse(PlayerPrefs.GetString(TOTAL_LOSSES_KEY, "0"));
            DisplayStatistic(totalLossesStat, m_totalLoss.ToString(), false);

            m_winLossRatio = PlayerPrefs.GetFloat(WIN_LOSS_RATIO_KEY, 0.0f);
            DisplayStatistic(winLossRatioStat, m_winLossRatio.ToString(), false);

        }

        /**
         * 
         */
        private void DisplayStatistic(Statistic statistic, string value, bool isTimestamp)
        {
            string formatted = isTimestamp ? Utils.GetTimestamp(float.Parse(value)) : string.Format(value);
            statistic.SetText(formatted);
        }

        /**
         * Invoked when the player wins the game. All statistics related
         * to winning the game will be saved to player prefs.
         */
        public void OnWin()
        {
            Debug.Log("Game has been won. Saving statistics (not yet implemented)...");
            // TODO calculate all winning related stats here
        }

        /**
         * Invoked when the player loses the game. All statistics related
         * to losing the game will be saved to player prefs.
         */
        public void OnLose()
        {
            Debug.Log("Game has been lost. Saving statistics (not yet implemented)...");
            // TODO calculate all losing related stats here
            m_totalLoss++;

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

