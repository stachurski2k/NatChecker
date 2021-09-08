using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace NatChecker
{
    class NetworkManager
    {
        public static IPEndPoint[] stunServers = new IPEndPoint[]
        {
            new IPEndPoint(GetIpV4("stun.sipgate.net"), 3478),
            new IPEndPoint(GetIpV4("stun.actionvoip.com"), 3478),
            new IPEndPoint(GetIpV4("stun.l.google.com"), 19302),
            new IPEndPoint(GetIpV4("stun3.l.google.com"), 19302),
            new IPEndPoint(GetIpV4("stun.actionvoip.com"), 3478),
        };
        public IPAddress LocalIP;
        public IPAddress PublicIP;
        public NatType TypeOfNat;
        public bool InternetConnection=false;
        List<Thread> activeThreads;
        static IPAddress GetIpV4(string name)
        {
            foreach (IPAddress ip in Dns.GetHostAddresses(name))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return null;
        }
        public void StartInit()
        {
            activeThreads = new List<Thread>();
            Socket mainSocket= new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            mainSocket.Bind(new IPEndPoint(IPAddress.Any, 23000));
            if ((InternetConnection=CheckInternetConnection(mainSocket)))
            {
                LocalIP = GetIpV4(Dns.GetHostName());
                GetPublicIp();
                CheckNatType();
            }
        }
        bool CheckInternetConnection(Socket s)
        {
            StunMessage request = new StunMessage();
            StunMessage response = GetStunResponse(s, request);
            return response != null;
        }
        StunMessage GetStunResponse(Socket s,StunMessage request)
        {
            StunMessage response = null;
            int i=0;
            try
            {
                do
                {
                    response = GetStunResponse(s, request, stunServers[i]);
                    i++;
                } while (i < stunServers.Length && response == null);
            }
            catch
            {
                return null;
            }
            return response;
        }
        StunMessage GetStunResponse(Socket s, StunMessage request, IPEndPoint server)
        {
            try
            {
                byte[] data = request.ToBytes();
                s.SendTo(data, server);
                if (s.Poll(1000000, SelectMode.SelectRead))
                {
                    byte[] received = new byte[1024];
                    s.Receive(received);
                    return StunMessage.Parse(received);
                }
            }
            catch
            {
                return null;
            }
            return null;
        }
        void CheckNatType()
        {
            Thread getNatTypeThread = new Thread(new ThreadStart(() =>
              {
                  TypeOfNat = NatType.Compatible;
                  Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                  s.Bind(new IPEndPoint(IPAddress.Any, 23002));
                  StunMessage response = GetStunResponse(s, new StunMessage(), stunServers[0]);
                  if (response == null)
                  {
                      TypeOfNat = NatType.Uncompatible;
                  }
                  else
                  {
                      IPEndPoint prev;
                      prev = response.PublicEndPoint;
                      for (int i = 1; i < stunServers.Length; i++)
                      {
                          StunMessage msg = GetStunResponse(s, new StunMessage(), stunServers[i]);
                          if (msg == null)
                          {
                              TypeOfNat = NatType.Uncompatible;
                              break;
                          }
                          else
                          {
                              if (prev.ToString() != msg.PublicEndPoint.ToString())
                              {
                                  TypeOfNat = NatType.Uncompatible;
                                  break;
                              }
                          }
                      }
                  }
              }));
            activeThreads.Add(getNatTypeThread);
            getNatTypeThread.Start();
        }
        void GetPublicIp()
        {
            Thread getIpThread = new Thread(new ThreadStart(() =>
              {
                  Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                  s.Bind(new IPEndPoint(IPAddress.Any, 23001));
                  StunMessage request = new StunMessage();
                  StunMessage response = GetStunResponse(s, request);
                  PublicIP = response.PublicEndPoint.Address;

              }));
            activeThreads.Add(getIpThread);
            getIpThread.Start();
        }
        public void EndInit()
        {
            foreach (Thread t in activeThreads)
            {
                t.Join();
            }
            activeThreads.Clear();
        }
        public void Dispose()
        {
            foreach(Thread t in activeThreads)
            {
                t.Join();
            }
            activeThreads.Clear();
            //Console.WriteLine("InitializationFinished!");
        }

    }
}
