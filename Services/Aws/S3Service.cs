using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;

namespace ThryftAiServer.Services.Aws;

public class S3Service(IAmazonS3 s3Client, IConfiguration configuration)
{
    private readonly string _bucketName = configuration["AWS:BucketName"] ?? "thryftai";

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var fileTransferUtility = new TransferUtility(s3Client);
        
        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = $"images/{Guid.NewGuid()}_{System.Text.RegularExpressions.Regex.Replace(fileName, @"\s+", "")}",
            BucketName = _bucketName,
            ContentType = contentType
        };

        await fileTransferUtility.UploadAsync(uploadRequest);

        return $"https://{_bucketName}.s3.amazonaws.com/{uploadRequest.Key}";
    }
}
