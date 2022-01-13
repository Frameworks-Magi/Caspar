using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Caspar
{
    public static partial class Api
    {
        public class Progress<T> : IProgress<T>
        {
            public dynamic Args { get; set; }
            private readonly Action<T> handler;

            public Progress(Action<T> handler)
            {
                this.handler = handler;
            }

            public void Report(T value)
            {
                handler(value);
            }
        }

        public class ProgressableStream : System.IO.Stream
        {
            // NOTE: for illustration purposes. For production code, one would want to
            // override *all* of the virtual methods, delegating to the base _stream object,
            // to ensure performance optimizations in the base _stream object aren't
            // bypassed.

            private readonly Stream stream;
            private readonly IProgress<int> readProgress;
            private readonly IProgress<int> writeProgress;

            public ProgressableStream(Stream stream, IProgress<int> readProgress, IProgress<int> writeProgress)
            {
                this.stream = stream;
                this.readProgress = readProgress;
                this.writeProgress = writeProgress;
            }

            public override bool CanRead { get { return stream.CanRead; } }
            public override bool CanSeek { get { return stream.CanSeek; } }
            public override bool CanWrite { get { return stream.CanWrite; } }
            public override long Length { get { return stream.Length; } }
            public override long Position
            {
                get { return stream.Position; }
                set { stream.Position = value; }
            }

            public override void Flush() { stream.Flush(); }
            public override long Seek(long offset, SeekOrigin origin) { return stream.Seek(offset, origin); }
            public override void SetLength(long value) { stream.SetLength(value); }


            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = stream.Read(buffer, offset, count);

                readProgress?.Report(bytesRead);
                return bytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                writeProgress?.Report(count);
                stream.Write(buffer, offset, count);
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                writeProgress?.Report(count);
                await stream.WriteAsync(buffer, offset, count, cancellationToken);
                return;
            }
        }



        public static class ProgressableZipFile
        {
            public static async Task CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, IProgress<double> progress, CompressionLevel compressionLevel = CompressionLevel.Fastest)
            {
                sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

                FileInfo[] sourceFiles =
                    new DirectoryInfo(sourceDirectoryName).GetFiles("*", SearchOption.AllDirectories);
                double totalBytes = sourceFiles.Sum(f => f.Length);
                long currentBytes = 0;

                using (ZipArchive archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
                {
                    foreach (FileInfo file in sourceFiles)
                    {
                        // NOTE: naive method to get sub-path from file name, relative to
                        // input directory. Production code should be more robust than this.
                        // Either use Path class or similar to parse directory separators and
                        // reconstruct output file name, or change this entire method to be
                        // recursive so that it can follow the sub-directories and include them
                        // in the entry name as they are processed.
                        string entryName = file.FullName.Substring(sourceDirectoryName.Length + 1);

                        entryName = entryName.Replace("\\", "/");

                        ZipArchiveEntry entry = archive.CreateEntry(entryName, compressionLevel);

                        entry.LastWriteTime = file.LastWriteTime;

                        using (Stream inputStream = File.OpenRead(file.FullName))
                        using (Stream outputStream = entry.Open())
                        {
                            Stream progressStream = new ProgressableStream(inputStream,
                                new Progress<int>(i =>
                                {
                                    currentBytes += i;
                                    progress.Report(currentBytes / totalBytes);
                                }), null);

                            await progressStream.CopyToAsync(outputStream);
                        }
                    }
                }
            }

            public static async Task ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Progress<double> progress)
            {
                using (ZipArchive archive = ZipFile.OpenRead(sourceArchiveFileName))
                {
                    long totalBytes = archive.Entries.Sum(e => e.Length);
                    long currentBytes = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fileName = Path.Combine(destinationDirectoryName, entry.FullName);
                        fileName = fileName.Replace('\\', '/');
                        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                        using (Stream inputStream = entry.Open())
                        using (Stream outputStream = File.OpenWrite(fileName))
                        {
                            Stream progressStream = new ProgressableStream(outputStream, null,
                                new Progress<int>(i =>
                                {
                                    currentBytes += i;
                                    progress.Args = fileName;
                                    progress.Report((double)currentBytes / totalBytes);
                                }));

                            await inputStream.CopyToAsync(progressStream);
                        }

                        File.SetLastWriteTime(fileName, entry.LastWriteTime.LocalDateTime);
                    }
                }
            }
        }
    }
    
}
