﻿using Google.Protobuf;
using Google.Protobuf.Reflection;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SynthesisServer.Client
{
    public class UDPHandlerHost : IUDPHandler
    {
        public DHParameters Parameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public UdpClient UDPClient { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        MessageDescriptor IUDPHandler.Descriptor { get; set; }

        public void HandleDisconnect()
        {
            throw new NotImplementedException();
        }

        public void HandleGameData()
        {
            throw new NotImplementedException();
        }

        public void HandleKeyExchange()
        {
            throw new NotImplementedException();
        }

        public void ReceiveUpdate()
        {
            throw new NotImplementedException();
        }

        public void SendUpdate(IMessage Update)
        {
            throw new NotImplementedException();
        }

        public void Start(UdpClient client, int port, long timeoutMS)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
