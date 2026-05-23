using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CrystalFolders.Helpers
{
    public static class PipeServer
    {
        #region================== FIELDS & CONSTANTS ==================
        private const string PIPE_NAME = "CrystalFolders_Pipe_8F3A2B1C";
        private static CancellationTokenSource _cts;
        #endregion

        #region================== EVENTS ==================
        public static event Action<string[]> MessageReceived;
        #endregion

        #region================== PUBLIC METHODS ==================
        public static void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ServerLoop(_cts.Token));
        }

        public static void Stop()
        {
            try { _cts?.Cancel(); } catch { }
        }

        public static bool SendToRunningInstance(string[] args)
        {
            try
            {
                using (var client = new NamedPipeClientStream(
                    ".", PIPE_NAME, PipeDirection.Out))
                {
                    client.Connect(2000);

                    using (var writer = new StreamWriter(client, Encoding.UTF8))
                    {
                        string payload = string.Join("\n", args);
                        writer.Write(payload);
                        writer.Flush();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PipeClient] {ex.Message}");
                return false;
            }
        }
        #endregion

        #region================== SERVER LOOP ==================
        private static async Task ServerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(
                        PIPE_NAME,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous))
                    {
                        await server.WaitForConnectionAsync(token);

                        using (var reader = new StreamReader(server, Encoding.UTF8))
                        {
                            string content = await reader.ReadToEndAsync();
                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                string[] args = content.Split(
                                    new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                                for (int i = 0; i < args.Length; i++)
                                    args[i] = args[i].TrimEnd('\r');

                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    MessageReceived?.Invoke(args);
                                });
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PipeServer] {ex.Message}");
                    await Task.Delay(500, token).ContinueWith(_ => { });
                }
            }
        }
        #endregion
    }
}