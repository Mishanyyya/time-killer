using Godot;
using System;

public partial class Camera2d : Camera2D
{
	[Export] public NodePath TargetPath = "..";
	[Export] public float MouseInfluence = 0.25f;
	[Export] public float MaxMouseOffset = 120f;
	[Export] public float OffsetSmoothSpeed = 8f;
	[Export] public bool ZoomByTargetSize = true;
	[Export] public float ZoomReferenceScale = 1.0f;
	[Export] public float ZoomSizeExponent = -0.5f;
	[Export] public float MinZoomFactor = 0.75f;
	[Export] public float MaxZoomFactor = 2.0f;
	[Export] public float ZoomSmoothSpeed = 6f;
	[Export] public bool ZoomOutBySpeed = true;
	[Export] public float MaxSpeedZoomOut = 0.03f;
	[Export] public float SpeedForMaxZoomOut = 900f;
	[Export] public float SpeedZoomSmoothSpeed = 12f;

	private Node2D target;
	private Vector2 baseZoom = Vector2.One;
	private float smoothedSpeedZoomOut = 0f;

	public override void _Ready()
	{
		target = GetNodeOrNull<Node2D>(TargetPath) ?? GetParentOrNull<Node2D>();
		TopLevel = true;
		Enabled = true;
		baseZoom = Zoom;

		if (target != null)
			GlobalPosition = target.GlobalPosition;
	}

	public override void _Process(double delta)
	{
		if (target == null)
			return;

		GlobalPosition = target.GlobalPosition;

		Vector2 toMouse = GetGlobalMousePosition() - target.GlobalPosition;
		Vector2 desiredOffset = toMouse * MouseInfluence;
		desiredOffset = desiredOffset.LimitLength(MaxMouseOffset);

		float t = 1.0f - Mathf.Exp(-OffsetSmoothSpeed * (float)delta);
		Offset = Offset.Lerp(desiredOffset, t);

		float sizeZoomFactor = 1.0f;
		if (ZoomByTargetSize)
		{
			float sizeX = target.Transform.X.Length();
			float sizeY = target.Transform.Y.Length();
			float avgSize = (sizeX + sizeY) * 0.5f;
			float refSize = ZoomReferenceScale <= 0.0f ? 1.0f : ZoomReferenceScale;
			float sizeFactor = avgSize / refSize;

			sizeZoomFactor = Mathf.Pow(Mathf.Max(sizeFactor, 0.001f), ZoomSizeExponent);
			sizeZoomFactor = Mathf.Clamp(sizeZoomFactor, MinZoomFactor, MaxZoomFactor);
		}

		float targetSpeedZoomOut = 0f;
		if (ZoomOutBySpeed && target is CharacterBody2D body && SpeedForMaxZoomOut > 0f)
		{
			float speedRatio = Mathf.Clamp(body.Velocity.Length() / SpeedForMaxZoomOut, 0f, 1f);
			targetSpeedZoomOut = speedRatio * MaxSpeedZoomOut;
		}

		float speedT = 1.0f - Mathf.Exp(-SpeedZoomSmoothSpeed * (float)delta);
		smoothedSpeedZoomOut = Mathf.Lerp(smoothedSpeedZoomOut, targetSpeedZoomOut, speedT);

		float speedZoomFactor = Mathf.Max(0.01f, 1.0f - smoothedSpeedZoomOut);
		Vector2 desiredZoom = baseZoom * sizeZoomFactor * speedZoomFactor;
		float zoomT = 1.0f - Mathf.Exp(-ZoomSmoothSpeed * (float)delta);
		Zoom = Zoom.Lerp(desiredZoom, zoomT);
	}
}
