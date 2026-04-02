using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
    public const float Speed = 300.0f;
    private NavigationAgent2D navAgent;

    // Ссылка на игрока, задаётся при спавне
    public Node2D Player;

    public override void _Ready()
    {
        navAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        
        // Настройки для лучшего обхода препятствий
        navAgent.Radius = 10.0f;                    // радиус агента
        navAgent.PathDesiredDistance = 4.0f;        // насколько близко к пути нужно идти
        navAgent.TargetDesiredDistance = 8.0f;      // когда считать цель достигнута
        navAgent.MaxSpeed = Speed;                  // максимальная скорость в навигации
        navAgent.AvoidanceEnabled = true;           // включаем RVO для обхода

        Player = MainCharacter.Instance;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Player == null) return;
		
        Vector2 playerPosition = Player.GlobalPosition;
        navAgent.TargetPosition = playerPosition;
        navAgent.NavigationLayers = 1; // слой навигации должен совпадать с Navigation2D

        if (!navAgent.IsNavigationFinished())
        {
            Vector2 nextPos = navAgent.GetNextPathPosition();
            Vector2 dir = (nextPos - GlobalPosition).Normalized();
            Velocity = dir * Speed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
    }
}