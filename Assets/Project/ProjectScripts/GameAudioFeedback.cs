using UnityEngine;

/// <summary>Подписчик GameEvents — звуки матча, ошибки, shuffle, таймера и победы.</summary>
[DisallowMultipleComponent]
public class GameAudioFeedback : MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnMatch += HandleMatch;
        GameEvents.OnWrongMatch += HandleWrongMatch;
        GameEvents.OnTimeUp += HandleTimeUp;
        GameEvents.OnBoardShuffled += HandleBoardShuffled;
        GameEvents.OnGameOver += HandleGameOver;
    }

    void OnDisable()
    {
        GameEvents.OnMatch -= HandleMatch;
        GameEvents.OnWrongMatch -= HandleWrongMatch;
        GameEvents.OnTimeUp -= HandleTimeUp;
        GameEvents.OnBoardShuffled -= HandleBoardShuffled;
        GameEvents.OnGameOver -= HandleGameOver;
    }

    static void HandleMatch() => AudioManager.Instance?.PlayMatch();
    static void HandleWrongMatch() => AudioManager.Instance?.PlayWrong();
    static void HandleBoardShuffled() => AudioManager.Instance?.PlayShuffle();

    static void HandleTimeUp()
    {
        AudioManager.Instance?.PlayLose();
        AudioManager.Instance?.PauseMusic(true);
    }

    static void HandleGameOver(bool won)
    {
        if (!won) return;
        AudioManager.Instance?.PlayWin();
        AudioManager.Instance?.PauseMusic(true);
    }
}
