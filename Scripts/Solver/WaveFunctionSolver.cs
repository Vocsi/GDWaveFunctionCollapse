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
		
		private SuperPosition[] Grid;
		
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
			
			Grid = new SuperPosition[ GridSize.X * GridSize.Y ];
			for ( int i = 0; i < Grid.Length; i++ )
				Grid[ i ] = new SuperPosition( NodeSet.StartingSet, NodeSet.NodeSize ); 
			
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
				int cheapestNode = FindLowestEntropy();
				if ( cheapestNode == -1 )
					break;

				if ( Collapse( cheapestNode ) )
					continue;
				
				GD.PrintErr( "Could not collapse node" );
				break;
				
			} while ( CollapsedNodes < Grid.Length );

			ulong elapsedTime = Time.GetTicksMsec() - startTime;
			GD.Print( $"Completed in: { elapsedTime / 1000.0 } s" );
			CallDeferred( "ResetIsGenerating" );
		}

		private int FindLowestEntropy()
		{
			( int, int ) cheapestNode = ( int.MaxValue, -1 );
			for ( int i = 0; i < Grid.Length; i++ )
				if ( !Grid[ i ].Collapsed )
					if ( cheapestNode.Item1 > Grid[ i ].Entropy.Count )
						cheapestNode = ( Grid[ i ].Entropy.Count, i );
			return cheapestNode.Item2;
		}

		private bool Collapse( int index )
		{
			SuperPosition superPosition = Grid[ index ];
			if ( superPosition.Collapsed || superPosition.Entropy.Count == 0 )
				return false;
			
			superPosition.Entropy = new List< WFCNode >() { superPosition.Entropy[ RngGen.RandiRange( 0, superPosition.Entropy.Count - 1 ) ] };
			superPosition.Collapsed = true;

			CollapsedNodes++;

			CallDeferred("SpawnSprite", IndexToPosition( index, GridSize.X, superPosition.NodeSize ), superPosition.Entropy[ 0 ] );
			
			Propagate( index );
			
			return true;
		}

		private void SpawnSprite( Vector2 position, WFCNode node )
		{
			NodeInstance nodeInstance = new NodeInstance();
			nodeInstance.Texture = node.Sprite;
			nodeInstance.Position = position;
			nodeInstance.RotationDegrees = -node.Rotation;
			nodeInstance.Connectors = node.Connectors;
			nodeInstance.Visible = true;
			AddChild( nodeInstance );
		}

		private Vector2 IndexToPosition( int index, int width, float size )
		{
			return new Vector2( ( index % width ) * size, Mathf.FloorToInt( index / width ) * size );
		}

		private void Propagate( int index )
		{
			int leftIndex = index - 1;
			int rightIndex = index + 1;
			int aboveIndex = index - GridSize.X;
			int underIndex = index + GridSize.X;

			if ( leftIndex >= 0 && leftIndex % GridSize.X != GridSize.X - 1 )
				CheckAdjacent( index, leftIndex, 3, 1 );
			if ( rightIndex <= Grid.Length - 1 && rightIndex % GridSize.X != 0 )
				CheckAdjacent( index, rightIndex, 1, 3 );
			if ( aboveIndex >= 0 )
				CheckAdjacent( index, aboveIndex, 0, 2 );
			if ( underIndex < Grid.Length )
				CheckAdjacent( index, underIndex, 2, 0 );
		}

		private void CheckAdjacent( int currentIndex, int adjacentIndex, int currentCheck, int adjacentCheck )
		{
			SuperPosition current = Grid[ currentIndex ];
			SuperPosition adjacent = Grid[ adjacentIndex ]; 
			if ( adjacent.Collapsed )
				return;

			bool altered = false;
			for ( int i = adjacent.Entropy.Count - 1; i >= 0; i-- )
			{
				bool matchFound = false;
				foreach ( var node in current.Entropy )
				{
					if ( node.Connectors[ currentCheck ] != adjacent.Entropy[ i ].GetReversedConnector( adjacentCheck ) )
						continue;
					matchFound = true;
					break;
				}
			
				if ( matchFound )
					continue;
				
				adjacent.Entropy.RemoveAt( i );
				altered = true;
			}

			if ( altered )
			{
				if ( adjacent.Entropy.Count == 1 )
					Collapse( adjacentIndex );
				else
					Propagate( adjacentIndex );
			}
		}
	}
}
