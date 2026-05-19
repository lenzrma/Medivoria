using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public enum TileMovementMode
    {
        Static,
        FromTop,
        FromBottom
    }

    public static GridManager Instance;

    [Header("Grid Settings")]
    public int columns = 16;
    public int rows = 9;
    public GameObject tilePrefab;
    public Transform gridParent;

    [Header("Tile Sprites")]
    public Sprite[] tileSprites;

    [Header("Background")]
    public Image backgroundImage;
    public Sprite[] levelBackgrounds;

    private Tile[,] grid;
    private Tile firstSelected = null;
    private bool isProcessing = false;
    private List<GameObject> lineObjects = new List<GameObject>();
    private TileMovementMode movementMode = TileMovementMode.Static;
    private int currentLevel = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GenerateGrid()
    {
        grid = new Tile[columns, rows];

        List<int> tileTypes = GenerateQuads();
        Shuffle(tileTypes);

        foreach (Transform child in gridParent)
        {
            if (child != null && child.name == GameManager.PlayBoardMatteChildName) continue;
            Destroy(child.gameObject);
        }

        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int index = y * columns + x;
                GameObject tileGO = Instantiate(tilePrefab, gridParent);
                Tile tile = tileGO.GetComponent<Tile>();
                tile.Init(x, y, tileTypes[index], tileSprites[tileTypes[index]]);
                grid[x, y] = tile;
            }

        // На старте всегда должен быть хотя бы один ход.
        EnsurePlayableBoard();
    }

    public void SetupLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, 10);
        movementMode = GetMovementModeForLevel(currentLevel);

        if (backgroundImage != null && levelBackgrounds != null && levelBackgrounds.Length >= currentLevel)
            backgroundImage.sprite = levelBackgrounds[currentLevel - 1];
    }

    TileMovementMode GetMovementModeForLevel(int level)
    {
        if (level <= 4) return TileMovementMode.Static;
        if (level <= 7) return TileMovementMode.FromTop;
        return TileMovementMode.FromBottom;
    }

    List<int> GenerateQuads()
    {
        List<int> types = new List<int>();
        for (int i = 0; i < tileSprites.Length; i++)
        { types.Add(i); types.Add(i); types.Add(i); types.Add(i); }
        return types;
    }

    void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void OnTileClicked(Tile tile)
    {
        if (isProcessing || !GameManager.Instance.IsGameRunning()) return;
        if (tile.IsMatched) return;

        if (firstSelected == tile)
        {
            firstSelected.SetSelected(false);
            firstSelected = null;
            return;
        }

        if (firstSelected == null)
        {
            firstSelected = tile;
            tile.SetSelected(true);
        }
        else
        {
            if (firstSelected.TileType == tile.TileType)
            {
                List<Vector2Int> path = FindPath(firstSelected.GridX, firstSelected.GridY, tile.GridX, tile.GridY);
                if (path != null)
                {
                    StartCoroutine(MatchTiles(firstSelected, tile, path));
                    firstSelected = null;
                }
                else
                {
                    StartCoroutine(WrongSelection(tile));
                }
            }
            else
            {
                firstSelected.SetSelected(false);
                firstSelected = tile;
                tile.SetSelected(true);
            }
        }
    }

    IEnumerator MatchTiles(Tile a, Tile b, List<Vector2Int> path)
    {
        isProcessing = true;
        DrawPath(path);
        yield return new WaitForSeconds(0.4f);
        ClearLines();
        a.SetMatched();
        b.SetMatched();
        GameManager.Instance.OnMatchFound();

        if (!GameManager.Instance.IsGameRunning())
        {
            isProcessing = false;
            yield break;
        }

        yield return new WaitForSeconds(0.32f);
        ApplyMovementAfterMatch();

        if (!GameManager.Instance.IsGameRunning())
        {
            isProcessing = false;
            yield break;
        }

        if (!HasLivingTiles())
        {
            GameManager.Instance.OnAllTilesCleared();
            isProcessing = false;
            yield break;
        }

        if (!HasAnyMoves() && HasLivingTiles())
        {
            if (TryStartDeadlockResolve())
                yield break;
            yield return StartCoroutine(ShuffleBoard());
        }
        else
            isProcessing = false;
    }

    IEnumerator WrongSelection(Tile b)
    {
        isProcessing = true;
        b.SetSelected(true);
        yield return new WaitForSeconds(0.4f);
        firstSelected.SetSelected(false);
        b.SetSelected(false);
        firstSelected = null;
        GameEvents.RaiseWrongMatch();
        isProcessing = false;
    }


    bool CellEmpty(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return false;
        return grid[x, y] == null || grid[x, y].IsMatched;
    }

    bool CellEmptyOrOut(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return true;
        return grid[x, y] == null || grid[x, y].IsMatched;
    }

    bool HasDirectPath(int x1, int y1, int x2, int y2)
    {
        if (x1 == x2)
        {
            int minY = Mathf.Min(y1, y2);
            int maxY = Mathf.Max(y1, y2);
            for (int y = minY + 1; y < maxY; y++)
                if (!CellEmptyOrOut(x1, y)) return false;
            return true;
        }
        if (y1 == y2)
        {
            int minX = Mathf.Min(x1, x2);
            int maxX = Mathf.Max(x1, x2);
            for (int x = minX + 1; x < maxX; x++)
                if (!CellEmptyOrOut(x, y1)) return false;
            return true;
        }
        return false;
    }

    public List<Vector2Int> FindPath(int ax, int ay, int bx, int by)
    {

        const int maxTurns = 2;
        const int dirCount = 4;

        if (ax == bx && ay == by) return null;

        bool Passable(int x, int y)
        {

            if (x < 0 || x >= columns || y < 0 || y >= rows) return true;
            if ((x == ax && y == ay) || (x == bx && y == by)) return true;
            return CellEmpty(x, y);
        }

        int W = columns + 2;
        int H = rows + 2;
        int Idx(int x, int y) => (y + 1) * W + (x + 1); 

        int total = W * H * dirCount;
        int[] bestTurns = new int[total];
        int[] prevPacked = new int[total]; 
        for (int i = 0; i < total; i++) { bestTurns[i] = 999; prevPacked[i] = 0; }

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        var q = new Queue<(int x, int y, int dir)>();

        for (int d = 0; d < dirCount; d++)
        {
            int state = (Idx(ax, ay) * dirCount) + d;
            bestTurns[state] = 0;
            prevPacked[state] = 0;
            q.Enqueue((ax, ay, d));
        }

        int bestEndState = -1;

        while (q.Count > 0)
        {
            var (x, y, dir) = q.Dequeue();
            int baseIdx = Idx(x, y);
            int baseState = baseIdx * dirCount + dir;
            int turns = bestTurns[baseState];
            if (turns > maxTurns) continue;

            if (x == bx && y == by)
            {
                bestEndState = baseState;
                break;
            }

            for (int nd = 0; nd < dirCount; nd++)
            {
                int nx = x + dx[nd];
                int ny = y + dy[nd];
                if (nx < -1 || nx > columns || ny < -1 || ny > rows) continue;
                if (!Passable(nx, ny)) continue;

                int nTurns = turns + (nd == dir ? 0 : 1);
                if (nTurns > maxTurns) continue;

                int nState = (Idx(nx, ny) * dirCount) + nd;
                if (nTurns >= bestTurns[nState]) continue;

                bestTurns[nState] = nTurns;
                prevPacked[nState] = (baseIdx * dirCount + dir) + 1;
                q.Enqueue((nx, ny, nd));
            }
        }

        if (bestEndState < 0)
        {
            int endIdx = Idx(bx, by);
            int best = 999;
            for (int d = 0; d < dirCount; d++)
            {
                int s = endIdx * dirCount + d;
                if (bestTurns[s] < best)
                {
                    best = bestTurns[s];
                    bestEndState = s;
                }
            }
            if (best > maxTurns) return null;
        }

        var cells = new List<Vector2Int>();
        int curState = bestEndState;
        while (curState >= 0)
        {
            int cellIdx = curState / dirCount;
            int cx = (cellIdx % W) - 1;
            int cy = (cellIdx / W) - 1;
            cells.Add(new Vector2Int(cx, cy));

            int packedPrev = prevPacked[curState];
            if (packedPrev == 0) break;
            curState = packedPrev - 1;
        }
        cells.Reverse();

        var path = new List<Vector2Int>();
        if (cells.Count == 0) return null;
        path.Add(cells[0]);
        for (int i = 1; i < cells.Count - 1; i++)
        {
            Vector2Int a = cells[i - 1];
            Vector2Int b = cells[i];
            Vector2Int c = cells[i + 1];
            Vector2Int d1 = b - a;
            Vector2Int d2 = c - b;
            if (d1 != d2) path.Add(b);
        }
        path.Add(cells[^1]);

        return path;
    }

    List<Vector2Int> MakePath(params int[] coords)
    {
        var path = new List<Vector2Int>();
        for (int i = 0; i < coords.Length; i += 2)
            path.Add(new Vector2Int(coords[i], coords[i + 1]));
        return path;
    }

    void DrawPath(List<Vector2Int> path)
    {
        for (int i = 0; i < path.Count - 1; i++)
            DrawSegment(path[i], path[i + 1]);
    }

    void DrawSegment(Vector2Int from, Vector2Int to)
    {
        Vector3 startPos = GetTileWorldPos(from);
        Vector3 endPos = GetTileWorldPos(to);

        GameObject go = new GameObject("Line");
        go.transform.SetParent(gridParent.parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(1f, 0.8f, 0f, 0.9f);
        lr.startWidth = lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.SetPosition(0, startPos + Vector3.forward * -1);
        lr.SetPosition(1, endPos + Vector3.forward * -1);
        lr.sortingOrder = 100;
        lineObjects.Add(go);
    }

    Vector3 GetTileWorldPos(Vector2Int pos)
    {
        if (pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows)
        {
            if (grid[pos.x, pos.y] != null)
                return grid[pos.x, pos.y].transform.position;
        }
        RectTransform rt = gridParent as RectTransform;
        float tw = rt.rect.width / columns;
        float th = rt.rect.height / rows;
        Vector3 origin = rt.position - new Vector3(rt.rect.width / 2 - tw / 2, -rt.rect.height / 2 + th / 2, 0);
        return origin + new Vector3(pos.x * tw, -pos.y * th, 0);
    }

    void ClearLines()
    {
        foreach (var go in lineObjects) if (go) Destroy(go);
        lineObjects.Clear();
    }

    bool HasLivingTiles()
    {
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
            if (!CellEmpty(x, y)) return true;
        return false;
    }

    bool HasAnyMoves()
    {
        for (int x1 = 0; x1 < columns; x1++)
        for (int y1 = 0; y1 < rows; y1++)
        {
            if (CellEmpty(x1, y1)) continue;
            for (int x2 = 0; x2 < columns; x2++)
            for (int y2 = 0; y2 < rows; y2++)
            {
                if (x1 == x2 && y1 == y2) continue;
                if (CellEmpty(x2, y2)) continue;
                if (grid[x1, y1].TileType == grid[x2, y2].TileType)
                    if (FindPath(x1, y1, x2, y2) != null) return true;
            }
        }
        return false;
    }

    /// <summary>Поле пустое или последняя пара без пути — не зацикливать shuffle.</summary>
    bool TryStartDeadlockResolve()
    {
        List<Tile> alive = CollectAliveTiles();
        if (alive.Count == 0)
        {
            GameManager.Instance.OnAllTilesCleared();
            isProcessing = false;
            return true;
        }

        if (alive.Count == 2 && alive[0].TileType == alive[1].TileType)
        {
            if (FindPath(alive[0].GridX, alive[0].GridY, alive[1].GridX, alive[1].GridY) != null)
                return false;

            StartCoroutine(ForceFinishLastPair(alive[0], alive[1]));
            return true;
        }

        return false;
    }

    IEnumerator ForceFinishLastPair(Tile a, Tile b)
    {
        isProcessing = true;
        if (firstSelected != null)
        {
            firstSelected.SetSelected(false);
            firstSelected = null;
        }

        var path = new List<Vector2Int>
        {
            new Vector2Int(a.GridX, a.GridY),
            new Vector2Int(b.GridX, b.GridY)
        };
        yield return MatchTiles(a, b, path);
    }

    List<Tile> CollectAliveTiles()
    {
        var list = new List<Tile>();
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
            if (!CellEmpty(x, y)) list.Add(grid[x, y]);
        return list;
    }

    IEnumerator ShuffleBoard()
    {
        if (!GameManager.Instance.IsGameRunning())
        {
            isProcessing = false;
            yield break;
        }

        isProcessing = true;
        if (firstSelected != null)
        {
            firstSelected.SetSelected(false);
            firstSelected = null;
        }

        if (TryStartDeadlockResolve())
            yield break;

        Debug.Log("Нет ходов — перемешиваем!");
        yield return new WaitForSeconds(0.5f);

        List<Tile> alive = new List<Tile>();
        List<int> types = new List<int>();
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
            if (!CellEmpty(x, y)) { alive.Add(grid[x, y]); types.Add(grid[x, y].TileType); }

        Shuffle(types);

        for (int i = 0; i < alive.Count; i++)
            alive[i].Reinit(types[i], tileSprites[types[i]]);

        EnsurePlayableBoard();

        GameEvents.RaiseBoardShuffled();
        isProcessing = false;
    }

    void EnsurePlayableBoard()
    {
        if (HasAnyMoves()) return;

        List<Tile> alive = new List<Tile>();
        List<int> types = new List<int>();
        for (int x = 0; x < columns; x++)
        for (int y = 0; y < rows; y++)
            if (!CellEmpty(x, y))
            {
                alive.Add(grid[x, y]);
                types.Add(grid[x, y].TileType);
            }

        const int maxAttempts = 60;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Shuffle(types);
            for (int i = 0; i < alive.Count; i++)
                alive[i].Reinit(types[i], tileSprites[types[i]]);

            if (HasAnyMoves())
                return;
        }

        Debug.LogWarning("Не удалось найти валидную раскладку за лимит попыток.");
    }

    void ApplyMovementAfterMatch()
    {
        if (movementMode == TileMovementMode.Static) return;

        for (int x = 0; x < columns; x++)
        {
            List<int> aliveTypes = new List<int>();

            for (int y = 0; y < rows; y++)
                if (!CellEmpty(x, y))
                    aliveTypes.Add(grid[x, y].TileType);

            if (movementMode == TileMovementMode.FromTop)
                FillColumnFromTop(x, aliveTypes);
            else
                FillColumnFromBottom(x, aliveTypes);
        }
    }

    void FillColumnFromTop(int x, List<int> aliveTypes)
    {
        int targetY = rows - 1;
        for (int i = aliveTypes.Count - 1; i >= 0; i--)
        {
            grid[x, targetY].Reinit(aliveTypes[i], tileSprites[aliveTypes[i]]);
            targetY--;
        }

        for (int y = targetY; y >= 0; y--)
            grid[x, y].SetEmpty();
    }

    void FillColumnFromBottom(int x, List<int> aliveTypes)
    {
        int targetY = 0;
        for (int i = 0; i < aliveTypes.Count; i++)
        {
            grid[x, targetY].Reinit(aliveTypes[i], tileSprites[aliveTypes[i]]);
            targetY++;
        }

        for (int y = targetY; y < rows; y++)
            grid[x, y].SetEmpty();
    }

    public void ShowHint()
    {
        if (isProcessing) return;
        for (int x1 = 0; x1 < columns; x1++)
        for (int y1 = 0; y1 < rows; y1++)
        {
            if (CellEmpty(x1, y1)) continue;
            for (int x2 = 0; x2 < columns; x2++)
            for (int y2 = 0; y2 < rows; y2++)
            {
                if (x1 == x2 && y1 == y2) continue;
                if (CellEmpty(x2, y2)) continue;
                if (grid[x1, y1].TileType == grid[x2, y2].TileType)
                    if (FindPath(x1, y1, x2, y2) != null)
                    {
                        StartCoroutine(HintFlash(grid[x1, y1], grid[x2, y2]));
                        return;
                    }
            }
        }
        if (HasLivingTiles())
            StartCoroutine(ShuffleBoard());
    }

    IEnumerator HintFlash(Tile a, Tile b)
    {
        for (int i = 0; i < 3; i++)
        {
            a.SetHighlight(true); b.SetHighlight(true);
            yield return new WaitForSeconds(0.3f);
            a.SetHighlight(false); b.SetHighlight(false);
            yield return new WaitForSeconds(0.2f);
        }
    }

    public bool CanConnect(Tile a, Tile b) => FindPath(a.GridX, a.GridY, b.GridX, b.GridY) != null;
    public int TotalPairs => (columns * rows) / 2;
    public bool IsBusy => isProcessing;

    public void ForceShuffle()
    {
        if (!isProcessing) StartCoroutine(ShuffleBoard());
    }

    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= columns || y < 0 || y >= rows) return null;
        return grid[x, y];
    }

    /// <summary>Порядок как в GenerateGrid: y снаружи, x внутри. -1 = пустая/собранная клетка.</summary>
    public int[] ExportGridFlat()
    {
        int[] data = new int[columns * rows];
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            Tile t = grid[x, y];
            int i = y * columns + x;
            if (t == null || t.IsMatched) data[i] = -1;
            else data[i] = t.TileType;
        }
        return data;
    }

    public void RestoreGridFromSave(int[] data)
    {
        if (data == null || data.Length != columns * rows)
        {
            GenerateGrid();
            return;
        }

        firstSelected = null;
        isProcessing = false;
        grid = new Tile[columns, rows];

        foreach (Transform child in gridParent)
        {
            if (child != null && child.name == GameManager.PlayBoardMatteChildName) continue;
            Destroy(child.gameObject);
        }

        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            int i = y * columns + x;
            int t = data[i];
            GameObject tileGO = Instantiate(tilePrefab, gridParent);
            Tile tile = tileGO.GetComponent<Tile>();
            if (t < 0 || t >= tileSprites.Length)
            {
                tile.Init(x, y, 0, tileSprites[0]);
                tile.SetEmpty();
            }
            else tile.Init(x, y, t, tileSprites[t]);
            grid[x, y] = tile;
        }

        EnsurePlayableBoard();
        if (!HasAnyMoves() && HasLivingTiles())
            StartCoroutine(ShuffleBoard());
    }
}
