/*
import { CloudFrontClient, CreateInvalidationCommand } from "@aws-sdk/client-cloudfront";

export const handler = async (event) => {
    // CloudFront distribution ID
    const distributionId = 'E1TWGKFDCFT1F9';
    
    // CloudFront 클라이언트 생성
    const cloudFront = new CloudFrontClient();
    
    try {
        // S3 이벤트에서 변경된 파일 경로 추출
        const invalidationPaths = event.Records.map(record => {
            const objectKey = record.s3.object.key;
            // URL 인코딩된 키를 디코딩
            const decodedKey = decodeURIComponent(objectKey.replace(/\+/g, ' '));
            return '/' + decodedKey;
        });
        
        // 무효화 요청 생성
        const command = new CreateInvalidationCommand({
            DistributionId: distributionId,
            InvalidationBatch: {
                Paths: {
                    Quantity: invalidationPaths.length,
                    Items: invalidationPaths
                },
                CallerReference: `${Date.now()}`
            }
        });
        
        // 무효화 실행
        const response = await cloudFront.send(command);
        
        console.log('Invalidation created:', response);
        return {
            statusCode: 200,
            body: JSON.stringify({
                message: 'Invalidation created successfully',
                invalidationId: response.Invalidation.Id
            })
        };
        
    } catch (error) {
        console.error('Error creating invalidation:', error);
        return {
            statusCode: 500,
            body: JSON.stringify({
                message: 'Error creating invalidation',
                error: error.message
            })
        };
    }
};
*/

/*
{
  "Records": [
    {
      "eventVersion": "2.1",
      "eventSource": "aws:s3",
      "awsRegion": "ap-northeast-2",
      "eventTime": "2025-05-21T10:00:00.000Z",
      "eventName": "ObjectCreated:Put",
      "userIdentity": {
        "principalId": "AWS:AROAEXAMPLE:user"
      },
      "requestParameters": {
        "sourceIPAddress": "192.168.0.1"
      },
      "responseElements": {
        "x-amz-request-id": "EXAMPLE123456789",
        "x-amz-id-2": "EXAMPLE123/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG"
      },
      "s3": {
        "s3SchemaVersion": "1.0",
        "configurationId": "testConfigRule",
        "bucket": {
          "name": "global.e-order.org",
          "ownerIdentity": {
            "principalId": "EXAMPLE"
          },
          "arn": "arn:aws:s3:::global.e-order.org"
        },
        "object": {
          "key": "loading_line.png",
          "size": 1024,
          "eTag": "0123456789abcdef0123456789abcdef",
          "sequencer": "0A1B2C3D4E5F678901"
        }
      }
    }
  ]
}
*/