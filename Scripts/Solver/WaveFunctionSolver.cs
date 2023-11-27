using System;
using System.Collections.Generic;
using Godot;
using WaveFunctionCollapse.Scripts.Nodes;

namespace WaveFunctionCollapse.Scripts.Solver
{

	public partial class WaveFunctionSolver : Node2D
	{
		[ Export ]
		public SuperPosition NodeSet { get; set; }

		[ Export ]
		public Vector2I GridSize;
		
		private SuperPosition[,] Grid;

		private int CollapsedNodes;

		private RandomNumberGenerator RngGen;

		[ Export ] 
		public ulong Seed;

		private bool CanGenerate;
		private GodotThread GenerateThread;
		
		public override void _Ready()
		{
			RngGen = new RandomNumberGenerator();
			if ( Seed == 0 )
				Seed = RngGen.Randi();
			RngGen.Seed = Seed;

			Grid = new SuperPosition[ GridSize.X, GridSize.Y ];
			for ( var x = 0; x < GridSize.X; x++ )
			{
				for ( var y = 0; y < GridSize.Y; y++ )
				{
					SuperPosition superPos = new SuperPosition();
					
					superPos.StartingSet = NodeSet.StartingSet;
					superPos.GenerateRotations();
					superPos.Collapsed = false;
					
					Grid[ x, y ] = superPos;
				}
			}
			
			CanGenerate = true;
		}

		public override void _ExitTree()
		{
			if ( GenerateThread != null )
				GenerateThread.WaitToFinish();
		}

		public override void _Process( double delta )
		{
			if ( Input.IsActionPressed( "Generate" ) && CanGenerate )
			{
				CanGenerate = false;
				Solve();
				//GenerateThread = new GodotThread();
				//Callable callable = new Callable( this, "Solve" );
				//Error error = GenerateThread.Start( callable );
				//if ( error != Error.Ok )
				//	GD.PrintErr( error );
			}
		}

		private void Solve()
		{
			do
			{
				Vector2I cheapestNode = FindLowestEntropy();
				if ( cheapestNode.X == -1 )
				{
					GD.PrintErr( "All nodes were already collapsed" );
					break;
				}

				if ( Collapse( cheapestNode.X, cheapestNode.Y ) ) 
					continue;
				
				GD.PrintErr( "Could not collapse node" );
				break;

			} while ( CollapsedNodes < Grid.Length );	
			
			GD.Print( "Completed" );
		}

		private Vector2I FindLowestEntropy()
		{
			( int, int, int ) cheapestNode = ( int.MaxValue, -1, -1 );
			for ( int x = 0; x < GridSize.X; x++ )
			{
				for ( int y = 0; y < GridSize.Y; y++ )
				{
					if ( Grid[ x, y ].Collapsed )
						continue;

					if ( cheapestNode.Item1 >= Grid[ x, y ].StartingSet.Length )
						cheapestNode = ( Grid[ x, y ].StartingSet.Length, x, y );
				}
			}
			
			return new Vector2I( cheapestNode.Item2, cheapestNode.Item3 );
		}

		private bool Collapse( int x, int y )
		{
			SuperPosition superPosition = Grid[ x, y ];
			if ( superPosition.Collapsed || superPosition.Entropy.Count <= 0 )
				return false;
			
			int index = RngGen.RandiRange( 0, superPosition.Entropy.Count - 1 );
			superPosition.Entropy = new List< WFCNode >() { superPosition.Entropy[ index ] };
			superPosition.Collapsed = true;

			CollapsedNodes += 1;
			
			Sprite2D sprite2D = new Sprite2D();
			sprite2D.Texture = superPosition.Entropy[ 0 ].Sprite;
			sprite2D.Position = new Vector2(x * 14, y * 14 );
			sprite2D.RotationDegrees = superPosition.Entropy[ 0 ].Rotation;
			AddChild( sprite2D );
			sprite2D.Visible = true;
			
			Propagate( x, y );
			
			return true;
		}

		private void Propagate( int x, int y )
		{
			SuperPosition collapsedNode = Grid[ x, y ];
			for ( int i = -1; i <= 1; i++ )
			{
				for ( int j = -1; j <= 1; j++ )
				{
					if ( x + i >= GridSize.X || x + i < 0 || y + j >= GridSize.Y || y + j < 0 || ( i == 0 && j == 0 ) )
						continue;
					
					if ( CheckAdjacent( collapsedNode, Grid[ x + i, y + j ] ) )
						Collapse( x + i, y + j );
				}
			}
			
		}

		private bool CheckAdjacent( SuperPosition current, SuperPosition adjacent )
		{
			if ( adjacent.Collapsed )
				return false;
			
			for ( int i = adjacent.Entropy.Count - 1; i >= 0; i-- )
			{
				bool foundConnection = false;
				for ( int index = 0; index < 4; index++ )
				{
					if ( current.Entropy[ 0 ].Connectors[ index ] == adjacent.Entropy[ i ].GetReversedConnector( ( index + 2 ) % 4 ) )
					{
						foundConnection = true;
						break;
					}
				}

				if ( !foundConnection )
					adjacent.Entropy.RemoveAt( i );
			}

			if ( adjacent.Entropy.Count == 0 )
				return true;
			
			return false;
		}
	}
}
