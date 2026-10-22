// TcpServer.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class TcpClientInfo
{
    public TcpClient Client { get; }
    public string EndPoint { get; }

    public TcpClientInfo(TcpClient client)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        EndPoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
    }
}

public class TcpDataReceivedEventArgs : EventArgs
{
    public string Data { get; }
    public TcpClientInfo Client { get; }

    public TcpDataReceivedEventArgs(string data, TcpClientInfo client)
    {
        Data = data;
        Client = client;
    }
}

public class TcpClientConnectedEventArgs : EventArgs
{
    public TcpClientInfo Client { get; }
    public TcpClientConnectedEventArgs(TcpClientInfo client) => Client = client;
}

public class TcpClientDisconnectedEventArgs : EventArgs
{
    public string EndPoint { get; }
    public TcpClientDisconnectedEventArgs(string endPoint) => EndPoint = endPoint;
}

public class TcpServer : IDisposable
{
    private TcpListener _listener;
    private readonly ConcurrentDictionary<string, TcpClientInfo> _clients = new ConcurrentDictionary<string, TcpClientInfo>();
    private CancellationTokenSource _cts;
    private readonly SynchronizationContext _uiContext;

    // 事件（自动在 UI 线程触发）
    public event EventHandler<TcpClientConnectedEventArgs> ClientConnected;
    public event EventHandler<TcpClientDisconnectedEventArgs> ClientDisconnected;
    public event EventHandler<TcpDataReceivedEventArgs> DataReceived;
    public event EventHandler<string> LogMessage; // 用于日志输出

    public bool IsRunning { get; private set; }
    public IPAddress IP { get; private set; }
    public int Port { get; private set; }

    // 构造函数：自动捕获当前同步上下文（通常是 UI 线程）
    public TcpServer(IPAddress ip, int port)
    {
        IP = ip;
        Port = port;
        _uiContext = SynchronizationContext.Current; // 在 UI 线程创建时有效
    }

    public void Start()
    {
        if (IsRunning)
            return;

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IP, Port);
        _listener.Start();
        IsRunning = true;

        OnLog($"TCP 服务器启动，监听IP{IP}端口 {Port}");
        _ = AcceptClientsAsync(_cts.Token);
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                if (token.IsCancellationRequested)
                    break;

                var clientInfo = new TcpClientInfo(client);
                _clients.TryAdd(clientInfo.EndPoint, clientInfo);

                OnClientConnected(clientInfo);
                _ = HandleClientAsync(clientInfo, token);
            }
            catch (ObjectDisposedException)
            {
                break; // Listener 已关闭
            }
            catch (Exception ex)
            {
                OnLog($"接受客户端时出错: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClientInfo clientInfo, CancellationToken token)
    {
        byte[] buffer = new byte[1024];
        try
        {
            using (clientInfo.Client)
            using (var stream = clientInfo.Client.GetStream())
            {
                while (!token.IsCancellationRequested && clientInfo.Client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0)
                        break; // 客户端断开

                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnDataReceived(data, clientInfo);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        catch (Exception ex)
        {
            OnLog($"处理客户端 {clientInfo.EndPoint} 时出错: {ex.Message}");
        }
        finally
        {
            RemoveClient(clientInfo.EndPoint);
            OnClientDisconnected(clientInfo.EndPoint);
        }
    }

    public void SendToClient(string endPoint, string message)
    {
        if (_clients.TryGetValue(endPoint, out var clientInfo))
        {
            SendToClientInternal(clientInfo, message);
        }
    }

    public void SendToAllClients(string message)
    {
        foreach (var client in _clients.Values)
        {
            SendToClientInternal(client, message);
        }
    }

    private void SendToClientInternal(TcpClientInfo client, string message)
    {
        try
        {
            if (client.Client.Connected)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                client.Client.GetStream().Write(data, 0, data.Length);
            }
        }
        catch (Exception ex)
        {
            OnLog($"向 {client.EndPoint} 发送失败: {ex.Message}");
            RemoveClient(client.EndPoint);
            OnClientDisconnected(client.EndPoint);
        }
    }

    private void RemoveClient(string endPoint)
    {
        _clients.TryRemove(endPoint, out _);
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        _cts?.Cancel();
        _listener?.Stop();

        // 关闭所有客户端
        foreach (var client in _clients.Values)
        {
            try
            { client.Client.Close(); }
            catch { }
        }
        _clients.Clear();

        IsRunning = false;
        OnLog("TCP 服务器已停止");
    }

    // 线程安全地触发事件（调度到 UI 线程）
    private void OnClientConnected(TcpClientInfo client)
    {
        _uiContext?.Post(_ => ClientConnected?.Invoke(this, new TcpClientConnectedEventArgs(client)), null);
    }

    private void OnClientDisconnected(string endPoint)
    {
        _uiContext?.Post(_ => ClientDisconnected?.Invoke(this, new TcpClientDisconnectedEventArgs(endPoint)), null);
    }

    private void OnDataReceived(string data, TcpClientInfo client)
    {
        _uiContext?.Post(_ => DataReceived?.Invoke(this, new TcpDataReceivedEventArgs(data, client)), null);
    }

    private void OnLog(string message)
    {
        _uiContext?.Post(_ => LogMessage?.Invoke(this, $"{message}"), null);
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _listener?.Stop();
        //_listener.Dispose();
        GC.SuppressFinalize(this);
    }
}