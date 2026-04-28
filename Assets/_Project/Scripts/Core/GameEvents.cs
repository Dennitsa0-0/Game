namespace ChronoDrop.Core
{
    // ── Gameplay lifecycle ──────────────────────────────────────────────────
    public readonly struct GameStartedEvent { }

    public readonly struct GameOverEvent
    {
        public readonly float DepthMeters;
        public GameOverEvent(float depth) { DepthMeters = depth; }
    }

    public readonly struct GamePausedEvent
    {
        public readonly bool IsPaused;
        public GamePausedEvent(bool paused) { IsPaused = paused; }
    }

    // ── Player ─────────────────────────────────────────────────────────────
    public readonly struct PlayerDiedEvent
    {
        public readonly float DepthMeters;
        public PlayerDiedEvent(float depth) { DepthMeters = depth; }
    }

    public readonly struct NearMissEvent
    {
        public readonly float DistanceMeters;
        public NearMissEvent(float distance) { DistanceMeters = distance; }
    }

    // ── Economy ────────────────────────────────────────────────────────────
    public readonly struct CrystalCollectedEvent
    {
        public readonly int Amount;
        public CrystalCollectedEvent(int amount) { Amount = amount; }
    }

    // ── World ──────────────────────────────────────────────────────────────
    public readonly struct SpeedChangedEvent
    {
        public readonly float Speed;
        public SpeedChangedEvent(float speed) { Speed = speed; }
    }

    public readonly struct EraTransitionEvent
    {
        public readonly string EraDisplayName;
        public EraTransitionEvent(string name) { EraDisplayName = name; }
    }
}
