# AWS.Beanstalk.WebApplication
A demo web application in .NET Core for AWS Elastic Beanstalk


## Getting started


- Run the script **AWS.Beanstalk.WebApplication\scripts\pack.bat** to generate the application file **bundle.zip** in the **output** directory.
This bundle contains :<br>
    * the application binaries
    * .ebextensions configuration files (custom environment variables, elasticbeanstalk setup, aws resources such as dynamodb table ... etc), 
	* a manisfest file **aws-windows-deployment-manifest.json** describing the deployment in IIS

- Under ElasticBeanstalk console, create an application then its environment with selected platform **.NET (Windows/IIS)** and upload the **bundle.zip** file.

- Once the environment is created, browse the website, play with the product page. Product items will be stored and retrieved from generated **Product** dynamoDB table.


