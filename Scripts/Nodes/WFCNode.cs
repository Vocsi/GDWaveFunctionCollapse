using Godot;
using Array = System.Array;

namespace WaveFunctionCollapse.Scripts.Nodes
{
    public partial class WFCNode : Resource
    {
        [ Export ] 
        public Texture2D Sprite { get; set; }

        [ Export ]
        public string[] Connectors { get; set; }
        
        public float Rotation { get; set; }

        public WFCNode() : this( null, new string[] { "", "", "", "" }, 0 ) {}

        public WFCNode( WFCNode other )
        {
            Sprite = other.Sprite;
            Connectors = new[] { "", "", "", "" };
            for ( int i = 0; i < 4; i++ )
                Connectors[ i ] = other.Connectors[ i ];
            Rotation = other.Rotation;
        }
        
        public WFCNode( Texture2D sprite, string[] connectors, float rotation )
        {
            Sprite = sprite;
            Connectors = connectors;
            Rotation = rotation;
        }
        
        public string GetReversedConnector( int index )
        {
            char[] charArray = Connectors[ index ].ToCharArray();
            Array.Reverse( charArray );
            return new string( charArray );
        }
    }
}
