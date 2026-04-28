using UnityEngine;
using ChronoDrop.Core;

/// <summary>
/// Root scene entry point. Owns the GameStateMachine and wires top-level UI buttons.
/// Place on a dedicated "Bootstrap" GameObject in the Main scene.
/// </summary>
public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameStateMachine stateMachine;

    [Header("MVP: skip main menu and start immediately")]
    [SerializeField] private bool autoStartOnBoot = true;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    private void Start()
    {
        if (autoStartOnBoot)
            stateMachine.StartGame();
        else
            stateMachine.TransitionTo(GameState.MainMenu);
    }

    // ── Called by UI buttons ────────────────────────────────────────────────
    public void OnStartButtonPressed()   => stateMachine.StartGame();
    public void OnPauseButtonPressed()   => stateMachine.PauseGame();
    public void OnResumeButtonPressed()  => stateMachine.ResumeGame();
    public void OnRestartButtonPressed() => stateMachine.RestartGame();
    public void OnMenuButtonPressed()    => stateMachine.ReturnToMenu();
}
