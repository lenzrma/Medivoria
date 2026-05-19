using System;

/// <summary>Observer: игровые события без прямой связи Grid/UI/Audio.</summary>
public static class GameEvents
{
    public static event Action OnMatch;
    public static event Action OnWrongMatch;
    public static event Action OnTimeUp;
    public static event Action OnBoardShuffled;
    public static event Action<bool> OnGameOver;

    public static void RaiseMatch() => OnMatch?.Invoke();
    public static void RaiseWrongMatch() => OnWrongMatch?.Invoke();
    public static void RaiseTimeUp() => OnTimeUp?.Invoke();
    public static void RaiseBoardShuffled() => OnBoardShuffled?.Invoke();
    public static void RaiseGameOver(bool won) => OnGameOver?.Invoke(won);
}
