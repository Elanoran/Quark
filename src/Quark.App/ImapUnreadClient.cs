using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Quark.App;

public sealed class ImapUnreadClient
{
    private static readonly Regex UnseenRegex = new(@"UNSEEN\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<int> GetUnreadCountAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        if (!IsLoopbackHost(settings.Host))
        {
            throw new InvalidOperationException("Quark only supports local Proton Mail Bridge IMAP hosts.");
        }

        using var tcp = new TcpClient();
        await tcp.ConnectAsync(settings.Host, settings.Port, cancellationToken);

        Stream stream = tcp.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, false, leaveOpen: true);
        await ReadGreetingAsync(reader, cancellationToken);

        if (settings.UseSsl)
        {
            stream = await WrapSslAsync(stream, settings.Host, cancellationToken);
            reader.Dispose();
            using var sslReader = new StreamReader(stream, Encoding.ASCII, false, leaveOpen: true);
            return await ReadUnreadAfterLoginAsync(stream, sslReader, settings, cancellationToken);
        }

        if (settings.UseStartTls)
        {
            string startTlsResponse = await SendCommandAsync(stream, reader, "A001 STARTTLS", cancellationToken);
            if (!IsOk(startTlsResponse))
            {
                throw new InvalidOperationException(CleanResponse(startTlsResponse));
            }

            stream = await WrapSslAsync(stream, settings.Host, cancellationToken);
            reader.Dispose();
            using var tlsReader = new StreamReader(stream, Encoding.ASCII, false, leaveOpen: true);
            return await ReadUnreadAfterLoginAsync(stream, tlsReader, settings, cancellationToken);
        }

        return await ReadUnreadAfterLoginAsync(stream, reader, settings, cancellationToken);
    }

    private static async Task<int> ReadUnreadAfterLoginAsync(
        Stream stream,
        StreamReader reader,
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        string login = await SendCommandAsync(
            stream,
            reader,
            $"A002 LOGIN {Quote(settings.UserName)} {Quote(settings.Password)}",
            cancellationToken);

        if (!IsOk(login))
        {
            throw new InvalidOperationException("Login failed. Check the Proton Bridge IMAP credentials.");
        }

        string status = await SendCommandAsync(
            stream,
            reader,
            $"A003 STATUS {QuoteMailbox(settings.Mailbox)} (UNSEEN)",
            cancellationToken);

        _ = await SendCommandAsync(stream, reader, "A004 LOGOUT", cancellationToken);

        Match match = UnseenRegex.Match(status);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int count))
        {
            throw new InvalidOperationException(CleanResponse(status));
        }

        return count;
    }

    private static async Task<Stream> WrapSslAsync(Stream stream, string host, CancellationToken cancellationToken)
    {
        var ssl = new SslStream(stream, false, (_, certificate, chain, errors) => ValidateCertificate(host, certificate, chain, errors));
        await ssl.AuthenticateAsClientAsync(host, null, System.Security.Authentication.SslProtocols.None, false);
        cancellationToken.ThrowIfCancellationRequested();
        return ssl;
    }

    private static bool ValidateCertificate(
        string host,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

        return IsLoopbackHost(host);
    }

    private static bool IsLoopbackHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ReadGreetingAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        string? line = await reader.ReadLineAsync(cancellationToken);
        if (line is null || !line.StartsWith("* OK", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("IMAP server did not send an OK greeting.");
        }
    }

    private static async Task<string> SendCommandAsync(
        Stream stream,
        StreamReader reader,
        string command,
        CancellationToken cancellationToken)
    {
        string tag = command.Split(' ', 2)[0];
        byte[] bytes = Encoding.ASCII.GetBytes(command + "\r\n");
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        var response = new StringBuilder();
        while (true)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                throw new IOException("IMAP server closed the connection.");
            }

            response.AppendLine(line);
            if (line.StartsWith(tag + " ", StringComparison.OrdinalIgnoreCase))
            {
                return response.ToString();
            }
        }
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string QuoteMailbox(string value)
    {
        return value.Equals("INBOX", StringComparison.OrdinalIgnoreCase) ? "INBOX" : Quote(value);
    }

    private static bool IsOk(string response)
    {
        return response.Contains(" OK ", StringComparison.OrdinalIgnoreCase)
            || response.EndsWith(" OK\r\n", StringComparison.OrdinalIgnoreCase)
            || response.EndsWith(" OK\n", StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanResponse(string response)
    {
        return string.Join(" ", response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
    }
}
