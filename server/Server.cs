﻿using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SynthesisServer.Utilities;

namespace SynthesisServer
{
    public sealed class Server
    {
        private const int LOBBY_SIZE = 6;
        private const int BUFFER_SIZE = 4096;

        private Socket _tcpSocket;
        private UdpClient _udpSocket;
        private SymmetricEncryptor _encryptor;

        //private Dictionary<string, ClientData> _clients;
        private List<ClientData> _clients;
        private Dictionary<string, Lobby> _lobbies; // Uses lobby name as key

        // These lock the collections, not the individual objects within
        private ReaderWriterLockSlim _lobbiesLock;
        private ReaderWriterLockSlim _clientsLock;

        private ILogger _logger;
        private bool _isRunning = false;

        private int _tcpPort;
        private int _udpPort;

        private static readonly Lazy<Server> lazy = new Lazy<Server>(() => new Server());
        public static Server Instance { get { return lazy.Value; } }
        private Server() 
        {
        }

        /*
        private struct ClientState {
            public byte[] buffer;
            public Socket socket;
            public string id;
        }
        */

        public void Stop()
        {
            _udpSocket.Close();
            _tcpSocket.Close();
            _clientsLock.EnterWriteLock();
            _clients.Clear();
            _clientsLock.ExitWriteLock();
            _lobbiesLock.EnterWriteLock();
            _lobbies.Clear();
            _lobbiesLock.ExitWriteLock();
        }

        public void Start(ILogger logger, int tcpPort = 18001, int udpPort = 18000)
        {

            _tcpPort = tcpPort;
            _udpPort = udpPort;

            _logger = logger;
            _isRunning = true;

            _tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _udpSocket = new UdpClient(new IPEndPoint(IPAddress.Any, _udpPort));

            _encryptor = new SymmetricEncryptor();

            //_clients = new Dictionary<string, ClientData>();

            _clientsLock = new ReaderWriterLockSlim();

            _lobbies = new Dictionary<string, Lobby>();
            _lobbiesLock = new ReaderWriterLockSlim();

            _tcpSocket.Bind(new IPEndPoint(IPAddress.Any, _tcpPort));
            _tcpSocket.Listen(5);
            _tcpSocket.BeginAccept(new AsyncCallback(TCPAcceptCallback), null);

            _udpSocket.BeginReceive(new AsyncCallback(UDPReceiveCallback), null);

            _logger.LogInformation("Server is running");

            Task.Run(() =>
            {
                while (_isRunning)
                {
                    CheckHeartbeats(5000);
                    Thread.Sleep(500);
                }
            });

            Task.Run(() =>
            {
            });
        }

        // TCP Callbacks
        private void TCPAcceptCallback(IAsyncResult asyncResult)
        {
            _logger.LogInformation("New client accepted");
            ClientData client = new ClientData(_tcpSocket.EndAccept(asyncResult), BUFFER_SIZE);

            if (_isRunning)
            {
                client.ClientSocket.BeginReceive(client.SocketBuffer, 0, BUFFER_SIZE, SocketFlags.None, new AsyncCallback(TCPReceiveCallback), client);
                _tcpSocket.BeginAccept(new AsyncCallback(TCPAcceptCallback), null);
            }
        }
        /*
            Data is send in this format: [4 byte int denoting header length] + [MessageHeader object] + [4 byte int denoting message body length] + [The actual message]
        */
        // HAVE EACH CLIENT HAVE ITS OWN READ WRITE LOCK AND ACCESS THINGS THROUGH THE LIST LESS. INSTEAD JUST PASS CLIENT INTO HANDLERS
        private void TCPReceiveCallback(IAsyncResult asyncResult)
        {
            _logger.LogInformation("Received Message");
            try
            {
                //ClientState state = (ClientState)asyncResult.AsyncState;
                ClientData client = (ClientData)asyncResult.AsyncState;
                client.ClientLock.ExitWriteLock();

                client.ClientLock.EnterReadLock();
                int received = client.ClientSocket.EndReceive(asyncResult);
                byte[] data = new byte[received];
                Array.Copy(client.SocketBuffer, data, received);
                if (BitConverter.IsLittleEndian) { Array.Reverse(data); }

                MessageHeader header = MessageHeader.Parser.ParseFrom(IO.GetNextMessage(ref data));
                Any message;
                if (header.IsEncrypted && client.SymmetricKey != null)
                {
                    //byte[] decryptedData = _encryptor.Decrypt(data, _clients[header.ClientId].SymmetricKey);
                    byte[] decryptedData = _encryptor.Decrypt(data, client.SymmetricKey);
                    message = Any.Parser.ParseFrom(IO.GetNextMessage(ref decryptedData));
                }
                else if (!header.IsEncrypted)
                {
                    message = Any.Parser.ParseFrom(IO.GetNextMessage(ref data));
                }
                else
                {
                    message = Any.Pack(new StatusMessage()
                    {
                        LogLevel = StatusMessage.Types.LogLevel.Error,
                        Msg = "Invalid Message body"
                    });
                }
                client.ClientLock.ExitReadLock();

                client.ClientLock.EnterUpgradeableReadLock();
                if (message.Is(KeyExchange.Descriptor)) { HandleKeyExchange(message.Unpack<KeyExchange>(), client); }
                else if (message.Is(Heartbeat.Descriptor)) { HandleHeartbeat(message.Unpack<Heartbeat>(), client); }
                else if (message.Is(ServerInfoRequest.Descriptor)) { HandleServerInfoRequest(message.Unpack<ServerInfoRequest>(), client); }
                else if (message.Is(CreateLobbyRequest.Descriptor)) { HandleCreateLobbyRequest(message.Unpack<CreateLobbyRequest>(), client); }
                else if (message.Is(DeleteLobbyRequest.Descriptor)) { HandleDeleteLobbyRequest(message.Unpack<DeleteLobbyRequest>(), client); }
                else if (message.Is(JoinLobbyRequest.Descriptor)) { HandleJoinLobbyRequest(message.Unpack<JoinLobbyRequest>(),client); }
                else if (message.Is(LeaveLobbyRequest.Descriptor)) { HandleLeaveLobbyRequest(message.Unpack<LeaveLobbyRequest>(), client); }
                else if (message.Is(StartLobbyRequest.Descriptor)) { HandleStartLobbyRequest(message.Unpack<StartLobbyRequest>(), client); }
                else if (message.Is(SwapRequest.Descriptor)) { HandleSwapRequest(message.Unpack<SwapRequest>(), client); }
                else if (message.Is(StatusMessage.Descriptor)) { HandleStatus(message.Unpack<StatusMessage>(), client); }
                client.ClientLock.ExitUpgradeableReadLock();

                if (_isRunning)
                {
                    client.ClientSocket.BeginReceive(client.SocketBuffer, 0, BUFFER_SIZE, SocketFlags.None, new AsyncCallback(TCPReceiveCallback), client);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }
        private void TCPSendCallback(IAsyncResult asyncResult)
        {

        }


        // UDP Callbacks
        private void UDPReceiveCallback(IAsyncResult asyncResult)
        {
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = _udpSocket.EndReceive(asyncResult, ref remoteEP);
            if (BitConverter.IsLittleEndian) { Array.Reverse(buffer); } // make sure to do while sending

            MessageHeader header = MessageHeader.Parser.ParseFrom(IO.GetNextMessage(ref buffer));

            // TODO: make clients accessible through id not socket (socket can just be a property of the client) (Still check to make sure correct socket is being used for client)

            Any message;

            _clientsLock.EnterReadLock();
            ClientData client = null;
            foreach (var x in _clients)
            {
                x.ClientLock.EnterReadLock();
                if (x.ID.Equals(header.ClientId))
                {
                    client = x;
                }
                x.ClientLock.ExitReadLock();
            }
            _clientsLock.ExitReadLock();

            if (header.IsEncrypted && client != null)
            {
                client.ClientLock.EnterReadLock();
                byte[] decryptedData = _encryptor.Decrypt(buffer, client.SymmetricKey);
                client.ClientLock.ExitReadLock();
                message = Any.Parser.ParseFrom(IO.GetNextMessage(ref decryptedData));
            }
            else
            {
                message = Any.Pack(new StatusMessage()
                {
                    LogLevel = StatusMessage.Types.LogLevel.Error,
                    Msg = "Invalid Message"
                });
            }

            if (message.Is(MatchStartResponse.Descriptor)) { HandleMatchStartResponse(message.Unpack<MatchStartResponse>(), client, remoteEP); }
            if (_isRunning)
            {
                _udpSocket.BeginReceive(new AsyncCallback(UDPReceiveCallback), null);
            }

        }
        private void UDPSendCallback(IAsyncResult asyncResult)
        {

        }


        // UDP Handlers
        private void HandleMatchStartResponse(MatchStartResponse matchStartResponse, ClientData client, IPEndPoint remoteEP)
        {
            _logger.LogInformation(client.ID + " is starting a match");
            client.UDPEndPoint = remoteEP;

            _clientsLock.EnterWriteLock();
            _clients.Remove(client);
            _clientsLock.ExitWriteLock();
        }


        // TCP Handlers
        private void HandleKeyExchange(KeyExchange keyExchange, ClientData client)
        {
            _logger.LogInformation(client.ID + " is performing a key exchange");

            client.ClientLock.EnterWriteLock();
            client.GenerateSharedSecret(keyExchange.PublicKey, new DHParameters(new BigInteger(keyExchange.P), new BigInteger(keyExchange.G)), _encryptor);
            client.ClientLock.EnterReadLock();

            _clientsLock.EnterReadLock();
            IO.SendMessage
            (
                new KeyExchange()
                {
                    ClientId = client.ID,
                    PublicKey = client.GetPublicKey()
                },
                client.ClientSocket,
                new AsyncCallback(TCPSendCallback)
            );
            _clientsLock.ExitReadLock();
        }
        private void HandleHeartbeat(Heartbeat heartbeat, ClientData client)
        {
            _logger.LogInformation(client.ID + " has sent a heartbeat");
            client.UpdateHeartbeat();
        }
        private void HandleServerInfoRequest(ServerInfoRequest serverInfoRequest, ClientData client)
        {
            ServerInfoResponse response = new ServerInfoResponse()
            {
                CurrentLobby = client.CurrentLobby,
                CurrentName = client.Name
            };
            _lobbiesLock.EnterReadLock();
            foreach (string lobby in _lobbies.Keys)
            {
                var lobbyData = new ServerInfoResponse.Types.Lobby
                {
                    LobbyName = lobby
                };
                _lobbies[lobby].LobbyLock.EnterReadLock();
                List<ClientData> lobbyClients = _lobbies[lobby].Clients;
                _lobbies[lobby].LobbyLock.ExitReadLock();
                foreach (var lobbyClient in lobbyClients)
                {
                    lobbyData.Clients.Add(lobbyClient.Name);
                }
                response.Lobbies.Add(lobbyData);
            }
            IO.SendEncryptedMessage(response, client.ID, client.SymmetricKey, client.ClientSocket, _encryptor, new AsyncCallback(TCPSendCallback));
        }
        private void HandleCreateLobbyRequest(CreateLobbyRequest createLobbyRequest, ClientData client)
        {
            _logger.LogInformation(client.ID + " has requested to create a lobby");
            _lobbiesLock.EnterUpgradeableReadLock();

            if (_lobbies.ContainsKey(createLobbyRequest.LobbyName))
            {
                IO.SendEncryptedMessage
                (
                    new CreateLobbyResponse()
                    {
                        LobbyName = createLobbyRequest.LobbyName,
                        GenericResponse = new GenericResponse()
                        {
                            Success = false,
                            LogMessage = "That lobby already exists"
                        }
                    },
                    client.ID,
                    client.SymmetricKey,
                    client.ClientSocket,
                    _encryptor,
                    new AsyncCallback(TCPSendCallback)
                );
            }
            else
            {
                _lobbiesLock.EnterWriteLock();
                _lobbies[createLobbyRequest.LobbyName] = new Lobby(client, LOBBY_SIZE);
                _lobbiesLock.ExitWriteLock();

                client.ClientLock.EnterWriteLock();
                client.CurrentLobby = createLobbyRequest.LobbyName;
                client.ClientLock.ExitWriteLock();

                IO.SendEncryptedMessage
                (
                    new CreateLobbyResponse()
                    {
                        LobbyName = createLobbyRequest.LobbyName,
                        GenericResponse = new GenericResponse()
                        {
                            Success = true,
                            LogMessage = "Lobby created successfully"
                        }
                    },
                    client.ID,
                    client.SymmetricKey,
                    client.ClientSocket,
                    _encryptor,
                    new AsyncCallback(TCPSendCallback)
                );
            }

            _lobbiesLock.ExitUpgradeableReadLock();
        }
        private void HandleDeleteLobbyRequest(DeleteLobbyRequest deleteLobbyRequest, ClientData client)
        {
            _logger.LogInformation(client.ID + " has requested to delete a lobby");
            _lobbiesLock.EnterUpgradeableReadLock();

            if (_lobbies.ContainsKey(deleteLobbyRequest.LobbyName) && _lobbies[deleteLobbyRequest.LobbyName].Host.Equals(client))
            {
                _lobbiesLock.EnterReadLock();
                foreach (ClientData x in _lobbies[deleteLobbyRequest.LobbyName].Clients)
                {
                    if (x != null)
                    {
                        x.ClientLock.EnterWriteLock();
                        x.CurrentLobby = null;
                        x.ClientLock.ExitWriteLock();
                    }
                }
                _lobbiesLock.ExitReadLock();

                _lobbiesLock.EnterWriteLock();
                _lobbies.Remove(deleteLobbyRequest.LobbyName);
                _lobbiesLock.ExitWriteLock();

                IO.SendEncryptedMessage
                (
                    new DeleteLobbyResponse()
                    {
                        LobbyName = deleteLobbyRequest.LobbyName,
                        GenericResponse = new GenericResponse()
                        {
                            Success = true,
                            LogMessage = "Lobby successfully deleted"
                        }
                    },
                    client.ID,
                    client.SymmetricKey,
                    client.ClientSocket,
                    _encryptor,
                    new AsyncCallback(TCPSendCallback)
                );
            }
            else
            {
                IO.SendEncryptedMessage
                (
                    new CreateLobbyResponse()
                    {
                        LobbyName = deleteLobbyRequest.LobbyName,
                        GenericResponse = new GenericResponse()
                        {
                            Success = false,
                            LogMessage = "Could not delete lobby"
                        }
                    },
                    client.ID,
                    client.SymmetricKey,
                    client.ClientSocket,
                    _encryptor,
                    new AsyncCallback(TCPSendCallback)
                );
            }

            _lobbiesLock.ExitUpgradeableReadLock();
        }
        private void HandleJoinLobbyRequest(JoinLobbyRequest joinLobbyRequest, ClientData client)
        {
            _logger.LogInformation(client.ID + " has requested to join a lobby");
            _lobbiesLock.EnterReadLock();
            bool lobbyExists = _lobbies.TryGetValue(joinLobbyRequest.LobbyName, out Lobby lobby);
            

            if (lobbyExists && client.CurrentLobby == null)
            {
                lobby.LobbyLock.EnterWriteLock();
                bool success = lobby.TryAddClient(client);
                lobby.LobbyLock.ExitWriteLock();
                if (success)
                {
                    client.ClientLock.EnterWriteLock();
                    client.CurrentLobby = joinLobbyRequest.LobbyName;
                    client.ClientLock.ExitWriteLock();

                    IO.SendEncryptedMessage
                    (
                        new CreateLobbyResponse()
                        {
                            LobbyName = joinLobbyRequest.LobbyName,
                            GenericResponse = new GenericResponse()
                            {
                                Success = true,
                                LogMessage = "Joined lobby successfully"
                            }
                        },
                        client.ID,
                        client.SymmetricKey,
                        client.ClientSocket,
                        _encryptor,
                        new AsyncCallback(TCPSendCallback)
                    );
                }
                else 
                {
                    IO.SendEncryptedMessage
                    (
                        new CreateLobbyResponse()
                        {
                            LobbyName = joinLobbyRequest.LobbyName,
                            GenericResponse = new GenericResponse()
                            {
                                Success = false,
                                LogMessage = "Could not join lobby"
                            }
                        },
                        client.ID,
                        client.SymmetricKey,
                        client.ClientSocket,
                        _encryptor,
                        new AsyncCallback(TCPSendCallback)
                    );
                }
            }
            else
            {
                IO.SendEncryptedMessage
                (
                    new CreateLobbyResponse()
                    {
                        LobbyName = joinLobbyRequest.LobbyName,
                        GenericResponse = new GenericResponse()
                        {
                            Success = false,
                            LogMessage = "Could not join lobby"
                        }
                    },
                    client.ID,
                    client.SymmetricKey,
                    client.ClientSocket,
                    _encryptor,
                    new AsyncCallback(TCPSendCallback)
                );
            }
            _lobbiesLock.ExitReadLock();
        }
        private void HandleLeaveLobbyRequest(LeaveLobbyRequest leaveLobbyRequest, ClientData client)
        {
            _logger.LogInformation(client.ID + " has requested to leave a lobby");
            _lobbiesLock.EnterReadLock();
            bool lobbyExists = _lobbies.TryGetValue(leaveLobbyRequest.LobbyName, out Lobby lobby);
            if (lobbyExists && client.CurrentLobby == null)
            {
                lobby.LobbyLock.EnterWriteLock();
                bool success = lobby.TryRemoveClient(client);
                lobby.LobbyLock.ExitWriteLock();
                if (success)
                {
                    client.ClientLock.EnterWriteLock();
                    client.CurrentLobby = null;
                    client.ClientLock.ExitWriteLock();

                    IO.SendEncryptedMessage
                    (
                        new CreateLobbyResponse()
                        {
                            LobbyName = leaveLobbyRequest.LobbyName,
                            GenericResponse = new GenericResponse()
                            {
                                Success = true,
                                LogMessage = "Left lobby successfully"
                            }
                        },
                        client.ID,
                        client.SymmetricKey,
                        client.ClientSocket,
                        _encryptor,
                        new AsyncCallback(TCPSendCallback)
                    );
                }
                else
                {
                    IO.SendEncryptedMessage
                    (
                        new CreateLobbyResponse()
                        {
                            LobbyName = leaveLobbyRequest.LobbyName,
                            GenericResponse = new GenericResponse()
                            {
                                Success = false,
                                LogMessage = "Could not leave lobby"
                            }
                        },
                        client.ID,
                        client.SymmetricKey,
                        client.ClientSocket,
                        _encryptor,
                        new AsyncCallback(TCPSendCallback)
                    );
                }
            }
            else
            {
                IO.SendEncryptedMessage
                (
                    new CreateLobbyResponse()
                    {
                        LobbyName = leaveLobbyRequest.LobbyName,
                        GenericResponse = new GenericResponse()
                        {
                            Success = false,
                            LogMessage = "Could not leave lobby"
                        }
                    },
                    client.ID,
                    client.SymmetricKey,
                    client.ClientSocket,
                    _encryptor,
                    new AsyncCallback(TCPSendCallback)
                );
            }
            _lobbiesLock.ExitReadLock();
        }
        private void HandleStartLobbyRequest(StartLobbyRequest startLobbyRequest, ClientData client)
        {
            _logger.LogInformation(client.ID + " has requested to start a lobby");
            _lobbiesLock.EnterUpgradeableReadLock();
            
            if (_lobbies.TryGetValue(startLobbyRequest.LobbyName, out Lobby lobby))
            {
                lobby.LobbyLock.EnterReadLock();
                bool isHost = lobby.Host.Equals(client);
                lobby.LobbyLock.ExitReadLock();

                if (isHost)
                {
                    _lobbiesLock.EnterWriteLock();
                    _lobbies.Remove(startLobbyRequest.LobbyName);
                    StartLobby(lobby);
                    _lobbiesLock.ExitWriteLock();

                    IO.SendEncryptedMessage
                    (
                        new StartLobbyResponse()
                        {
                            LobbyName = startLobbyRequest.LobbyName,
                            GenericResponse = new GenericResponse()
                            {
                                Success = true,
                                LogMessage = "Lobby successfully started"
                            }
                        },
                        client.ID,
                        client.SymmetricKey,
                        client.ClientSocket,
                        _encryptor,
                        new AsyncCallback(TCPSendCallback)
                    );
                }
                else
                {
                    IO.SendEncryptedMessage
                    (
                        new StartLobbyResponse()
                        {
                            LobbyName = startLobbyRequest.LobbyName,
                            GenericResponse = new GenericResponse()
                            {
                                Success = false,
                                LogMessage = "You do not have permission to start this lobby"
                            }
                        },
                        client.ID,
                        client.SymmetricKey,
                        client.ClientSocket,
                        _encryptor,
                        new AsyncCallback(TCPSendCallback)
                    );
                }
            }
            else
            {
                IO.SendEncryptedMessage
                (
                    new StartLobbyResponse()
                    {
                        LobbyName = startLobbyRequest.LobbyName,
                        GenericResponse = new GenericResponse()
                        {
                            Success = false,
                            LogMessage = "Lobby does not exist"
                        }
                    },
                    client.ID,
                    client.SymmetricKey,
                    client.ClientSocket,
                    _encryptor,
                    new AsyncCallback(TCPSendCallback)
                );
            }
            _lobbiesLock.ExitUpgradeableReadLock();
        }
        private void HandleSwapRequest(SwapRequest swapRequest, ClientData client)
        {
            _logger.LogInformation(client.ID + " has requested to swap positions in a lobby");
            _lobbiesLock.EnterReadLock();
            bool lobbyExists = _lobbies.TryGetValue(swapRequest.LobbyName, out Lobby lobby);
            _lobbiesLock.ExitReadLock();
            if (lobbyExists)
            {
                lobby.LobbyLock.EnterWriteLock();
                bool success = client.Equals(lobby.Host) && lobby.Swap(swapRequest.FirstPostion, swapRequest.SecondPostion);
                lobby.LobbyLock.ExitWriteLock();

                if (success)
                {
                    IO.SendEncryptedMessage
                    (
                        new SwapResponse()
                        {
                            LobbyName = swapRequest.LobbyName,
                            GenericResponse = new GenericResponse()
                            {
                                Success = true,
                                LogMessage = "Swap Successful"
                            }
                        },
                        client.ID,
                        client.SymmetricKey,
                        client.ClientSocket,
                        _encryptor,
                        new AsyncCallback(TCPSendCallback)
                    );
                }
                else
                {
                    IO.SendEncryptedMessage
                    (
                        new SwapResponse()
                        {
                            LobbyName = swapRequest.LobbyName,
                            GenericResponse = new GenericResponse()
                            {
                                Success = false,
                                LogMessage = "Swap failed"
                            }
                        },
                        client.ID,
                        client.SymmetricKey,
                        client.ClientSocket,
                        _encryptor,
                        new AsyncCallback(TCPSendCallback)
                    );
                }
            }
            else
            {
                IO.SendEncryptedMessage
                (
                    new SwapResponse()
                    {
                        LobbyName = swapRequest.LobbyName,
                        GenericResponse = new GenericResponse()
                        {
                            Success = false,
                            LogMessage = "Swap failed"
                        }
                    },
                    client.ID,
                    client.SymmetricKey,
                    client.ClientSocket,
                    _encryptor,
                    new AsyncCallback(TCPSendCallback)
                );
            }
        }
        private void HandleStatus(StatusMessage statusMessage, ClientData client)
        {
            _clientsLock.EnterReadLock();
            foreach (ClientData x in _clients)
            {
                if (x.SymmetricKey != null)
                {
                    IO.SendEncryptedMessage(statusMessage, x.ID, x.SymmetricKey, x.ClientSocket, _encryptor, new AsyncCallback(TCPSendCallback));
                }
            }
            _clientsLock.ExitReadLock();
        }
        

        private void StartLobby(Lobby lobby)
        {
            lobby.LobbyLock.EnterWriteLock(); // Will never be accessed again
            lobby.Start();

            MatchStart startMsg = new MatchStart()
            {
                UdpPort = _udpPort
            };

            _clientsLock.EnterReadLock();
            for (int i = 0; i < lobby.Clients.Count; i++)
            {
                IO.SendEncryptedMessage(startMsg, lobby.Clients[i].ID, lobby.Clients[i].SymmetricKey, lobby.Clients[i].ClientSocket, _encryptor, new AsyncCallback(TCPSendCallback));
            }

            long start = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            while (System.DateTimeOffset.Now.ToUnixTimeMilliseconds() - start <= 5000)
            {
                _clientsLock.EnterReadLock();
                bool allSet = true;
                foreach (ClientData x in lobby.Clients)
                {
                    if (x.UDPEndPoint == null)
                    {
                        allSet = false;
                    }
                }
                if (allSet) 
                {
                    _clientsLock.ExitReadLock();
                    break;
                }
                _clientsLock.ExitReadLock();
                Thread.Sleep(200);
            }

            _clientsLock.EnterReadLock();
            foreach (ClientData client in lobby.Clients)
            {
                if (client.UDPEndPoint == null)
                {
                    lobby.TryRemoveClient(client);
                }
            }
            _clientsLock.ExitReadLock();

            ConnectionDataHost hostMsg = new ConnectionDataHost();
            foreach (var client in lobby.Clients)
            {
                hostMsg.ClientEnpoints.Add(client.UDPEndPoint.ToString());
            }

            ConnectionDataClient clientMsg = new ConnectionDataClient()
            {
                HostEndpoint = lobby.Host.UDPEndPoint.ToString()
            };

            foreach (ClientData client in lobby.Clients)
            {
                if (lobby.Host.Equals(client))
                {
                    IO.SendEncryptedMessage(hostMsg, client.ID, client.SymmetricKey, client.ClientSocket, _encryptor, new AsyncCallback(TCPSendCallback));
                }
                else
                {
                    IO.SendEncryptedMessage(clientMsg, client.ID, client.SymmetricKey, client.ClientSocket, _encryptor, new AsyncCallback(TCPSendCallback));
                }
            }

        }

        public void CheckHeartbeats(long clientTimeout)
        {
            _clientsLock.EnterUpgradeableReadLock();
            for ( int i = _clients.Count - 1; i >= 0; i--)
            {
                _clients[i].ClientLock.EnterReadLock();
                if (System.DateTimeOffset.Now.ToUnixTimeMilliseconds() - _clients[i].LastHeartbeat >= clientTimeout)
                {
                    _clientsLock.EnterWriteLock();
                    _clients[i].ClientSocket.Close();
                    _clients.Remove(_clients[i]);
                    _clientsLock.ExitWriteLock();
                }
            }
            _clientsLock.ExitUpgradeableReadLock();
        }
    }
}
