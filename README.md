# AWS.Beanstalk.WebApplication
A demo web application in .NET Core for AWS Elastic Beanstalk.

It is leveraging AWS DynamoDB and X-Ray services, via configuration files.


## 1. Folder organization

Main components are highlighted below :

```
|
| -- /AWS.Beanstalk.WebApplication/                               -> the .NET application
        |
        | -- appsettings.json                                     -> The configuration file
        |
        | -- /Controllers/
                | -- ProductController.cs
        |
        | -- /scripts/
                | -- /deploy/
                        | -- aws-windows-deployment-manifest.json -> manisfest for deployment under Windows IIS
                | -- build.bat                                    -> script for build the .NET application
                | -- deploy.bat                                   -> script for deployment to elasticbeanstalk

```

When running for development locally, you should configure **appsettings.json** with correct region and create a dynamodb named "Product" table manually.

## 2. Prerequisites

### 2.1 Create an EC2 Profile

Your elasticbeanstalk environment will launch EC2 instance(s) with the default AWS role :

- **"aws-elasticbeanstalk-ec2-role"**. You need extend it with additionnal policies:

    * **SSM Role (optional)** to log into EC2 via AWS Session manager console for debug. So, there's no need to attach the EC2 any keypair and any security group with RDP ingress rule.
    * Custom inline policy **"MyCustomPolicy" (mandatory)** for DynamoDB, X-Ray, SNS operations defined as below:

```
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "CustomPolicy",
            "Effect": "Allow",
            "Action": [
                "sns:Publish",
                "dynamodb:PutItem",
                "dynamodb:DescribeTable",
                "dynamodb:DeleteItem",
                "dynamodb:GetItem",
                "dynamodb:Scan",
                "dynamodb:UpdateItem",
                "xray:GetSamplingStatisticSummaries",
                "xray:PutTelemetryRecords",
                "xray:GetSamplingRules",
                "xray:GetSamplingTargets",
                "xray:PutTraceSegments"
            ],
            "Resource": "*"
        }
    ]
}
```
EC2 profile should look like:

![alt capture](https://github.com/danmgs/AWS.Beanstalk.WebApplication/blob/master/img/configure_ec2_profile.PNG)


### 2.2 Setup the eb-cli and create an eb configuration

- You must install the [eb-cli](https://docs.aws.amazon.com/fr_fr/elasticbeanstalk/latest/dg/eb-cli3-install-windows.html) in order to the **script\build.bat** to make the deployment to elasticbeanstalk.

You may require to install [python](https://www.anaconda.com) first.

- Go to **script\deploy** directory, type command:

```
eb init
```

Follow instructions to generate the elasticbeanstalk application and its environment in [elasticbeanstalk console](https://console.aws.amazon.com/elasticbeanstalk).

<details>
  <summary>Click to expand details</summary>

  ![alt capture](https://github.com/danmgs/AWS.Beanstalk.WebApplication/blob/master/img/configure_eb_app_env.PNG)

</details>

This will generate the configuration file **AWS.Beanstalk.WebApplication\scripts\deploy\\.elasticbeanstalk\\** which will be used further by the **script\deploy.bat**.

To [create the environment](https://docs.aws.amazon.com/fr_fr/elasticbeanstalk/latest/dg/eb3-create.html), you can use command:

```
# this command create an environment based on your parameters

eb create
```

BUT I prefer to create it via the [elasticbeanstalk console](https://console.aws.amazon.com/elasticbeanstalk) with following options:


| Configuration         | Value to select               | Comments                                                         |
| :-------------------: | ----------------------------- | ---------------------------------------------------------------- |
| Environment type      | Web server environment        |                                                                  |
| Application name      | Appdemo                       | name it as you like                                              |
| Environment name      | Appdemo-env                   | name it as you like                                              |
| Platform              | .NET (Windows/IIS)            |                                                                  |
| Application code      | default sample application    |                                                                  |
| IAM Instance Profile  | aws-elasticbeanstalk-ec2-role | refer Prerequisites section                                      |

- Once the environment created,

set the default environment, you have just created (I named it ***Appdemo-env***) for further deployment.

```
eb use Appdemo-env
```


## 3. Getting started

- Go to directory **AWS.Beanstalk.WebApplication\scripts**, in order :

    * run the script **build.bat** to build the application.

    This will generate into the temporary **output** directory.

    * run the script **deploy.bat** to deploy the application.

    This will generate a bundle zipfile into the **deploy** directory.

    The bundle will automatically be uploaded to your elasticbeanstalk environment, by leveraging the **eb deploy** command against your elasticbeanstalk configuration.

- The bundle contains:

    * the application binaries

    * .ebextensions configuration files for:
        * custom [environment variables](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/environments-cfg-softwaresettings.html) (in [elasticbeanstalk console](https://console.aws.amazon.com/elasticbeanstalk): **Configuration > Software > Environment properties**)

        * aws resources such as Dynamodb **"Product"** table

        * X-Ray daemon setup

        * a [manifest](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/dotnet-manifest.html) file **"aws-windows-deployment-manifest.json"** describing the deployment in IIS

        * elasticbeanstalk configuration

- Once the environment is ready, browse the website:

    * play with the product page.

    Product items will be stored and retrieved from generated **Product** dynamoDB table.

    * check the [X-Ray console](https://aws.amazon.com/xray) for insights.

    Service map in X-Ray:

    ![alt capture](https://github.com/danmgs/AWS.Beanstalk.WebApplication/blob/master/img/xray_service_map.PNG)

    You can filter traces on annotations via the search bar, by example:

    ```
    # filter on create product operation
    annotation.operationType="CreateProduct"

    # filter on all type of operations with product
    annotation.operationType CONTAINS "Product"
    ```

:mag_right: Filtering
<details>
  <summary>Click to expand details</summary>

  ![alt capture](https://github.com/danmgs/AWS.Beanstalk.WebApplication/blob/master/img/xray_filter.PNG)

  ![alt capture](https://github.com/danmgs/AWS.Beanstalk.WebApplication/blob/master/img/xray_segment_details.PNG)

  ![alt capture](https://github.com/danmgs/AWS.Beanstalk.WebApplication/blob/master/img/xray_segment_details_annotations.PNG)

</details>

## 4. Useful resources

- [Set environment properties](https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/environments-cfg-softwaresettings.html)

- [Config samples](https://github.com/awsdocs/elastic-beanstalk-samples/tree/master/configuration-files/aws-provided/instance-configuration)


## 5. Some useful commands when connected into EC2

```bash
  # View application custom logs
    cat C:\logs\xray-sdk.log
    cat C:\logs\webapp.log
    cat C:\logs\all.log
```

```bash
  # List all started services in windows
    net start
```

```bash
  # Stop and start any service: net stop/start servicename
    net stop "AWS X-Ray"
    net start "AWS X-Ray"
```

```bash
  # Restart IIS
    iisreset
```