using Godot;
using System;

public partial class WorldGenerator : Node2D
{
    [Export] public TileMapLayer tileMapLayer;
    [Export] public NavigationRegion2D navRegion;
    [Export] public PackedScene enemyScene;
    [Export] public PackedScene playerScene;
    [Export] public Node2D enemyContainer;
    [Export] public int width = 100;
    [Export] public int height = 100;
    [Export] public float noiseFrequency = 0.08f;
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

        if (seed == 0)
            seed = (int)GD.Randi();

        noise = new FastNoiseLite();
        noise.Seed = seed;
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = noiseFrequency;

        GenerateTileMap();
        GenerateNavMesh();
        SpawnPlayer();
        SpawnEnemies();
    }

    private void GenerateTileMap()
    {
        tileMapLayer.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float n = noise.GetNoise2D(x, y);
                bool walkable = n < 0.2f;

                tileMapLayer.SetCell(
                    new Vector2I(x, y),
                    walkable ? walkableSourceId : blockedSourceId,
                    walkable ? walkableAtlasCoords : blockedAtlasCoords
                );
            }
        }
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
        if (playerScene == null)
        {
            return;
        }

        Vector2I spawn = FindEmptyTile();
        player = (Node2D)playerScene.Instantiate();
        player.GlobalPosition = tileMapLayer.ToGlobal(tileMapLayer.MapToLocal(spawn));
        AddChild(player);
    }

    private void SpawnEnemies()
    {
        if (enemyScene == null || enemyContainer == null)
        {
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2I spawn = FindEmptyTile();
            var enemy = (Node2D)enemyScene.Instantiate();
            enemy.GlobalPosition = tileMapLayer.ToGlobal(tileMapLayer.MapToLocal(spawn));
            enemyContainer.AddChild(enemy);
        }
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
            {
                return cell;
            }
        }

        return new Vector2I(width / 2, height / 2);
    }
}