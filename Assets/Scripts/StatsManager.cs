using UnityEngine;

namespace Solitaire
{
    /**
     * Keep track of gameplay statistics relative to wins
     * 
     * Track the following with PlayerPrefs
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

        private enum FillStates
        {
            NO_FILL,
            FILL_NA_ON_ZERO
        };

        private const string TIME_HISTORY_KEY = "TimeHistory";
        private const string MOVES_HISTORY_KEY = "MovesHistory";
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

        private string m_timeHistory;
        private string m_movesHistory;
        private long m_fastestTimeMillis;
        private long m_longestTimeMillis;
        private long m_averageTimeMillis;
        private uint m_leastMoves;
        private uint m_mostMoves;
        private float m_averageMoves;
        private uint m_currentMoves;
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
            // Init current game vars
            m_currentMoves = 0;

            // Init saved stats vars
            Init();
        }

        /**
         * Initialize the saved statistics values
         */
        private void Init()
        {
            // Load everything from PlayerPrefs to the respective stat
            m_timeHistory = PlayerPrefs.GetString(TIME_HISTORY_KEY, "");
            m_movesHistory = PlayerPrefs.GetString(MOVES_HISTORY_KEY, "");
            m_fastestTimeMillis = long.Parse(PlayerPrefs.GetString(FASTEST_TIME_KEY, "0"));
            m_longestTimeMillis = long.Parse(PlayerPrefs.GetString(LONGEST_TIME_KEY, "0"));
            m_averageTimeMillis = long.Parse(PlayerPrefs.GetString(AVERAGE_TIME_KEY, "0"));
            m_leastMoves = uint.Parse(PlayerPrefs.GetString(LEAST_MOVES_KEY, "0"));
            m_mostMoves = uint.Parse(PlayerPrefs.GetString(MOST_MOVES_KEY, "0"));
            m_averageMoves = PlayerPrefs.GetFloat(AVERAGE_MOVES_KEY, 0.0f);
            m_totalMoves = ulong.Parse(PlayerPrefs.GetString(TOTAL_MOVES_KEY, m_currentMoves.ToString()));
            m_totalWins = ulong.Parse(PlayerPrefs.GetString(TOTAL_WINS_KEY, "0"));
            m_totalLoss = ulong.Parse(PlayerPrefs.GetString(TOTAL_LOSSES_KEY, "0"));
            m_winLossRatio = PlayerPrefs.GetFloat(WIN_LOSS_RATIO_KEY, 0.0f);
        }

        private void Update()
        {
            DisplayStatistic(fastestTimeStat, m_fastestTimeMillis.ToString(), true);
            DisplayStatistic(longestTimeStat, m_longestTimeMillis.ToString(), true);
            DisplayStatistic(averageTimeStat, m_averageTimeMillis.ToString(), true);
            DisplayStatistic(leastMovesStat, m_leastMoves.ToString(), false);
            DisplayStatistic(mostMovesStat, m_mostMoves.ToString(), false);
            DisplayStatistic(averageMovesStat, m_averageMoves.ToString(), false);
            DisplayStatistic(totalMovesStat, m_totalMoves.ToString(), false, FillStates.NO_FILL);
            DisplayStatistic(totalWinsStat, m_totalWins.ToString(), false, FillStates.NO_FILL);
            DisplayStatistic(totalLossesStat, m_totalLoss.ToString(), false, FillStates.NO_FILL);
            DisplayStatistic(winLossRatioStat, m_winLossRatio.ToString(), false);
        }

        /**
         * Display values to the statistics UI fields given a statistic
         * component target, a respective value to set, and whether the formatted
         * value should be interpreted as a timestamp or not.
         * 
         * @param statistic   the Statistic UI component to target in the Scene.
         * @param value       the value to set for the respective statistic UI component.
         * @param isTimestamp whether the value should be interpreted as a timestamp.
         */
        private void DisplayStatistic(Statistic statistic, string value, bool isTimestamp, FillStates fillState = FillStates.FILL_NA_ON_ZERO)
        {
            string formatted = value.Equals("0") && fillState.Equals(FillStates.FILL_NA_ON_ZERO) ? "N/A" : value;
            if (isTimestamp)
            {
                if (value.Equals("0") && fillState.Equals(FillStates.FILL_NA_ON_ZERO))
                    formatted = "N/A";
                else
                    formatted = Utils.GetTimestamp(float.Parse(value));
            }

            statistic.SetText(formatted);
        }

        /**
         * Increment the total number of moves. Does not save to PlayerPrefs.
         */
        public void TallyMove()
        {
            m_currentMoves++;
            m_totalMoves++;
        }

        /**
         * Invoked when the player wins the game. All statistics related
         * to winning the game will be saved to player prefs. 
         * 
         * Calculates and updates/saves the following:
         * fastest time
         * longest (slowest) time
         * average time
         * least moves
         * most moves
         * average moves
         * total moves
         * total wins
         * win-loss ratio
         */
        public void OnWin()
        {
            if (GameManager.DEBUG_MODE) Debug.Log("Game has been won. Saving statistics...");

            long currentTimeMillis = GameManager.Instance.GetCurrentTime();
            m_fastestTimeMillis = m_fastestTimeMillis > currentTimeMillis ? currentTimeMillis : m_fastestTimeMillis;
            m_longestTimeMillis = currentTimeMillis > m_longestTimeMillis ? currentTimeMillis : m_longestTimeMillis;

            // Compute the average time
            ComputeAverageTime(currentTimeMillis);

            // Determine least and most moves
            m_mostMoves = m_currentMoves > m_mostMoves ? m_currentMoves : m_mostMoves;
            m_leastMoves = m_leastMoves > m_currentMoves ? m_currentMoves : m_leastMoves;

            // Calculate the average moves
            ComputeAverageMoves();

            m_totalWins++;
            m_winLossRatio = m_totalLoss != 0 ? m_totalWins / m_totalLoss : m_totalWins;

            // Save all computed/tracked values to player prefs
            PlayerPrefs.SetString(TIME_HISTORY_KEY, m_timeHistory);
            PlayerPrefs.SetString(MOVES_HISTORY_KEY, m_movesHistory);
            PlayerPrefs.SetString(FASTEST_TIME_KEY, m_fastestTimeMillis.ToString());
            PlayerPrefs.SetString(LONGEST_TIME_KEY, m_longestTimeMillis.ToString());
            PlayerPrefs.SetString(AVERAGE_TIME_KEY, m_averageTimeMillis.ToString());
            PlayerPrefs.SetString(LEAST_MOVES_KEY, m_leastMoves.ToString());
            PlayerPrefs.SetString(MOST_MOVES_KEY, m_mostMoves.ToString());
            PlayerPrefs.SetFloat(AVERAGE_MOVES_KEY, m_averageMoves);
            PlayerPrefs.SetString(TOTAL_MOVES_KEY, m_totalMoves.ToString());
            PlayerPrefs.SetString(TOTAL_WINS_KEY, m_totalWins.ToString());
            PlayerPrefs.SetFloat(WIN_LOSS_RATIO_KEY, m_winLossRatio);

            PlayerPrefs.Save();
        }

        /**
         * Invoked when the player loses the game. All statistics related
         * to losing the game will be saved to player prefs.
         * 
         * Calculates and updates/saves the following:
         * total losses
         * win-loss ratio
         * total moves over time
         */
        public void OnLose()
        {
            if (GameManager.DEBUG_MODE) Debug.Log("Game has been lost. Saving statistics...");

            m_totalLoss++;
            m_winLossRatio = m_totalWins / m_totalLoss;

            PlayerPrefs.SetString(TOTAL_LOSSES_KEY, m_totalLoss.ToString());
            PlayerPrefs.SetFloat(WIN_LOSS_RATIO_KEY, m_winLossRatio);
            PlayerPrefs.SetString(TOTAL_MOVES_KEY, m_totalMoves.ToString());
            PlayerPrefs.SetString(TIME_HISTORY_KEY, m_timeHistory);
            PlayerPrefs.SetString(MOVES_HISTORY_KEY, m_movesHistory);

            PlayerPrefs.Save();
        }

        /**
         *  Function anticipated to be triggered by button event to
         *  reset the statistics. Clears the PlayerPrefs entries for
         *  each of the statistics.
         */
        public void ResetStatistics()
        {
            if (GameManager.DEBUG_MODE) Debug.Log("Resetting statistics...");
            PlayerPrefs.DeleteKey(TIME_HISTORY_KEY);
            PlayerPrefs.DeleteKey(MOVES_HISTORY_KEY);
            PlayerPrefs.DeleteKey(FASTEST_TIME_KEY);
            PlayerPrefs.DeleteKey(LONGEST_TIME_KEY);
            PlayerPrefs.DeleteKey(AVERAGE_TIME_KEY);
            PlayerPrefs.DeleteKey(LEAST_MOVES_KEY);
            PlayerPrefs.DeleteKey(MOST_MOVES_KEY);
            PlayerPrefs.DeleteKey(AVERAGE_MOVES_KEY);
            PlayerPrefs.DeleteKey(TOTAL_MOVES_KEY);
            PlayerPrefs.DeleteKey(TOTAL_WINS_KEY);
            PlayerPrefs.DeleteKey(TOTAL_LOSSES_KEY);
            PlayerPrefs.DeleteKey(WIN_LOSS_RATIO_KEY);

            // Re-init the saved stats vars
            Init();

            PlayerPrefs.Save();
        }

        /**
         * Compute the average time using the saved time history string in 
         * addition to the current game time provided by the game manager.
         */
        private void ComputeAverageTime(long currentTimeMillis)
        {
            // Add the current time to the time history and then tokenize to calculate the average time
            if (m_timeHistory == "")
            {
                m_averageTimeMillis = currentTimeMillis;
                m_timeHistory = currentTimeMillis.ToString();
            }
            else
            {
                m_timeHistory += "," + currentTimeMillis;
                string[] timeTokens = m_timeHistory.Split(',');
                long timeSum = 0;
                for (int i = 0; i < timeTokens.Length; i++)
                {
                    timeSum += long.Parse(timeTokens[i]);
                }

                // Do the average time calculation
                m_averageTimeMillis = timeSum / timeTokens.Length;
            }
        }

        /**
         * Compute the average moves by using the saved moves history string
         * along with the current amount of moves that were done in the current
         * game session.
         */
        private void ComputeAverageMoves()
        {
            if (m_movesHistory == "")
            {
                m_averageMoves = m_currentMoves;
                m_movesHistory = m_currentMoves.ToString();
            }
            else
            {
                m_movesHistory += "," + m_currentMoves;
                string[] movesTokens = m_movesHistory.Split(',');
                int movesSum = 0;
                for (int i = 0; i < movesTokens.Length; i++)
                {
                    movesSum += int.Parse(movesTokens[i]);
                }

                // Do the average moves calculation
                m_averageMoves = movesSum / movesTokens.Length;
            }
        }
    }
}

