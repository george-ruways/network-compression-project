using System.Net.Sockets;

namespace CompressionClient;

public class MainForm : Form
{
    private const int BufferSize = 81920;

    private readonly TextBox hostTextBox = new TextBox();
    private readonly NumericUpDown portNumber = new NumericUpDown();
    private readonly TextBox fileTextBox = new TextBox();
    private readonly Button browseButton = new Button();
    private readonly Button sendButton = new Button();
    private readonly ProgressBar progressBar = new ProgressBar();
    private readonly Label statusLabel = new Label();

    public MainForm()
    {
        Text = "File Compression Client";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(560, 330);
        Size = new Size(620, 360);

        BuildForm();
    }

    private void BuildForm()
    {
        TableLayoutPanel mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 6
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label titleLabel = new Label
        {
            Text = "Network File Compressor",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 15, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 14)
        };

        TableLayoutPanel serverLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        serverLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        serverLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        serverLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        serverLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        Label hostLabel = new Label { Text = "Server", AutoSize = true, Anchor = AnchorStyles.Left };
        hostTextBox.Text = "127.0.0.1";
        hostTextBox.Dock = DockStyle.Fill;

        Label portLabel = new Label
        {
            Text = "Port",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(12, 6, 4, 0)
        };

        portNumber.Minimum = 1;
        portNumber.Maximum = 65535;
        portNumber.Value = 9000;
        portNumber.Dock = DockStyle.Fill;

        serverLayout.Controls.Add(hostLabel, 0, 0);
        serverLayout.Controls.Add(hostTextBox, 1, 0);
        serverLayout.Controls.Add(portLabel, 2, 0);
        serverLayout.Controls.Add(portNumber, 3, 0);

        TableLayoutPanel fileLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));

        Label fileLabel = new Label { Text = "File", AutoSize = true, Anchor = AnchorStyles.Left };
        fileTextBox.ReadOnly = true;
        fileTextBox.Dock = DockStyle.Fill;

        browseButton.Text = "Browse";
        browseButton.Dock = DockStyle.Fill;
        browseButton.Click += BrowseButton_Click;

        fileLayout.Controls.Add(fileLabel, 0, 0);
        fileLayout.Controls.Add(fileTextBox, 1, 0);
        fileLayout.Controls.Add(browseButton, 2, 0);

        sendButton.Text = "Send and Save Compressed File";
        sendButton.Height = 36;
        sendButton.Dock = DockStyle.Top;
        sendButton.Click += SendButton_Click;

        progressBar.Dock = DockStyle.Top;
        progressBar.Height = 18;
        progressBar.Visible = false;
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 25;
        progressBar.Margin = new Padding(0, 12, 0, 8);

        statusLabel.Text = "Choose a file to start.";
        statusLabel.AutoSize = true;
        statusLabel.Dock = DockStyle.Top;

        mainLayout.Controls.Add(titleLabel);
        mainLayout.Controls.Add(serverLayout);
        mainLayout.Controls.Add(fileLayout);
        mainLayout.Controls.Add(sendButton);
        mainLayout.Controls.Add(progressBar);
        mainLayout.Controls.Add(statusLabel);

        Controls.Add(mainLayout);
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "Choose a file to compress",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            fileTextBox.Text = dialog.FileName;
            statusLabel.Text = "Ready to send.";
        }
    }

    private async void SendButton_Click(object? sender, EventArgs e)
    {
        string inputPath = fileTextBox.Text.Trim();
        if (!File.Exists(inputPath))
        {
            MessageBox.Show(this, "Please choose a valid file first.", "Missing File",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using SaveFileDialog saveDialog = new SaveFileDialog
        {
            Title = "Save compressed file",
            FileName = Path.GetFileName(inputPath) + ".gz",
            Filter = "GZip file (*.gz)|*.gz|All files (*.*)|*.*"
        };

        if (saveDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SetBusy(true, "Sending file to server...");

        try
        {
            string host = hostTextBox.Text.Trim();
            int port = (int)portNumber.Value;
            string outputPath = saveDialog.FileName;

            TransferResult result = await Task.Run(() => SendFileToServer(host, port, inputPath, outputPath));

            statusLabel.Text = $"Saved {result.CompressedBytes} bytes to {outputPath}";
            MessageBox.Show(this, "Compressed file saved successfully.", "Done",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Transfer failed.";
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy, string message = "")
    {
        browseButton.Enabled = !isBusy;
        sendButton.Enabled = !isBusy;
        hostTextBox.Enabled = !isBusy;
        portNumber.Enabled = !isBusy;
        progressBar.Visible = isBusy;

        if (!string.IsNullOrWhiteSpace(message))
        {
            statusLabel.Text = message;
        }
    }

    private static TransferResult SendFileToServer(string host, int port, string inputPath, string outputPath)
    {
        using TcpClient client = new TcpClient();
        client.Connect(host, port);

        using NetworkStream stream = client.GetStream();
        long originalSize = new FileInfo(inputPath).Length;

        WriteInt64(stream, originalSize);
        SendFile(stream, inputPath);

        long compressedSize = ReadInt64(stream);
        ReceiveFile(stream, outputPath, compressedSize);

        return new TransferResult(originalSize, compressedSize);
    }

    private static void SendFile(NetworkStream stream, string filePath)
    {
        byte[] buffer = new byte[BufferSize];

        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        int read;
        while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            stream.Write(buffer, 0, read);
        }
    }

    private static void ReceiveFile(NetworkStream stream, string outputPath, long bytesToRead)
    {
        byte[] buffer = new byte[BufferSize];

        using FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        long remaining = bytesToRead;

        while (remaining > 0)
        {
            int wanted = remaining > buffer.Length ? buffer.Length : (int)remaining;
            int read = stream.Read(buffer, 0, wanted);

            if (read == 0)
            {
                throw new EndOfStreamException("Server disconnected before sending the whole compressed file.");
            }

            fileStream.Write(buffer, 0, read);
            remaining -= read;
        }
    }

    private static long ReadInt64(NetworkStream stream)
    {
        byte[] sizeBytes = ReadExact(stream, sizeof(long));
        return BitConverter.ToInt64(sizeBytes, 0);
    }

    private static void WriteInt64(NetworkStream stream, long value)
    {
        byte[] sizeBytes = BitConverter.GetBytes(value);
        stream.Write(sizeBytes, 0, sizeBytes.Length);
    }

    private static byte[] ReadExact(NetworkStream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;

        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
            {
                throw new EndOfStreamException("Connection closed while reading data.");
            }

            offset += read;
        }

        return buffer;
    }

    private sealed record TransferResult(long OriginalBytes, long CompressedBytes);
}

