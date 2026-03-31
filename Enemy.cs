using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	public const float Speed = 300.0f;
	private NavigationAgent2D navAgent; 
	

	public override void _Ready()
	{
		navAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
		navAgent.Radius = 12.0f; // Set the radius for collision avoidance
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 playerPosition = GetNode<MainCharacter>("../MainCharacter").GlobalPosition;
		navAgent.TargetPosition = playerPosition;
		navAgent.NavigationLayers = 1; // Set the navigation layer to match the one used in the Navigation2D node
		
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
