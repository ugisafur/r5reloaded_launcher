using Amazon.S3.Model;
using Amazon.S3;
using System.Net;
using Amazon.Auth.AccessControlPolicy;
using Polly.Retry;
using Polly;

namespace r2_upload
{
    public static class CloudflareClient
    {
        public static string accountId = "";
        public static string accessKey = "";
        public static string accessSecret = "";
        public static int filesLeftCount = 0;

        public static SemaphoreSlim _uploadSemaphore = new(20);

        public static List<Task<string>> InitializeUploadTasks(GameFiles gameFiles, Branch branch, string folderPath, string bucketName)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (gameFiles == null) throw new ArgumentNullException(nameof(gameFiles));
            if (branch == null) throw new ArgumentNullException(nameof(branch));

            var uploadTasks = new List<Task<string>>(gameFiles.files.Count);

            foreach (var file in gameFiles.files)
            {
                string RealitivePath = Path.GetRelativePath(folderPath, file.name);
                string fileUploadPath = $"{branch.game_url.Replace("https://cdn.r5r.org/", "")}/{RealitivePath}";

                uploadTasks.Add(
                    UploadFileAsync(
                        fileUploadPath,
                        file.name,
                        bucketName
                    )
                );
            }

            return uploadTasks;
        }

        private static async Task<string> UploadFileAsync(string fileUploadPath, string fileLocalPath, string bucketName)
        {
            await _uploadSemaphore.WaitAsync();

            try
            {
                await CreateRetryPolicy(50).ExecuteAsync(async () =>
                {
                    await UploadFile(fileUploadPath, fileLocalPath, bucketName);
                });

                return fileUploadPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to upload {fileUploadPath}: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                Console.WriteLine($"Finished uploading {fileUploadPath} | Files Left: {filesLeftCount--}");
                _uploadSemaphore.Release();
            }
        }

        public static async Task UploadFile(string fileUploadPath, string fileLocalPath, string bucketName)
        {
            var s3Client = new AmazonS3Client(
            accessKey,
            accessSecret,
            new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true, // Ensure bucket name is in the URL path
                RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,   // Adjust checksum behavior
                ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED      // Adjust checksum behavior
            });

            var uploadResponses = new List<UploadPartResponse>();

            var initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = fileUploadPath,
                ContentType = "application/octet-stream",
            };

            var initResponse = s3Client.InitiateMultipartUploadAsync(initiateRequest);

            var contentLength = new FileInfo(fileLocalPath).Length;
            var partSize = 5242880;

            // Shared variable to track how many bytes have been processed.
            long uploadedBytes = 0;

            // CancellationTokenSource for the progress logging task.
            var cts = new CancellationTokenSource();

            // Start a background task to log progress every 5 seconds.
            var progressTask = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // Calculate progress percentage; make sure it doesn't exceed 100%
                    double percentage = Math.Min((double)uploadedBytes / contentLength * 100, 100);
                    Console.WriteLine($"{fileUploadPath} upload progress: {percentage:F2}%");
                    try
                    {
                        await Task.Delay(2000, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // If cancelled, exit the loop.
                        break;
                    }
                }
            }, cts.Token);

            try
            {
                long filePosition = 0;
                for (var i = 1; filePosition < contentLength; ++i)
                {
                    // Create request to upload a part.
                    var uploadRequest = new UploadPartRequest
                    {
                        BucketName = bucketName,
                        Key = fileUploadPath,
                        UploadId = initResponse.Result.UploadId,
                        PartNumber = i,
                        PartSize = partSize,
                        FilePosition = filePosition,
                        FilePath = fileLocalPath,
                        DisablePayloadSigning = true
                    };

                    // Upload part and add response to our list.
                    var uploadResponse = await s3Client.UploadPartAsync(uploadRequest);
                    uploadResponses.Add(uploadResponse);

                    uploadedBytes = Math.Min(filePosition + partSize, contentLength);
                    filePosition += partSize;
                }

                // Cancel the progress logging task since uploading is done.
                cts.Cancel();
                // Optionally wait for the progress task to finish cleanly.
                await progressTask;

                // Step 3: complete.
                var completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = fileUploadPath,
                    UploadId = initResponse.Result.UploadId
                };

                // add ETags for uploaded files
                completeRequest.AddPartETags(uploadResponses);

                var completeUploadResponse = s3Client.CompleteMultipartUploadAsync(completeRequest);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception occurred: {0}", exception.ToString());

                var abortMPURequest = new AbortMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = fileUploadPath,
                    UploadId = initResponse.Result.UploadId
                };

                s3Client.AbortMultipartUploadAsync(abortMPURequest);

                throw new Exception("Upload failed", exception);
            }
        }

        private static AsyncRetryPolicy CreateRetryPolicy(int maxRetryAttempts)
        {
            const double exponentialBackoffFactor = 2.0;

            return Polly.Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: maxRetryAttempts,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(exponentialBackoffFactor, retryAttempt)),
                    onRetry: (exception, timeSpan, retryNumber, context) =>
                    {
                    }
                );
        }
    }

    public class GameFiles
    {
        public List<GameFile> files { get; set; }
    }

    public class GameFile
    {
        public string name { get; set; }
        public string checksum { get; set; }
    }
}