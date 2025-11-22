namespace OpenGaugeServer
{
    public enum MessageType
    {
        Init,
        ReInit,
        Var,
        Event,
        Unknown
    }

    /// <summary>
    /// A message to send from us to the clients.
    /// </summary>
    public class ServerMessage<TPayload>
    {
        public MessageType Type { get; set; }
        public required TPayload Payload { get; set; }

        public override string ToString()
        {
            return $"ServerMessage type={Type} payload={Payload}";
        }
    }

    public class VarPayload
    {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public required object? Value { get; set; }

        public override string ToString()
        {
            return $"VarPayload Name={Name} Unit={Unit} Value={Value}";
        }
    }

    public class InitPayload
    {
        public required string? VehicleName { get; set; }
        public required VarDef[] Vars { get; set; }
        public required string[] Events { get; set; }

        public override string ToString()
        {
            var VarsList = string.Join(", ", Vars.Select(v => $"{v.Name} ({v.Unit})"));
            var eventsList = string.Join(", ", Events);

            return $"InitPayload:\n" +
                $"  Vehicle: {VehicleName}" +
                $"  Vars: [{VarsList}]\n" +
                $"  Events: [{eventsList}]";
        }
    }

    /// <summary>
    /// A message from a client.
    /// </summary>
    public class ClientMessage<TPayload>
    {
        public MessageType Type { get; set; }
        public required TPayload Payload { get; set; }

        public override string ToString()
        {
            return $"ClientMessage type={Type} payload={Payload!}";
        }
    }

    public class VarDef
    {
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public bool? Debug { get; set; } // if client wants us to print extra debugging stuff
    }
}