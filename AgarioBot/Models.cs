using System.Numerics;
using System.Text.Json.Serialization;

namespace Agario
{
    public abstract class GameObject
    {
        public long ID { get; set; }

        public Vector2 Position { get; set; }
        public float Mass { get; set; }

        public float X { get { return Position.X; } }
        public float Y { get { return Position.Y; } }

        [JsonConstructor]
        public GameObject(long ID, float X, float Y, float Mass)
        {
            this.ID = ID;
            this.Position = new Vector2(X, Y);
            this.Mass = Mass;
        }
    }

    public class Player : GameObject
    {
        public string Name { get; set; }

        [JsonConstructor]
        public Player(long ID, float X, float Y, float Mass, string Name) : base(ID, X, Y, Mass)
        {
            this.Name = Name;
        }
    }

    public class Food : GameObject
    {
        [JsonConstructor]
        public Food(long ID, float X, float Y, float Mass) : base(ID, X, Y, Mass)
        {

        }
    }
}
