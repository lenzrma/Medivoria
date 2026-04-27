using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameRunSaveData
{
    public const int KindMidGame = 0;
    public const int KindNextLevelAfterVictory = 1;

    public int version = 1;
    /// <summary>0 — снимок сетки, 1 — после победы: начать уровень <see cref="level"/> с чистой сеткой.</summary>
    public int saveKind;
    public int columns;
    public int rows;
    public int level;
    public float timeRemaining;
    public int hints;
    public int matches;
    public int[] cells = Array.Empty<int>();
}

public static class GameRunPersistence
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "medivoria_run.json");

    public static bool HasSave() => File.Exists(FilePath);

    public static void Save(GameRunSaveData data)
    {
        if (data == null) return;
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameRunPersistence.Save: " + e.Message);
        }
    }

    public static GameRunSaveData Load()
    {
        if (!HasSave()) return null;
        try
        {
            return JsonUtility.FromJson<GameRunSaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameRunPersistence.Load: " + e.Message);
            return null;
        }
    }

    public static void Clear()
    {
        try
        {
            if (HasSave()) File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("GameRunPersistence.Clear: " + e.Message);
        }
    }
}

public static class GameSession
{
    /// <summary>Следующая загрузка GameScene должна восстановить сейв.</summary>
    public static bool RequestContinue;
}
