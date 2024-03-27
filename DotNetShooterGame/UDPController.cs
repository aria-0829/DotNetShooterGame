using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using DotNetShooterGame;
using System.Windows.Forms;

public class UDPController
{
    private readonly Socket m_socket = new Socket(AddressFamily.InterNetwork,
                                                  SocketType.Dgram,
                                                  ProtocolType.Udp);
    private const int m_bufSize = 8 * 1024; // buffer size
    private readonly byte[] m_buffer = new byte[m_bufSize]; // a byte array to store received data
    private readonly Queue<Messages> m_messages = new Queue<Messages>(); // a queue to store received messages
    public EndPoint m_epFrom = new IPEndPoint(IPAddress.Any, 0);
    private bool m_isServer = false;
    public bool isConnected = false;

    public void Server(string address, int port)
    {
        m_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
        m_socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
        m_isServer = true;
        Receive();
    }

    public void Client(string address, int port)
    {
        m_socket.Connect(IPAddress.Parse(address), port);
        Receive();
    }

    private void Receive()
    {
        m_socket.BeginReceiveFrom(m_buffer, 0, m_bufSize,
                                  SocketFlags.None, ref m_epFrom, new AsyncCallback(RecvCallback),
                                  null);
    }

    private void RecvCallback(IAsyncResult ar) // invoked when data is received on the socket
    {
        int bytes = m_socket.EndReceiveFrom(ar, ref m_epFrom);
        string message = Encoding.ASCII.GetString(m_buffer, 0, bytes);
        lock (m_messages)
        {
            m_messages.Enqueue(new Messages()
            {
                Message = message,
                RemoteEP = m_epFrom
            });
        }
        if (m_isServer)
        {
            string retMessage = "Server received: " + message;
            m_socket.SendTo(Encoding.ASCII.GetBytes(retMessage), m_epFrom);
        }

        Receive();
    }

    public void SendTo(string text, EndPoint ep) // sends data to a specific endpoint
    {
        byte[] data = Encoding.ASCII.GetBytes(text);
        m_socket.SendTo(data, ep);
    }

    public void Send(string text) // sends data without specifying a destination
    {
        byte[] data = Encoding.ASCII.GetBytes(text);
        m_socket.Send(data, data.Length, SocketFlags.None);
    }

    public Messages GetNextMessage()
    {
        lock (m_messages)
        {
            if (m_messages.Count > 0)
            {
                return m_messages.Dequeue(); // dequeues the next message from the message queue,
                                             // allowing other components to retrieve and process incoming messages
            }
        }
        return null;
    }

    internal void Close()
    {
        m_socket.Close();
        Application.Exit();
    }
}
