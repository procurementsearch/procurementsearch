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
            config.ServiceURL = Defines.AppSettings.s3ServiceUrl;
            config.ForcePathStyle = true;
            s3Client = new AmazonS3Client(Defines.AppSettings.s3AccessKey, Defines.AppSettings.s3SecretKey, config);
        }


        /**
         * Upload a file from disk to an s3 bucket
         * @param filePath The path to the file on disk
         * @param s3Bucket The s3 bucket to upload to
         * @param newName The name to assign to the uploaded file, once it's uploaded
         * @return string The full URL to the file after it has been uploaded
         */
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
            return config.ServiceURL + (config.ServiceURL[config.ServiceURL.Length - 1] == '/' ? "" : "/") + s3Bucket + "/" + newName;
        }


        /**
         * Upload a buffer of bytes to an s3 bucket
         * @param buffer The data to store
         * @param s3Bucket The s3 bucket to upload to
         * @param newName The name to assign to the uploaded file, once it's uploaded
         * @return string The full URL to the object after it has been uploaded
         */
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
            return config.ServiceURL + (config.ServiceURL[config.ServiceURL.Length - 1] == '/' ? "" : "/") + s3Bucket + "/" + newName;
        }


        /**
         * Delete an object from an s3 bucket
         * @param s3Bucket The s3 bucket to delete from
         * @param name The name of the object to delete
         * @return none
         */
        public void Delete(string s3Bucket, string name)
        {
            DeleteObjectRequest d = new DeleteObjectRequest();
            d.BucketName = s3Bucket;
            d.Key = name;

            // And, delete the object
            s3Client.DeleteObjectAsync(d);
        }



        /**
         * Copy an object on s3
         * @param oldS3Bucket The s3 bucket to copy from
         * @param oldName The name of the object to copy
         * @param newS3Bucket The s3 bucket to copy from
         * @param newName The name of the object to copy
         * @return none
         */
        public void Duplicate(string oldS3Bucket, string oldName, string newS3Bucket, string newName)
        {
            CopyObjectRequest c = new CopyObjectRequest();
            c.SourceBucket = oldS3Bucket;
            c.SourceKey = oldName;
            c.DestinationBucket = newS3Bucket;
            c.DestinationKey = newName;

            // And, copy the object
            s3Client.CopyObjectAsync(c);
        }



    }
}