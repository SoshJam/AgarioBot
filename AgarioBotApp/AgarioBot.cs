using Agario;
using Communications;
using Microsoft.Extensions.Logging.Abstractions;
using System.Numerics;
using System.Text.Json;

public class AgarioBot
{
    private Networking connection;

    private bool gameActive = false;
    private Dictionary<long, Player> players;
    private Dictionary<long, Food> foods;

    private long playerID;
    private Player player;

    private string Name;

    private Action<string> Report;

    private const int movementSpeed = 1000;

    public AgarioBot(string Name, Action<string> ReportCallback)
    {
        this.players = new Dictionary<long, Player>();
        this.foods = new Dictionary<long, Food>();

        this.player = new Player(0, 0, 0, 0, "null");

        this.Name = Name;
        this.connection = new Networking(NullLogger.Instance, OnConnect, OnDisconnect, OnMessage, '\n');

        this.Report = ReportCallback;
    }

    public void Connect(string address, int port)
    {
        try
        {
            this.connection.Connect(address, port);
        }
        catch (Exception ex)
        {
            Report(ex.Message);
        }
    }

    private void OnConnect(Networking channel)
    {
        channel.AwaitMessagesAsync();
        channel.Send(String.Format(Protocols.CMD_Start_Game, this.Name));

        /*System.Timers.Timer timer = new System.Timers.Timer(100);
        timer.Elapsed += (o, e) => Update();
        timer.Start();*/
    }

    private void OnDisconnect(Networking channel)
    {
        // do nothing?
    }

    private void OnMessage(Networking channel, string message)
    {
        try
        {

            if (message.StartsWith(Protocols.CMD_HeartBeat))
            {
                string parameterString = message.Substring(Protocols.CMD_HeartBeat.Length);
                Update();
            }
            else if (message.StartsWith(Protocols.CMD_Update_Players))
            {
                string parameterString = message.Substring(Protocols.CMD_Update_Players.Length);
                List<Player> newPlayers = JsonSerializer.Deserialize<List<Player>>(parameterString) ?? new List<Player>();

                lock (players)
                {
                    foreach (Player p in newPlayers)
                    {
                        this.players[p.ID] = p;
                        if (this.gameActive && p.ID == this.playerID)
                            this.player = p;
                    }
                }
            }
            else if (message.StartsWith(Protocols.CMD_Food))
            {
                string parameterString = message.Substring(Protocols.CMD_Food.Length);
                List<Food> newFood = JsonSerializer.Deserialize<List<Food>>(parameterString) ?? new List<Food>();

                lock (foods)
                {
                    foreach (Food f in newFood)
                    {
                        this.foods[f.ID] = f;
                    }
                }
            }
            else if (message.StartsWith(Protocols.CMD_Dead_Players))
            {
                string parameterString = message.Substring(Protocols.CMD_Dead_Players.Length);
                List<long> deadPlayers = JsonSerializer.Deserialize<List<long>>(parameterString) ?? new List<long>();

                lock (players)
                {
                    foreach (long ID in deadPlayers)
                    {
                        this.players.Remove(ID);
                        if (ID == playerID) gameActive = false;
                    }
                }
            }
            else if (message.StartsWith(Protocols.CMD_Eaten_Food))
            {
                string parameterString = message.Substring(Protocols.CMD_Eaten_Food.Length);
                List<long> eatenFood = JsonSerializer.Deserialize<List<long>>(parameterString) ?? new List<long>();

                lock (foods)
                {
                    foreach (long ID in eatenFood)
                    {
                        this.foods.Remove(ID);
                    }
                }
            }
            else if (message.StartsWith(Protocols.CMD_Player_Object))
            {
                string parameterString = message.Substring(Protocols.CMD_Player_Object.Length);
                if (!int.TryParse(parameterString, out int ID))
                    throw new Exception("Could not parse ID.");

                this.playerID = ID;
                gameActive = true;
            }
        }
        catch (Exception ex)
        {
            Report($"==========\nError in message: {message}\n{ex.Message}\n==========");
        }
    }

    /// <summary>
    /// Called every time the server sends out a Heartbeat
    /// </summary>
    private void Update()
    {
        if (!gameActive) return;

        Vector2 targetPosition = player.Position;

        string state = "Idle";

        // Go to the closest food
        Food? closestFood = null;
        float closestFoodDistance = 9999f;
        lock (foods)
        {
            foreach (Food f in this.foods.Values)
            {
                float distance = (player.Position - f.Position).Length();

                if (distance < closestFoodDistance)
                {
                    closestFoodDistance = distance;
                    closestFood = f;
                }
            }

            if (closestFood is not null)
            {
                state = "Chasing Food";
                targetPosition = closestFood.Position;
            }
        }

        // If there's a player nearby, chase it down instead
        lock (players)
        {
            Player? closestPlayer = null;
            float closestPlayerDistance = 9999f;
            foreach (Player p in this.players.Values)
            {
                // if it is us or one of our offspring
                if (p.ID == player.ID || p.Name == player.Name) continue;
                float distance = (player.Position - p.Position).Length();
                if (distance < closestPlayerDistance)
                {
                    closestPlayerDistance = distance;
                    closestPlayer = p;
                }
            }

            if (closestPlayer is not null && closestPlayerDistance < player.Mass / 2)
            {
                if (closestPlayer.Mass < player.Mass * 0.65)
                {
                    state = "Chasing Player";
                    targetPosition = closestPlayer.Position;
                }
            }
        }

        // Calculate the actual target position
        Vector2 distanceVector = targetPosition - player.Position;
        Vector2 normalizedDistanceVector = distanceVector / distanceVector.Length();
        Vector2 movementVector = normalizedDistanceVector * (player.Mass > 300 ? movementSpeed : movementSpeed / 10f);

        Vector2 adjustedTargetPosition = player.Position + movementVector;

        this.connection.Send(String.Format(Protocols.CMD_Move, (int)adjustedTargetPosition.X, (int)adjustedTargetPosition.Y));

        Report($"{Name} - {playerID} - {(int)player.Position.X},{(int)player.Position.Y} - {(int)player.Mass} - {state}");
    }
}
