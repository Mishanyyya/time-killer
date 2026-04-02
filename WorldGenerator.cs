using Godot;
using System;
using System.Collections.Generic;

public partial class WorldGenerator : Node2D
{
    [Export] public TileMapLayer tileMapLayer;
    [Export] public NavigationRegion2D navRegion;
    [Export] public PackedScene enemyScene;
    [Export] public PackedScene playerScene;
    [Export] public Node2D enemyContainer; // сюда будут добавляться все враги
    [Export] public int width = 100;
    [Export] public int height = 100;
    [Export] public float noiseFrequency = 0.12f;   // выше = больше мелких препятствий
    [Export] public float walkableThreshold = 0.15f; // ниже = меньше проходимо, больше стен
    [Export] public int seed = 0;
    [Export] public int enemyCount = 5;

    [Export] public int walkableSourceId = 0;
    [Export] public Vector2I walkableAtlasCoords = new(0, 0);
    [Export] public int blockedSourceId = 0;
    [Export] public Vector2I blockedAtlasCoords = new(1, 0);

    private FastNoiseLite noise;
    private Node2D player;

    public override void _Ready()
    {
        if (tileMapLayer == null)
        {
            GD.PushError("WorldGenerator: tileMapLayer is not assigned.");
            return;
        }

        if (enemyContainer == null)
        {
            GD.PushError("WorldGenerator: enemyContainer is not assigned.");
            return;
        }

        if (seed == 0)
            seed = (int)GD.Randi();

        noise = new FastNoiseLite();
        noise.Seed = seed;
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        noise.Frequency = noiseFrequency;

        // Генерируем карту, пока она не будет полностью связной (макс 20 попыток)
        int regenerateAttempts = 0;
        bool isConnected = false;
        while (!isConnected && regenerateAttempts < 20)
        {
            GenerateTileMap();
            isConnected = IsMapConnected();
            if (!isConnected)
            {
                seed++;
                noise.Seed = seed;
                regenerateAttempts++;
            }
        }

        if (!isConnected)
        {
            GD.PushWarning("Не удалось сгенерировать полностью связную карту");
        }

        GenerateNavMesh();
        SpawnPlayer();
        SpawnEnemies(enemyCount);
    }

    private void GenerateTileMap()
    {
        tileMapLayer.Clear();

        // Шаг 1: Генерируем базовую карту по Perlin шуму
        bool[,] walkableMap = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float n = noise.GetNoise2D(x, y);
                walkableMap[x, y] = n < walkableThreshold;
            }
        }

        // Шаг 2: Пост-обработка — убираем диагональные щели
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Диагональ вниз-вправо
                if (x + 1 < width && y + 1 < height)
                {
                    bool right = walkableMap[x + 1, y];
                    bool down = walkableMap[x, y + 1];
                    bool diagonal = walkableMap[x + 1, y + 1];
                    if (!right && !down && diagonal)
                        walkableMap[x + 1, y + 1] = false;
                }

                // Диагональ вниз-влево
                if (x - 1 >= 0 && y + 1 < height)
                {
                    bool left = walkableMap[x - 1, y];
                    bool down = walkableMap[x, y + 1];
                    bool diagonal = walkableMap[x - 1, y + 1];
                    if (!left && !down && diagonal)
                        walkableMap[x - 1, y + 1] = false;
                }

                // Диагональ вверх-вправо
                if (x + 1 < width && y - 1 >= 0)
                {
                    bool right = walkableMap[x + 1, y];
                    bool up = walkableMap[x, y - 1];
                    bool diagonal = walkableMap[x + 1, y - 1];
                    if (!right && !up && diagonal)
                        walkableMap[x + 1, y - 1] = false;
                }

                // Диагональ вверх-влево
                if (x - 1 >= 0 && y - 1 >= 0)
                {
                    bool left = walkableMap[x - 1, y];
                    bool up = walkableMap[x, y - 1];
                    bool diagonal = walkableMap[x - 1, y - 1];
                    if (!left && !up && diagonal)
                        walkableMap[x - 1, y - 1] = false;
                }
            }
        }

        // Шаг 3: Дополнительное расширение препятствий
        bool[,] expandedMap = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                expandedMap[x, y] = walkableMap[x, y];
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!walkableMap[x, y])
                {
                    if (x > 0 && y > 0)
                        expandedMap[x - 1, y - 1] = false;
                    if (x < width - 1 && y > 0)
                        expandedMap[x + 1, y - 1] = false;
                    if (x > 0 && y < height - 1)
                        expandedMap[x - 1, y + 1] = false;
                    if (x < width - 1 && y < height - 1)
                        expandedMap[x + 1, y + 1] = false;
                }
            }
        }

        // Шаг 3.5: Убираем паттерны типа 101/010/101 (шахматный узор)
        // Если центр проходим, но все 4 диагональных соседа непроходимы - блокируем центр
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (expandedMap[x, y]) // если центр проходим
                {
                    bool topLeft = !expandedMap[x - 1, y - 1];
                    bool topRight = !expandedMap[x + 1, y - 1];
                    bool bottomLeft = !expandedMap[x - 1, y + 1];
                    bool bottomRight = !expandedMap[x + 1, y + 1];

                    // Если все 4 диагональных соседа заблокированы
                    if (topLeft && topRight && bottomLeft && bottomRight)
                    {
                        expandedMap[x, y] = false; // блокируем центр
                    }
                }
            }
        }

        // Шаг 4: Записываем результат в TileMapLayer
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool walkable = expandedMap[x, y];
                tileMapLayer.SetCell(
                    new Vector2I(x, y),
                    walkable ? walkableSourceId : blockedSourceId,
                    walkable ? walkableAtlasCoords : blockedAtlasCoords
                );
            }
        }
    }

    private bool IsMapConnected()
    {
        // Находим первую проходимую клетку
        Vector2I start = Vector2I.Zero;
        int totalWalkable = 0;
        bool foundStart = false;

        for (int x = 0; x < width && !foundStart; x++)
        {
            for (int y = 0; y < height && !foundStart; y++)
            {
                int sourceId = tileMapLayer.GetCellSourceId(new Vector2I(x, y));
                if (sourceId == walkableSourceId)
                {
                    start = new Vector2I(x, y);
                    foundStart = true;
                }
            }
        }

        // Считаем все проходимые клетки
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int sourceId = tileMapLayer.GetCellSourceId(new Vector2I(x, y));
                if (sourceId == walkableSourceId)
                    totalWalkable++;
            }
        }

        if (!foundStart || totalWalkable == 0) 
            return false; // нет проходимых клеток

        // Flood fill от стартовой клетки (только ортогональные соседи)
        bool[,] visited = new bool[width, height];
        Queue<Vector2I> queue = new();
        queue.Enqueue(start);
        visited[start.X, start.Y] = true;
        int connectedCount = 1;

        Vector2I[] cardinalDirections = [Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right];

        while (queue.Count > 0)
        {
            Vector2I current = queue.Dequeue();

            foreach (var dir in cardinalDirections)
            {
                Vector2I neighbor = current + dir;
                if (neighbor.X >= 0 && neighbor.X < width && 
                    neighbor.Y >= 0 && neighbor.Y < height &&
                    !visited[neighbor.X, neighbor.Y])
                {
                    int sourceId = tileMapLayer.GetCellSourceId(neighbor);
                    if (sourceId == walkableSourceId)
                    {
                        visited[neighbor.X, neighbor.Y] = true;
                        queue.Enqueue(neighbor);
                        connectedCount++;
                    }
                }
            }
        }

        bool isConnected = connectedCount == totalWalkable;
        if (!isConnected)
        {
            GD.Print($"Карта несвязна: {totalWalkable} клеток, но достижимо только {connectedCount}");
        }
        return isConnected;
    }

    private void GenerateNavMesh()
    {
        if (navRegion == null)
        {
            return;
        }

        // Navigation is expected to come from TileSet navigation polygons on the placed tiles.
        if (navRegion.NavigationPolygon == null)
        {
            GD.PushWarning("WorldGenerator: navRegion has no NavigationPolygon assigned.");
        }
    }

    private void SpawnPlayer()
    {
        if (playerScene == null) return;

        Vector2I spawn = FindEmptyTile();
        player = (Node2D)playerScene.Instantiate();
        player.GlobalPosition = tileMapLayer.ToGlobal(tileMapLayer.MapToLocal(spawn));
        AddChild(player);
    }

    private void SpawnEnemies(int count)
    {
        if (enemyScene == null) return;

        for (int i = 0; i < count; i++)
        {
            SpawnEnemyOnce();
        }
    }

    private void SpawnEnemyOnce()
    {
        if (enemyScene == null || enemyContainer == null) return;

        Vector2I spawn = FindEmptyTile();
        var enemyInstance = (Enemy)enemyScene.Instantiate();

        enemyInstance.GlobalPosition = tileMapLayer.ToGlobal(tileMapLayer.MapToLocal(spawn));
        enemyInstance.Player = player; // передаём ссылку на игрока
        enemyContainer.AddChild(enemyInstance);

        GD.Print($"Spawning enemy at {spawn}");
    }

    private Vector2I FindEmptyTile()
    {
        for (int attempt = 0; attempt < 5000; attempt++)
        {
            int x = (int)(GD.Randi() % (uint)width);
            int y = (int)(GD.Randi() % (uint)height);
            Vector2I cell = new(x, y);

            int sourceId = tileMapLayer.GetCellSourceId(cell);
            Vector2I atlas = tileMapLayer.GetCellAtlasCoords(cell);

            if (sourceId == walkableSourceId && atlas == walkableAtlasCoords)
                return cell;
        }

        return new Vector2I(width / 2, height / 2);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("spawn_enemy"))
        {
            SpawnEnemyOnce();
        }
    }
}