﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Solitaire.Card;
using static Solitaire.Move;

namespace Solitaire
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static bool DEBUG_MODE = false;
        public static bool ANIMATIONS_ENABLED = true;

        public const int HOME_SCENE = 0;

        // Control the total amount of moves that can be stored in memory (prevents stack overflow)
        private const int MAX_MOVES_STACK_SIZE = 1000;

        // Control the speed that cards are moved from one point to the next (lower = faster where 0.0 is instantaneous)
        public const float CARD_TRANSLATION_SPEED_NORMAL = 0.25f;
        public const float CARD_TRANSLATION_SPEED_FAST = 0.05f;

        public const float Z_OFFSET_DRAGGING = 70.0f;
        public const float FOUNDATION_Y_OFFSET = 37.5f;
        public const float FACE_DOWN_Y_OFFSET = FOUNDATION_Y_OFFSET / 3; // Face down cards will have a smaller y-offset

        public static readonly string[] VALUE_REF =
        {
            "0", "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "JACK", "QUEEN", "KING"
        };

        public static readonly HashSet<string> PROHIBITED_DROP_LOCATIONS = new HashSet<string>
        {
            "Stock", "Talon"
        };

        public enum Sections
        {
            TABLEAU,
            STOCK,
            TALON,
            FOUNDATIONS
        };

        public enum GameStates
        {
            PLAYING,
            PAUSED,
            WON_PLAYING,
            WON_PAUSED
        };

        [Header("Set Up")]
        public GameObject tableau;
        public GameObject stock;
        public GameObject talon;
        public GameObject foundations;

        [Header("Utils")]
        public GameObject cardPrefab;
        public TextMeshProUGUI lblTimer;
        public GameObject resetModalOverlay;
        public GameObject settingsModalOverlay;

        [Header("Action Buttons")]
        public Button btnUndo;
        public Button btnRedo;
        public GameObject btnAutoWin;
        public Button btnConfirmReset;
        public Button btnSettings;

        private Sprite[] m_cardSprites;
        private Dictionary<CardSuit, Sprite[]> m_cardSpritesMap;

        private Transform m_stockPile;
        private Transform m_talonPile;

        private SnapManager[] m_foundationSnapManagers;
        private SnapManager[] m_tableauSnapManagers;

        // Used to update displayed time (maintains an accurate time accumulation)
        private System.Diagnostics.Stopwatch m_stopWatch;

        private CustomStack<Move> m_moves;        // Keep track of moves to allow for undoing
        private CustomStack<Move> m_undoneMoves;  // Keep track of moves that have been undone for redo capability

        private volatile bool m_blocked = false;
        private volatile bool m_doingAutoWin = false;
        private bool m_autoWinComplete = false;
        private bool m_enteredWinnableState = false;
        private bool m_playingResetBtnPulse = false;

        private bool m_firstTimeFocused = true;
        private bool m_demoMode = false;

        private GameStates m_currentGameState = GameStates.PLAYING;

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
                    Debug.LogWarning("There are multiple instances of the Game Manager script. Removing the old manager from the scene.");
                    Destroy(Instance.gameObject);
                }

                // Remove the old manager.
                Instance = null;
            }

            // Assign the instance variable as the Game Manager script on this object.
            Instance = GetComponent<GameManager>();
        }

        // Start is called before the first frame update
        void Start()
        {
            m_stockPile = stock.GetComponentInChildren<SnapManager>().transform;
            m_talonPile = talon.GetComponentInChildren<SnapManager>().transform;
            m_foundationSnapManagers = foundations.GetComponentsInChildren<SnapManager>();
            m_tableauSnapManagers = tableau.GetComponentsInChildren<SnapManager>();
            m_moves = new CustomStack<Move>();
            m_undoneMoves = new CustomStack<Move>();
            m_firstTimeFocused = true;

            // Only load card sprites and spawn stack if not using the demo scene (for unit testing support)
            m_demoMode = SceneManager.GetActiveScene().name.Contains("Demo");
            if (!m_demoMode)
            {
                LoadCardSprites();
                SpawnStack();
            }
            else
            {
                Debug.Log("Using demo scene.");
                Debug.Log("Steps for loading card sprites and spawning stack have been skipped.");
            }

            m_stopWatch = new System.Diagnostics.Stopwatch();
            m_stopWatch.Start();
        }

        private void Update()
        {
            // Only process if the game has not been won
            if (!HasWon())
            {
                if (!IsWinningState())
                {
                    // Turn off the button pulse animation for the reset button if it is playing
                    if (m_playingResetBtnPulse || IsPaused())
                    {
                        Animator animator = btnConfirmReset.GetComponent<Animator>();
                        animator.SetBool("WinningState", false);
                        m_playingResetBtnPulse = false;
                    }

                    // Check if the game is in a winnable state.
                    // Set the auto-win button to be active accordingly
                    if (!m_doingAutoWin)
                    {
                        if (IsWinnableState())
                        {
                            // Need various checks to make sure the auto-win button doesn't inadvertently reappear 
                            if (!btnAutoWin.activeInHierarchy && !m_blocked && !m_autoWinComplete)
                                btnAutoWin.SetActive(true);
                        }
                        else
                        {
                            if (btnAutoWin.activeInHierarchy) btnAutoWin.SetActive(false);
                        }
                    }

                    // Pause the timer if the game is paused
                    if (!IsPaused())
                    {
                        UpdateTimer();
                    }
                }
                else
                {
                    // Clear all moves from the moves lists once the game has been won
                    m_moves.Clear();
                    m_undoneMoves.Clear();

                    if (btnAutoWin.activeInHierarchy) btnAutoWin.SetActive(false);

                    // Play winning sound if it hasn't already been played
                    AudioSource winSound = SettingsManager.Instance.winSound;
                    if (!winSound.isPlaying && !IsPaused())
                        winSound.Play();

                    // Set the game state to win and invoke the stats manager win function
                    SetGameState(GameStates.WON_PLAYING);
                    StatsManager.Instance.OnWin();

                    // Show the win settings page (to start new game) and pause the game
                    SetPaused(true);
                    SettingsManager.Instance.winSettingsPage.SetActive(true);

                    // Set the new fastest time label visibility if a new fastest time was done
                    bool newFastestTime = StatsManager.Instance.IsNewFastestTime();
                    SettingsManager.Instance.lblHighScoreNotification.SetActive(newFastestTime);
                }
            }

            // Only toggle button pulse animations for the reset button if in won state
            if (HasWon())
            {
                // Trigger the button pulse animation for the reset button if it isn't already playing
                if (!m_playingResetBtnPulse && !IsPaused())
                {
                    Animator animator = btnConfirmReset.GetComponent<Animator>();
                    animator.SetBool("WinningState", true);
                    m_playingResetBtnPulse = true;
                }
                else if (m_playingResetBtnPulse && IsPaused())
                {
                    Animator animator = btnConfirmReset.GetComponent<Animator>();
                    animator.SetBool("WinningState", false);
                    m_playingResetBtnPulse = false;
                }
            }

            if (m_moves.Count >= MAX_MOVES_STACK_SIZE)
            {
                // Remove the oldest move
                m_moves.RemoveOldest();
            }

            if (m_undoneMoves.Count >= MAX_MOVES_STACK_SIZE)
            {
                // Remove the oldest undone move
                m_undoneMoves.RemoveOldest();
            }

            // Toggle interactability on undo and redo buttons based on size of respective moves list
            btnUndo.interactable = m_moves.Count > 0 && !m_doingAutoWin && !IsPaused() && !HasWon();
            btnRedo.interactable = m_undoneMoves.Count > 0 && !m_doingAutoWin && !IsPaused() && !HasWon();

            // Toggle interactability of reset button based on auto win state
            btnConfirmReset.interactable = !m_doingAutoWin && !IsPaused();
            btnSettings.interactable = !m_doingAutoWin && !IsPaused();
        }

        public float GetCardTranslationSpeed()
        {
            return IsDoingAutoWin() ? CARD_TRANSLATION_SPEED_FAST : CARD_TRANSLATION_SPEED_NORMAL;
        }

        public void SetGameState(GameStates gameState)
        {
            m_currentGameState = gameState;
        }

        public GameStates GetGameState()
        {
            return m_currentGameState;
        }

        /**
         * Detect when the application loses and gains focus to control
         * the pause state of the game.
         * 
         * @param bool hasFocus whether the game has focus or not.
         */
        void OnApplicationFocus(bool hasFocus)
        {
            // Stop the stop watch when paused so that the displayed time stops.
            // Start the stop watch if not paused
            if (m_stopWatch != null)
            {
                if (!hasFocus) m_stopWatch.Stop(); else m_stopWatch.Start();
            }

            // Re-load settings in case an audio source lost scope
            if (hasFocus && !m_firstTimeFocused)
            {
                SettingsManager.Instance.LoadSettings();
            }

            m_firstTimeFocused = false;
        }

        /**
         * Get the total count of cards for each pile in the foundation and 
         * sum them together.
         * 
         * @return the sum of the total amount of cards in the foundations.
         */
        private int GetFoundationSum()
        {
            int cardCountSum = 0;
            foreach (SnapManager snapManager in m_foundationSnapManagers)
            {
                cardCountSum += snapManager.GetCardCount();
            }

            return cardCountSum;
        }

        public bool IsWinningState()
        {
            // Game is won if the sum of all cards in the foundations is 52
            return GetFoundationSum() == 52;
        }

        /**
         * Determine if the game is in a winnable state.
         * Game is in a winnable state when the following conditions have been met:
         * 1) Total count of all face down cards in Tableau is equal to 0.
         * 2) Total count of cards in talon, tableau, and foundation is equal to 52.
         */
        private bool IsWinnableState()
        {
            int totalFaceDownTableauCards = 0;
            int cardsOfInterestCount = 0;
            
            foreach (SnapManager tableauSnapManager in tableau.GetComponentsInChildren<SnapManager>())
            {
                totalFaceDownTableauCards += tableauSnapManager.GetFaceDownCardCount();
                cardsOfInterestCount += tableauSnapManager.GetCardCount();
            }

            foreach (SnapManager foundationSnapManager in foundations.GetComponentsInChildren<SnapManager>())
            {
                cardsOfInterestCount += foundationSnapManager.GetCardCount();
            }

            // Add the total count of cards in talon to the cards of interest count as well (VOLATILE BUG)
            // TODO only way to make this work would be to implement logic for checking that there isn't a depending card
            //      somewhere in the talon that would cause a failed auto-win process.
            //cardsOfInterestCount += m_talonPile.GetComponent<SnapManager>().GetCardCount();

            // Keep the auto-win button active by allowing a threshold of 13 cards to be dragged at any point
            // after the initial winnable state was triggered.
            bool winnableState;
            if (m_enteredWinnableState)
            {
                winnableState = totalFaceDownTableauCards == 0 &&
                                cardsOfInterestCount >= 52 - 13 &&
                                stock.GetComponentInChildren<SnapManager>().GetCardCount() == 0 &&
                                m_talonPile.GetComponent<SnapManager>().GetCardCount() == 0;

                if (!winnableState)
                    m_enteredWinnableState = false;
            }
            else
            {
                m_enteredWinnableState = totalFaceDownTableauCards == 0 && cardsOfInterestCount == 52;
                winnableState = m_enteredWinnableState;
            }

            return winnableState;
        }

        public void SetDoingAutoWin(bool doAutoWin)
        {
            m_doingAutoWin = doAutoWin;
        }

        public bool IsDoingAutoWin()
        {
            return m_doingAutoWin;
        }

        public void OpenSettings()
        {
            // Only allow action if there is currently only one finger touching the screen
            if (TouchManager.Instance.GetTouchCount() == 1)
            {
                if (!HasWon())
                {
                    Animator spinAnimator = btnSettings.GetComponent<Animator>();
                    spinAnimator.SetTrigger("DoSpin");

                    SettingsManager.Instance.gearSound.Play();

                    SetPaused(true);
                    m_stopWatch.Stop();
                }
                else
                {
                    SettingsManager.Instance.winSettingsPage.SetActive(false);
                }

                // Display the modal overlay for settings
                settingsModalOverlay.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            if (!HasWon())
            {
                SetPaused(false);
                m_stopWatch.Start();
            }

            else
            {
                SettingsManager.Instance.winSettingsPage.SetActive(true);
            }

            // Close the modal overlay for settings
            settingsModalOverlay.SetActive(false);
        }

        /**
         * 
         */
        public void AutoWin()
        {
            // Only allow action if there is currently only one finger touching the screen
            if (TouchManager.Instance.GetTouchCount() == 1)
            {
                // Handle base case when auto win button is clicked when not in a winnable state
                if (!IsWinnableState())
                    return;

                btnAutoWin.SetActive(false);            // Hide the auto-win button while processing
                StartCoroutine(AutoWinCoroutine());     // Win the game
            }
        }

        /**
        * 
        **/
        private void UpdateTimer()
        {
            lblTimer.text = Utils.GetTimestamp(m_stopWatch.ElapsedMilliseconds);
        }

        /**
         * Process undo and redo move actions
         */
        private void ProcessMoveAction(MoveTypes moveType)
        {
            bool undoAction = moveType.Equals(MoveTypes.UNDO);
            CustomStack<Move> targetMoves = undoAction ? m_moves : m_undoneMoves;
            CustomStack<Move> altMoves = undoAction ? m_undoneMoves : m_moves;

            // Pop the last move from the moves list/stack
            Move move = targetMoves.Pop();

            if (move.IsSpecial())
            {
                // Special moves should only have one event
                Event ev = move.GetEvents()[0];
                ev.Reverse();

                // Swap the event type for proper redo
                Event.EventType evType = ev.GetEventType();
                Event.EventType newEvType = Event.EventType.NONE;
                switch (evType)
                {
                    case Event.EventType.REPLINISH:
                        newEvType = Event.EventType.DEPLINISH;
                        break;
                    case Event.EventType.DEPLINISH:
                        newEvType = Event.EventType.REPLINISH;
                        break;
                }

                ev.SetType(newEvType);
                m_blocked = false;
            }
            else
            {
                // Take precedence over events in the move (execute them first)
                List<Event> events = move.GetEvents();
                foreach (Event evt in events)
                {
                    // Reverse the event
                    evt.Reverse();
                }

                // Perform the move; don't want to track changes so that undone moves are managed through here
                move.GetTopCard().MoveTo(undoAction ? move.GetPreviousParent() : move.GetNextParent(), move.GetCards(), moveType);
            }

            // Add the move to the redo stack
            altMoves.Push(move);
        }

        /**
         * Determines wether it is safe to process move actions for cards.
         * Not safe when there exists any cards that are presently in transit
         * to a new position and/or are performing any other animations (e.g., flipping).
         * 
         * @return bool    wether it's safe to move cards or not.
         */
        public bool IsSafeToMoveCards()
        {
            // Get all card instances in scene
            Card[] cards = FindObjectsOfType<Card>();
            bool safe = true;
            foreach (Card card in cards)
            {
                if (card.IsTranslating() || card.IsFlipping())
                {
                    safe = false;
                    break;
                }
            }

            return safe;
        }

        /**
         * Undo the last move that was done.
         */
        public void Undo()
        {
            // Only allow action if there is currently only one finger touching the screen
            if (TouchManager.Instance.GetTouchCount() == 1)
            {
                // Don't proceed if already blocked
                if (!m_blocked && IsSafeToMoveCards())
                {
                    // Block additional actions and events until undo is complete.
                    m_blocked = true;
                    ProcessMoveAction(MoveTypes.UNDO);
                }
            }
        }

        /**
         * Redo the last undone move. Moves can only be redone as long as there
         * are still undone moves to redo and if a new move is not added to the
         * list of moves to undo.
         */
        public void Redo()
        {
            // Only allow action if there is currently only one finger touching the screen
            if (TouchManager.Instance.GetTouchCount() == 1)
            {
                // Don't proceed if already blocked
                if (!m_blocked && IsSafeToMoveCards())
                {
                    // Block additional actions and events until undo is complete.
                    m_blocked = true;
                    ProcessMoveAction(MoveTypes.REDO);
                }
            }
        }

        /**
         * Toggle the visability of the reset modal overlay to get confirmation
         * from the user on whether to reset the game.
         */
        public void ToggleResetModal()
        {
            // Only allow action if there is currently only one finger touching the screen
            if (TouchManager.Instance.GetTouchCount() == 1)
            {
                SetPaused(!IsPaused());

                // Stop the stop watch when paused so that the displayed time stops.
                // Start the stop watch if not paused
                if (IsPaused())
                {

                    m_stopWatch.Stop();
                }
                else
                {
                    m_stopWatch.Start();
                }

                // Display the modal overlay prompt to confirm Reset
                resetModalOverlay.SetActive(!resetModalOverlay.activeInHierarchy);

                // Only play the click sound if the reset window is open
                if (resetModalOverlay.activeInHierarchy)
                    SettingsManager.Instance.clickSound.Play();
            }
        }

        /**
         * 
         */
        public void Reset()
        {
            // Invoke the respective stats manager lose function only if not in won state
            if (!HasWon())
                StatsManager.Instance.OnLose();

            // Reset the stop watch time
            m_stopWatch.Reset();

            // Load the active scene to support interchanging between play and demo scenes
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /**
         * 
         */
        public void AddMove(Move move, MoveTypes moveType)
        {
            switch (moveType)
            {
                case MoveTypes.NORMAL:
                    m_moves.Push(move);

                    // Clear the redo stack if there are moves in it.
                    m_undoneMoves.Clear();
                    break;
                case MoveTypes.REDO:
                    m_moves.Push(move);
                    break;
                case MoveTypes.UNDO:
                    m_undoneMoves.Push(move);
                    break;
            }
        }

        /**
         * Dispatch events to the main set of moves, specifically the most recent move added to the list of
         * moves.
         */
        public void AddEventToLastMove(Event e)
        {
            // Add the event to the global list of events (assuming the first move in the list of moves is the target)
            if (m_moves.Count > 0)
            {
                m_moves.Peek().AddEvent(e);
            }
        }

        /**
         * Set blocking flag on actions and events to prevent action/event spamming.
         */
        public void SetBlocked(bool blocked)
        {
            m_blocked = blocked;
        }

        /**
         * Whether or not actions and events are actively being blocked.
         */
        public bool IsBlocked()
        {
            return m_blocked;
        }

        /**
         * Whether or not the game is paused.
         */
        public bool IsPaused()
        {
            return m_currentGameState.Equals(GameStates.PAUSED) ||
                   m_currentGameState.Equals(GameStates.WON_PAUSED);
        }

        /**
         * Wether or not the game has been won.
         */
        public bool HasWon()
        {
            return m_currentGameState.Equals(GameStates.WON_PLAYING) ||
                   m_currentGameState.Equals(GameStates.WON_PAUSED);
        }

        /**
         * Control the game state based on if the game should be 
         * paused or not.
         * 
         * @param bool paused    the paused flag to determine the game state.
         */
        public void SetPaused(bool paused)
        {
            if (HasWon())
            {
                SetGameState(paused ? GameStates.WON_PAUSED : GameStates.WON_PLAYING);
            }
            else
            {
                SetGameState(paused ? GameStates.PAUSED : GameStates.PLAYING);
            }
        }

        public Transform GetTalonPile()
        {
            return m_talonPile;
        }

        public Transform GetStockPile()
        {
            return m_stockPile;
        }

        /**
         * Get the next avaiable/valid move for the card in question.
         * Scan through the placeable card locations in the Talons
         * and Foundations to determine if there is a valid snap location
         * to move the specified card.
         * 
         * The intended use cases for this function is when a user double clicks
         * a card to auto-complete a move or if the user desires a hint.
         * 
         * @param Card card the card to use to compare to any available drop
         *                  location in the Talons or Foundations.
         *                  
         * @param int cardCount the number of cards that are being dragged 
         *                      relative to the card in question. Effects
         *                      the validation process (e.g., can't move more
         *                      than one card to the Foundations pile with a single
         *                      drag)
         *                  
         * @return the Transform of the snap that represents the next
         *         available move for the card in question. Returns null if
         *         there are no available moves.
         */
        public Transform GetNextAvailableMove(Card card, int cardCount = 1)
        {
            Transform nextMove = null;

            // Only process next available move if the card isn't translating or flipping
            if (card != null && !card.IsTranslating() && !card.IsFlipping())
            {
                Transform cardCurrentParent = card.GetCurrentParent();

                // Handle face-down card corner case (only process if the card is face up)
                if (!card.IsFaceDown())
                {
                    // First priority is the Foundations since the primary objective of the game is
                    // to get all cards to the Foundations.
                    // Only check valid foundation location if the card count is 1
                    if (cardCount == 1)
                    {
                        //Debug.Log("Checking if " + card.ToString() + " is valid to move to foundation...");
                        foreach (SnapManager snapManager in m_foundationSnapManagers)
                        {
                            if (snapManager.IsValidMove(card))
                            {
                                // Skip if the next move is the current card location
                                if (!snapManager.transform.Equals(cardCurrentParent))
                                {
                                    nextMove = snapManager.transform;
                                    break;
                                }
                            }
                        }
                    }

                    // Second priority is the tableau. Don't try to find a move if one has already been found
                    // in the foundations
                    if (!nextMove)
                    {
                        List<Transform> possibleMoves = new List<Transform>();

                        // Do first pass through tableau snap managers to get set of possible moves
                        foreach (SnapManager snapManager in m_tableauSnapManagers)
                        {
                            if (snapManager.IsValidMove(card))
                            {
                                // Skip if the next move is the current card location
                                if (!snapManager.transform.Equals(cardCurrentParent))
                                {
                                    possibleMoves.Add(snapManager.transform);
                                }
                            }
                        }

                        // Only proceed if there is at least one possible move
                        if (possibleMoves.Count > 0)
                        {
                            // Prioritize the move that is closest to the current card
                            int closestIndex = 0;

                            // Use square magnitude to calculate least distance between relative card
                            // and possible moves
                            float sqrMagnitude = Vector3.SqrMagnitude(card.transform.position - possibleMoves[0].position);
                            float minDistance = sqrMagnitude;
                            for (int i = 0; i < possibleMoves.Count; i++)
                            {
                                // Determine if there is a closer move to the card in question (after the first calculation).
                                if (i != 0)
                                {
                                    sqrMagnitude = Vector3.SqrMagnitude(card.transform.position - possibleMoves[i].position);
                                    if (sqrMagnitude < minDistance)
                                    {
                                        minDistance = sqrMagnitude;
                                        closestIndex = i;
                                    }
                                }
                            }

                            // Assign the next move to be the closest to the current card
                            nextMove = possibleMoves[closestIndex];
                        }
                    }
                }
            }

            return nextMove;
        }

        /**
         * Take all cards from talon and put them back in the stock
         */
        public void ReplinishStock(MoveTypes moveType = MoveTypes.NORMAL)
        {
            SnapManager talonSnapManager = talon.GetComponentInChildren<SnapManager>();

            Card[] talonCards = talonSnapManager.GetCardSet();
            if (DEBUG_MODE) Debug.Log("Cards in talon:");
            
            // Need to iterate in reverse order so that the cards are drawn from the stock in the same order as before
            for (int i = talonCards.Length - 1; i >= 0; i--)
            {
                Card card = talonCards[i];

                // Remove from talon
                card.transform.parent = null;

                // Move the card position from the talon to the stock
                card.transform.position = new Vector3(
                    m_stockPile.position.x,
                    m_stockPile.position.y,
                    card.transform.position.z
                );

                // Rotate the card to be face down again
                card.Flip(false);

                // Add the card to the stock
                card.transform.parent = m_stockPile;

                // Flip the first card from the stock pile over to the talon automatically
                if (i == 0)
                {
                    // Don't track this move
                    card.MoveTo(m_talonPile, null, MoveTypes.INCOGNITO);

                    // Need to manually flip card because of not tracking
                    if (card.IsFaceDown())
                        card.Flip();
                }
            }

            // Track replinish event
            if (moveType.Equals(MoveTypes.NORMAL))
            {
                Move move = new Move();
                move.SetSpecial(true);
                Event ev = new Event();
                ev.SetType(Event.EventType.REPLINISH);
                move.AddEvent(ev);
                AddMove(move, MoveTypes.NORMAL);
            }
        }

        /**
         * Used for reversing a replinishing event
         */
        public void DeplinishStock(MoveTypes moveType = MoveTypes.NORMAL)
        {
            SnapManager stockSnapManager = stock.GetComponentInChildren<SnapManager>();

            Card[] stockCards = stockSnapManager.GetCardSet();

            for (int i = stockCards.Length - 1; i >= 0; i--)
            {
                Card card = stockCards[i];

                // Remove from stock
                card.transform.parent = null;

                // Move the card position from the stock to the talon
                card.transform.position = new Vector3(
                    m_talonPile.position.x,
                    m_talonPile.position.y,
                    card.transform.position.z
                );

                // Rotate the card to be face up
                if (card.IsFaceDown())
                    card.Flip(false);

                // Add the card to the talon
                card.transform.parent = m_talonPile;
            }

            // Track deplinish event
            if (moveType.Equals(MoveTypes.NORMAL))
            {
                Move move = new Move();
                move.SetSpecial(true);
                Event ev = new Event();
                ev.SetType(Event.EventType.DEPLINISH);
                move.AddEvent(ev);
                AddMove(move, MoveTypes.NORMAL);
            }
        }

        /**
         * Get the current time in milliseconds that has elapsed on the
         * stop watch.
         * 
         * @return float the current time in milliseconds.
         */
        public long GetCurrentTime()
        {
            // Update the timer lable to make sure it is in sync with current value of stopwatch.
            // This handles corner case when the stop watch is stoped and this function is invoked (e.g., win state)
            UpdateTimer();
            return m_stopWatch.ElapsedMilliseconds;
        }

        private void LoadCardSprites()
        {
            // Load the card sprites from resources
            m_cardSprites = Resources.LoadAll<Sprite>("Sprites/playing_cards_spritesheet_01");
            if (DEBUG_MODE) Debug.Log("Loaded " + m_cardSprites.Length + " card sprites...");
            m_cardSpritesMap = new Dictionary<CardSuit, Sprite[]>();
            for (int i = 0; i < m_cardSprites.Length; i++)
            {
                Sprite sprite = m_cardSprites[i];
                string[] tokens = sprite.name.Split('_');
                int value = int.Parse(tokens[0]);
                string suitString = tokens[1];
                CardSuit suit;

                // Only process when value is between 0 and length of the card sprites
                if ((value - 1) < m_cardSprites.Length && (value - 1) >= 0)
                {
                    switch (suitString)
                    {
                        case "HEARTS":
                            suit = CardSuit.HEARTS;
                            break;
                        case "DIAMONDS":
                            suit = CardSuit.DIAMONDS;
                            break;
                        case "CLUBS":
                            suit = CardSuit.CLUBS;
                            break;
                        case "SPADES":
                            suit = CardSuit.SPADES;
                            break;
                        default:
                            suit = CardSuit.NONE;
                            break;
                    }

                    // Create the suit entry if it doesn't exist
                    if (!m_cardSpritesMap.ContainsKey(suit))
                    {
                        // 13 cards per suit
                        m_cardSpritesMap.Add(suit, new Sprite[13]);
                    }

                    // Value is not zero-indexed so we have to compensate for that
                    // Add the sprite to the respective suit entry and array slot
                    m_cardSpritesMap[suit][value - 1] = sprite;
                }
                else
                {
                    if (DEBUG_MODE) Debug.Log("Excluding " + sprite.name + " from the deck.");
                }
            }
        }

        /**
         * Shuffle an array of values based on specified datatype.
         * @param T[] array the template based array to shuffle.
         * @return T[] the array representing the shuffled version of the input array.
         */
        public void Shuffle<T>(ref List<T> array)
        {
            for (int i = array.Count; i > 1; i--)
            {
                // Pick random element to swap.
                int j = UnityEngine.Random.Range(0, array.Count - 1); // 0 <= j <= i-1
                                                          // Swap.
                T tmp = array[j];
                array[j] = array[i - 1];
                array[i - 1] = tmp;
            }
        }

        /**
         * Exit the game on an action event (button click)
         */
        public void ExitGame()
        {
            #if UNITY_EDITOR
                // Application.Quit() does not work in the editor so
                // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void SpawnStack()
        {
            // Generate the initial list of 52 cards first before shuffling
            List<CardTpl> deck = new List<CardTpl>();
            CardSuit[] cardSuits = new CardSuit[]
            {
                CardSuit.HEARTS, CardSuit.DIAMONDS, CardSuit.CLUBS, CardSuit.SPADES
            };

            int cardsPerSuit = 13;
            for (int i = 0; i < cardSuits.Length; i++)
            {
                for (int j = 0; j < cardsPerSuit; j++)
                {
                    CardTpl card = new CardTpl
                    {
                        value = j + 1,
                        suit = cardSuits[i] // Purposefully using 'i' to cycle through all cards before changing suit
                    };

                    // Add the card to the deck
                    deck.Add(card);
                }
            }

            // Shuffle the cards
            Shuffle(ref deck);

            // Spwan Tableau cards
            SpawnTableauCards(ref deck);

            // Spawn remaining cards to the stock
            SpawnStockCards(ref deck);
        }

        private void SpawnTableauCards(ref List<CardTpl> deck)
        {
            // Iterate through the tableau snaps and spawn cards from deck to them
            SnapManager[] tableauSnapManagers = tableau.GetComponentsInChildren<SnapManager>();
            int zOffset = 1;
            for (int i = 0; i < tableauSnapManagers.Length; i++)
            {
                Transform stackTarget = tableauSnapManagers[i].transform;

                for (int j = 0; j < i + 1; j++)
                {
                    Card card = cardPrefab.GetComponent<Card>();
                    card.value = deck[0].value;
                    card.suit = deck[0].suit;

                    // Get the sprite renderer for the current front face of this card
                    SpriteRenderer cardSprite = card.frontFace.GetComponent<SpriteRenderer>();

                    // Set the sprite for the front face
                    cardSprite.sprite = m_cardSpritesMap[card.suit][card.value - 1];

                    Vector3 posOffset = new Vector3(
                       stackTarget.position.x,
                       stackTarget.position.y - (FACE_DOWN_Y_OFFSET * j),
                       stackTarget.position.z - zOffset
                    );

                    // Spawn the card and add it to the respective tableau pile
                    GameObject spawnedCardObj = Instantiate(cardPrefab, posOffset, Quaternion.identity);
                    spawnedCardObj.name = VALUE_REF[card.value] + "_" + card.suit;
                    spawnedCardObj.transform.parent = stackTarget;

                    // Only make the last card flip face up
                    if (j + 1 == i + 1)
                    {
                        Card spawnedCard = spawnedCardObj.GetComponent<Card>();
                        spawnedCard.Flip(false);
                    }

                    zOffset++;

                    // Remove the card reference from the deck and move to the next card
                    deck.RemoveAt(0);
                }

                zOffset = 1;
            }
        }

        private void SpawnStockCards(ref List<CardTpl> deck)
        {
            // Card spawn location is dependent on the location of the Stock parent
            Transform stackTarget = stock.GetComponentInChildren<SnapManager>().transform;
            if (DEBUG_MODE) Debug.Log("Stack target is " + stackTarget.tag + " at " + stackTarget.position);

            int zOffset = 1;
            while (deck.Count != 0)
            {
                Card card = cardPrefab.GetComponent<Card>();
                card.value = deck[0].value;
                card.suit = deck[0].suit;

                // Get the sprite renderer for the current front face of this card
                SpriteRenderer cardSprite = card.frontFace.GetComponent<SpriteRenderer>();

                // Set the sprite for the front face
                cardSprite.sprite = m_cardSpritesMap[card.suit][card.value - 1];

                Vector3 posOffset = new Vector3(
                    stackTarget.position.x,
                    stackTarget.position.y,
                    stackTarget.position.z - zOffset
                );

                // Spawn the card and add it to the stock
                GameObject spawnedCardObj = Instantiate(cardPrefab, posOffset, Quaternion.identity);
                spawnedCardObj.name = VALUE_REF[card.value] + "_" + card.suit;
                spawnedCardObj.transform.parent = stackTarget;

                zOffset++;

                // Remove the card reference from the deck and move to the next card
                deck.RemoveAt(0);
            }
        }

        /**
         * Reset player prefs by deleting all keys. This should only be used
         * for testing and NOT in production.
         */
        public void ResetPlayerPrefs()
        {
            if (DEBUG_MODE || m_demoMode)
            {
                PlayerPrefs.DeleteAll();
                Debug.LogWarning("Player prefs have been successfully deleted.");
            }
            else
            {
                Debug.Log("Cannot reset player prefs because not in a valid game state (only supported in debug mode or demo mode).");
            }
           
        }

        /**
         * Perform the core function for running the auto complete process.
         * This coroutine will rapidly move cards from the tableau to the foundation
         * and automatically complete the game once a valid winning state has been
         * determined by the system.
         */
        private IEnumerator AutoWinCoroutine()
        {
            SetDoingAutoWin(true);

            int attempts = 0;
            int maxAttempts = 5000;

            // Build min priority queue based on all cards in tableau (prioritize lower value cards)
            PriorityQueue<Card> cardQueue = new PriorityQueue<Card>(true);

            // Add max attempts in case there is some corner case that occurs and causes a fail
            while (attempts < maxAttempts)
            {
                yield return new WaitForEndOfFrame();

                // Break out of the loop if the win state has been met
                if (HasWon())
                {
                    break;
                }

                SnapManager[] tableauSnaps = tableau.GetComponentsInChildren<SnapManager>();
                foreach (SnapManager tableauSnap in tableauSnaps)
                {
                    // Get only the top card for now
                    Card topCard = tableauSnap.GetTopCard();
                    if (topCard != null)
                    {
                        cardQueue.Enqueue(topCard.value, topCard);
                    }
                }

                // Dequeue cards from priority queue and move each one to the foundation
                while (cardQueue.Count > 0)
                {
                    Card priorityCard = cardQueue.Dequeue();
                    Transform nextMove = GetNextAvailableMove(priorityCard);

                    // Only process move if one existed and if it is to a foundation
                    if (nextMove)
                    {
                        SnapManager currentSnapManager = priorityCard.GetComponentInParent<SnapManager>();
                        SnapManager nextSnapManager = nextMove.GetComponent<SnapManager>();
                        bool validMove = nextSnapManager.belongingSection.Equals(Sections.FOUNDATIONS);
                        if (validMove)
                        {
                            // Make note of the current amount of attached cards
                            int currentCardCount = nextSnapManager.GetCardCount();

                            priorityCard.MoveTo(nextMove);

                            // Wait until the card is finished translating, is attached to the snap, and the current
                            // card count has incremented by one
                            yield return new WaitUntil(() => {
                                return priorityCard.transform.parent != null &&
                                       !priorityCard.IsTranslating() &&
                                       nextSnapManager.GetCardCount() >= (currentCardCount + 1);
                            });

                            // Add the current snap manager's top card to the priority queue
                            Card nextTopCard = currentSnapManager.GetTopCard();
                            if (nextTopCard != null)
                            {
                                cardQueue.Enqueue(nextTopCard.value, nextTopCard);
                            }
                        }
                    }
                }

                attempts++;
            }

            SetDoingAutoWin(false);
            m_autoWinComplete = true;
        }
    }
}

