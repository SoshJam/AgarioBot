Console.WriteLine("Server address: (default localhost)");
string address = Console.ReadLine() ?? "localhost";
address = address.Length > 0 ? address : "localhost";
Console.WriteLine($"Connecting to {address}");

AgarioBot bot = new AgarioBot($"AgarioBot", Console.WriteLine);
bot.Connect(address, 11000);

// Wait for user input
Console.Read();