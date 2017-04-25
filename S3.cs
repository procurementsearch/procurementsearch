using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;

namespace SearchProcurement.AWS
{
    public class S3
    {        
        Amazon.S3.AmazonS3Client S3Client = null;

        public S3(string accessKeyId, string secretAccessKey, string serviceUrl)
        {            
            Amazon.S3.AmazonS3Config s3Config = new Amazon.S3.AmazonS3Config();
            s3Config.ServiceURL = serviceUrl;

            this.S3Client = new Amazon.S3.AmazonS3Client(accessKeyId, secretAccessKey, s3Config);
        }


        public void UploadFile(string filePath, string s3Bucket, string newFileName, bool deleteLocalFileOnSuccess)
        {
            //save in s3
            Amazon.S3.Model.PutObjectRequest s3PutRequest = new Amazon.S3.Model.PutObjectRequest();
            s3PutRequest = new Amazon.S3.Model.PutObjectRequest();
            s3PutRequest.FilePath = filePath;
            s3PutRequest.BucketName = s3Bucket;
            s3PutRequest.CannedACL = Amazon.S3.S3CannedACL.PublicRead;

            //key - new file name
            if (!string.IsNullOrWhiteSpace(newFileName))
            {
                s3PutRequest.Key = newFileName;
            }

            s3PutRequest.Headers.Expires = new DateTime(2020, 1, 1);

            try
            {
                Amazon.S3.Model.PutObjectResponse s3PutResponse = this.S3Client.PutObject(s3PutRequest);

                if (deleteLocalFileOnSuccess)
                {
                    //Delete local file
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                //handle exceptions
            }
        }
    }
}