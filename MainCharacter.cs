using Godot;
using System;

public partial class MainCharacter : CharacterBody2D
{
	[Export] public bool ScaleMovementBySize = true;
	[Export] public float ReferenceScale = 1.0f;
    [Export] public float SizeStep = 0.5f;
    [Export] public float MinSize = 0.5f;
    [Export] public float MaxSize = 1.5f;

	[Export] public float BaseMaxSpeed = 620f;

    [Export] public float BaseAcceleration = 1200f;
    [Export] public float BaseDeceleration = 1400f;
    [Export] public float BaseTurnAcceleration = 2000f;

    [Export] public float BaseDashSpeed = 700f;
    [Export] public float DashDuration = 0.12f;
    [Export] public float DashControl = 0.2f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector2 dashDir = Vector2.Zero;

    private float SizeFactor
    {
        get
        {
            if (!ScaleMovementBySize)
                return 1.0f;

            float avgScale = (Mathf.Abs(Scale.X) + Mathf.Abs(Scale.Y)) * 0.5f;
            if (ReferenceScale <= 0.0f)
                return avgScale;

            return avgScale / ReferenceScale;
        }
    }

    private float MaxSpeed => BaseMaxSpeed * SizeFactor;
    private float Acceleration => BaseAcceleration * SizeFactor;
    private float Deceleration => BaseDeceleration * SizeFactor;
    private float TurnAcceleration => BaseTurnAcceleration * SizeFactor;
    private float DashSpeed => BaseDashSpeed * SizeFactor;

    private const string SizeUpAction = "size_up";
    private const string SizeDownAction = "size_down";

	public override void _Ready()
	{
        EnsureResizeActions();
	}

	public override void _PhysicsProcess(double delta)
    {
        float d = (float)delta;

		HandleResizeInput();

        Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        // DASH START
        if (Input.IsActionJustPressed("dash") && !isDashing && input != Vector2.Zero)
        {
            isDashing = true;
            dashTimer = DashDuration;
            dashDir = input.Normalized();
        }

        if (isDashing)
        {
            dashTimer -= d;

            Vector2 dashVelocity = dashDir * DashSpeed;
            Vector2 control = input * MaxSpeed * DashControl;

            Velocity = dashVelocity + control;

            if (dashTimer <= 0f)
                isDashing = false;
        }
        else
        {
            if (input != Vector2.Zero)
            {
                Vector2 target = input * MaxSpeed;

                float accel = Velocity.Dot(input) < 0
                    ? TurnAcceleration
                    : Acceleration;

                Velocity = Velocity.MoveToward(target, accel * d);
            }
            else
            {
                Velocity = Velocity.MoveToward(Vector2.Zero, Deceleration * d);
            }
        }

        MoveAndSlide();
    }

    private void EnsureResizeActions()
    {
        if (!InputMap.HasAction(SizeUpAction))
        {
            InputMap.AddAction(SizeUpAction);
            InputMap.ActionAddEvent(SizeUpAction, new InputEventKey { Keycode = Key.Equal });
        }

        if (!InputMap.HasAction(SizeDownAction))
        {
            InputMap.AddAction(SizeDownAction);
            InputMap.ActionAddEvent(SizeDownAction, new InputEventKey { Keycode = Key.Minus });
        }
    }

    private void HandleResizeInput()
    {
        float avgScale = (Mathf.Abs(Scale.X) + Mathf.Abs(Scale.Y)) * 0.5f;
        float nextScale = avgScale;

        if (Input.IsActionJustPressed(SizeUpAction))
            nextScale += SizeStep;

        if (Input.IsActionJustPressed(SizeDownAction))
            nextScale -= SizeStep;

        nextScale = Mathf.Clamp(nextScale, MinSize, MaxSize);
        Scale = new Vector2(nextScale, nextScale);
    }

	public override void _Process(double delta)
	{
	}
}
