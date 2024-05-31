using Godot;
using System;

public partial class CameraController : Camera2D
{

	[ Export ] 
	public float Speed = 200;
	
	public override void _Ready()
	{
	}

	public override void _Process( double delta )
	{
		Vector2 dir = new Vector2();
		float deltaTime = ( float )delta;
		
		if ( Input.IsActionPressed( "ui_up" ) ) 
			dir.Y -= 1;
		if ( Input.IsActionPressed( "ui_down" ) ) 
			dir.Y += 1;
		if ( Input.IsActionPressed( "ui_right" ) ) 
			dir.X += 1;
		if ( Input.IsActionPressed( "ui_left" ) ) 
			dir.X -= 1;
			
		Position = Position + dir * new Vector2( Speed * deltaTime, Speed * deltaTime );

		float zoom = 0;
		if ( Input.IsActionPressed( "ZoomIn" ) )
			zoom += 1;
		if ( Input.IsActionPressed( "ZoomOut" ) )
			zoom -= 1;

		const float zoomSpeed = 3;
		float newZoom = Mathf.Clamp( Zoom.X + zoom * ( zoomSpeed * deltaTime ), 0.1f, 10.0f );
		Zoom = new Vector2(newZoom, newZoom);
	}
}
