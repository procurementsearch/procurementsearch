using System;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace SearchProcurement.AWS
{
    public class S3
    {        
        AmazonS3Config config = null;
        AmazonS3Client s3Client = null;

        public S3()
        {
            // Set up the configuration object and the client
            config = new AmazonS3Config();
            config.ServiceURL = "https://objects-us-west-1.dream.io/";
            config.ForcePathStyle = true;
            s3Client = new AmazonS3Client(Defines.s3AccessKey, Defines.s3SecretKey, config);
        }

        public string UploadFile(string filePath, string s3Bucket, string newName)
        {
            TransferUtility up = new TransferUtility(s3Client);
            TransferUtilityUploadRequest upreq = new TransferUtilityUploadRequest();
            upreq.FilePath = filePath;
            upreq.BucketName = s3Bucket;
            upreq.Key = newName;
            upreq.CannedACL = S3CannedACL.PublicRead;
            up.Upload(upreq);

            // Return the object URL
            return config.ServiceURL + "/" + s3Bucket + "/" + newName;
        }

        public string UploadBuffer(byte[] buffer, string s3Bucket, string newName)
        {
            MemoryStream m = new MemoryStream();
            m.Write(buffer, 0, buffer.Length);

            TransferUtility up = new TransferUtility(s3Client);
            TransferUtilityUploadRequest upreq = new TransferUtilityUploadRequest();
            upreq.InputStream = m;
            upreq.BucketName = s3Bucket;
            upreq.Key = newName;
            upreq.CannedACL = S3CannedACL.PublicRead;
            up.Upload(upreq);

            // Return the object URL
            return config.ServiceURL + "/" + s3Bucket + "/" + newName;
        }

    }
}