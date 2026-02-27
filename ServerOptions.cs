namespace KeyValueStoreServer
{
    /// <summary>
    /// Represents the configuration options for the server, including data directory and port settings.
    /// </summary>
    /// <remarks>The DataDirectory property specifies the location where server data is stored, while the Port
    /// property defines the network port on which the server listens for incoming connections. Ensure that the
    /// specified port is not already in use by another application.</remarks>
    public class ServerOptions
    {
        /// <summary>
        /// Gets the directory path used for storing data files.
        /// </summary>
        /// <remarks>This property is initialized to an empty string. It is intended to be set to a valid
        /// directory path where data files can be stored or retrieved.</remarks>
        public string DataDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Gets the port number used for network communication.
        /// </summary>
        /// <remarks>The default value is set to 655334, which exceeds the maximum valid port number.
        /// Ensure that the port number is within the valid range of 0 to 65535 when configuring network
        /// settings.</remarks>
        public int Port { get; init; } = 655334;
    }
}
