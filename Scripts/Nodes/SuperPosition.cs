using System.Collections.Generic;
using Godot;

namespace WaveFunctionCollapse.Scripts.Nodes
{
    public partial class SuperPosition : Resource
    {
        [ Export ]
        public WFCNode[] StartingSet { get; set; }
        
        public List< WFCNode > Entropy = new List< WFCNode >();

        [ Export ] 
        public bool Collapsed { get; set; }
        
        public void GenerateRotations()
        {
            if ( StartingSet == null )
                return;

            for (int i = 0; i < StartingSet.Length; i++)
            {
                for ( int j = 0; j < 4; j++ )
                {
                    WFCNode newNode = new WFCNode( StartingSet[ i ] );
                    RoatateConnectors( newNode, StartingSet[ i ], j );
                    newNode.Rotation = j * 90;
                    Entropy.Add( newNode );
                }
            }

            //int originalLength = StartingSet.Length;
            //for ( int i = 0; i < originalLength; i++ )
            //{
            //    for ( int j = 0; j < 4; j++ )
            //    {
            //        WFCNode newNode = StartingSet[ i ];
            //        RoatateConnectors( newNode, StartingSet[ i ], j );
            //        newNode.Rotation = j * 90;
            //        Entropy.Add( newNode );
            //    }
            //}
        }

        private void RoatateConnectors( WFCNode rotating, WFCNode original, int times )
        {
            for ( int i = 0; i < 4; i++ )
                rotating.Connectors[ i ] = original.Connectors[ ( i + times ) % 4 ];
        }
        
    }
}
