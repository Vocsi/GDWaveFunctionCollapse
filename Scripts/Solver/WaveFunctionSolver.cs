using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
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
		private bool CurrentlyGenerating;
		private GodotThread GenerateThread;
		
		public override void _Ready()
		{
			SetupGrid();
		}

		private void SetupGrid()
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
				CurrentlyGenerating = true;
				GenerateThread = new GodotThread();
				Callable callable = new Callable( this, "Solve" );
				Error error = GenerateThread.Start( callable );
				if ( error != Error.Ok )
					GD.PrintErr( error );
			}

			if ( Input.IsActionPressed( "DeleteGrid" ) && !CurrentlyGenerating && !CanGenerate )
			{
				Array< Node > children = GetChildren();
				foreach ( Node child in children )
					child.QueueFree();
				SetupGrid();
			}
		}

		public void ResetIsGenerating()
		{
			CurrentlyGenerating = false;
			Seed = 0;
			CollapsedNodes = 0;
		}
		
		private void Solve()
		{
			ulong startTime = Time.GetTicksMsec();
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

			ulong elapsedTime = Time.GetTicksMsec() - startTime;
			GD.Print( $"Completed in: { elapsedTime } ms" );
			CallDeferred( "ResetIsGenerating" );
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
			
			CallDeferred( "SpawnSprite", new Vector2(x * 14, y * 14 ), superPosition.Entropy[ 0 ] );
			
			Propagate( x, y );
			
			return true;
		}

		public void SpawnSprite( Vector2 position, WFCNode node )
		{
			NodeInstance nodeInstance = new NodeInstance();
			nodeInstance.Texture = node.Sprite;
			nodeInstance.Position = position;
			nodeInstance.RotationDegrees = -node.Rotation;
			nodeInstance.Connectors = node.Connectors;
			nodeInstance.Visible = true;
			AddChild( nodeInstance );
		}
		
		private void Propagate( int x, int y )
		{
			SuperPosition collapsedNode = Grid[ x, y ];
			for ( int i = -1; i <= 1; i++ )
			{
				for ( int j = -1; j <= 1; j++ )
				{
					if ( x + i >= GridSize.X || x + i < 0 || y + j >= GridSize.Y || y + j < 0 || Mathf.Abs( i ) == Mathf.Abs( j ) )
						continue;
						
					if ( CheckAdjacent( collapsedNode, Grid[ x + i, y + j ], i, j, x, y ) )
						Propagate( x + i, y + j );
				}
			}
			
		}

		private bool CheckAdjacent( SuperPosition current, SuperPosition adjacent, int x, int y, int gridOriginX, int gridOriginY )
		{
			if ( adjacent.Collapsed )
				return false;

			int check = 0;
			int refCheck = 0;
			if (x == -1)
			{
				check = 1;
				refCheck = 3;
			}
			else if (x == 1)
			{
				check = 3;
				refCheck = 1;
			}
			else if ( y == -1 )
			{
				check = 2;
				refCheck = 0;
			}
			else if (y == 1)
			{
				check = 0;
				refCheck = 2;
			}

			//for ( int i = adjacent.Entropy.Count - 1; i >= 0; i-- )
			//	if (current.Entropy[0].Connectors[refCheck] != adjacent.Entropy[i].GetReversedConnector(check))
			//		adjacent.Entropy.RemoveAt( i );
			//
			//if ( adjacent.Entropy.Count <= 0 )
			//	return true;
			//return false;
			
			bool altered = false;
			for ( int i = adjacent.Entropy.Count - 1; i >= 0; i-- )
			{
				bool matchFound = false;
				foreach ( var node in current.Entropy )
				{
					if ( node.Connectors[ refCheck ] == adjacent.Entropy[ i ].GetReversedConnector( check ) )
					{
						matchFound = true;
						break;
					}
				}
			
				if ( matchFound )
					continue;
				
				adjacent.Entropy.RemoveAt( i );
				altered = true;
			}
			
			return altered;
		}
	}
}
