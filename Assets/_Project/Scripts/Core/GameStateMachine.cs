using System;
using UnityEngine;

namespace ChronoDrop.Core
{
    public enum GameState
    {
        Boot,
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public sealed class GameStateMachine : MonoBehaviour
    {
        public GameState Current { get; private set; } = GameState.Boot;

        public event Action<GameState, GameState> StateChanged;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        }

        // ── Public API (called by GameBootstrap / UI buttons) ───────────────

        public void StartGame()   => TransitionTo(GameState.Playing);
        public void PauseGame()   => TransitionTo(GameState.Paused);
        public void ResumeGame()  => TransitionTo(GameState.Playing);
        public void ReturnToMenu() => TransitionTo(GameState.MainMenu);

        public void RestartGame()
        {
            // Force re-entry into Playing even if already there
            GameState previous = Current;
            Current = GameState.GameOver;          // ensure the "same state" guard won't block
            TransitionTo(GameState.Playing);
        }

        // ── Internal ────────────────────────────────────────────────────────

        public void TransitionTo(GameState next)
        {
            if (next == Current)
                return;

            GameState previous = Current;
            Current = next;

            StateChanged?.Invoke(previous, next);
            DispatchBusEvent(next);
        }

        private void DispatchBusEvent(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    EventBus.Raise(new GameStartedEvent());
                    break;

                case GameState.Paused:
                    EventBus.Raise(new GamePausedEvent(true));
                    break;

                case GameState.MainMenu:
                    EventBus.Raise(new GamePausedEvent(false));
                    break;
            }
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            if (Current != GameState.Playing)
                return;

            TransitionTo(GameState.GameOver);
            EventBus.Raise(new GameOverEvent(evt.DepthMeters));
        }
    }
}
