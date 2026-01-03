using bifeldy_lib_90.Models;
using Renci.SshNet;
using System.Text;

namespace bifeldy_lib_90.Extensions {

    public static class SshCommandExtension {

        public static async Task ExecuteAsyncWithProgress(
            this SshCommand command,
            CancellationToken cancellationToken,
            IProgress<CScriptOutputLine> progress = null
        ) {
            IAsyncResult asyncResult = command.BeginExecute();

            Task stdoutTask = ReadStreamAsync(command.OutputStream, false, progress, cancellationToken);
            Task stderrTask = ReadStreamAsync(command.ExtendedOutputStream, true, progress, cancellationToken);

            while (!asyncResult.IsCompleted) {
                if (cancellationToken.IsCancellationRequested) {
                    command.CancelAsync(); // Attempt to cancel on server
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Delay(100, cancellationToken); // efficient wait
            }

            await Task.WhenAll(stdoutTask, stderrTask);

            _ = command.EndExecute(asyncResult);
        }

        private static async Task ReadStreamAsync(
            Stream stream,
            bool isError,
            IProgress<CScriptOutputLine> progress,
            CancellationToken token
        ) {
            if (stream == null) {
                return;
            }

            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true)) {
                while (!token.IsCancellationRequested) {
                    string line = await reader.ReadLineAsync(token);

                    if (line == null) {
                        break; // End of stream
                    }

                    progress?.Report(new CScriptOutputLine(line, isError));
                }
            }
        }

    }

}
