# Hosting in AWS

We host `play.void.dev` in an AWS VPC

  * We provision the VPC using Cloudformation stack [platform-vpc.yml](./platform-vpc.yml)
  * We provision the service using Cloudformation stack [platform-service.yml](./platform-service.yml)
  * We deploy the service via a GitHub action defined in [.github/workflows/deploy.yml](../.github/workflows/deploy.yml)

## Dashboards

  * We have an [AWS Dashboard](https://us-west-2.console.aws.amazon.com/cloudwatch/home?region=us-west-2#dashboards/dashboard/platform?start=PT12H&end=null)
  * as well as an [AWS Cost Report](https://us-east-1.console.aws.amazon.com/costmanagement/home#/cost-explorer?chartStyle=STACK&costAggregate=unBlendedCost&endDate=2025-04-23&excludeForecasting=false&filter=%5B%7B%22dimension%22:%7B%22id%22:%22TagKey%22,%22displayValue%22:%22Tag%22%7D,%22operator%22:%22INCLUDES%22,%22values%22:%5B%7B%22value%22:%22share%22,%22displayValue%22:%22share%22%7D%5D,%22growableValue%22:%7B%22value%22:%22pod%22,%22displayValue%22:%22pod%22%7D%7D%5D&futureRelativeRange=CUSTOM&granularity=Daily&groupBy=%5B%22Service%22%5D&historicalRelativeRange=MONTH_TO_DATE&isDefault=false&region=us-west-2&reportArn=arn:aws:ce::339712894694:ce-saved-report%2F90491612-c438-4fcc-bf2b-e047ac9c5e62&reportId=90491612-c438-4fcc-bf2b-e047ac9c5e62&reportName=Share%20Pod%20Daily%20Costs&showOnlyUncategorized=false&showOnlyUntagged=false&startDate=2025-04-01&usageAggregate=undefined&useNormalizedUnits=false)

## External Services

In addition to AWS Services...

  * The database is managed by [PlanetScale](https://app.planetscale.com/voiddotdev/void-cloud)
  * Email is sent by [Postmark](https://account.postmarkapp.com/servers/13931279/streams/outbound/overview)
  * Errors are tracked in [Sentry.io](https://void-industries.sentry.io/projects/platform/?project=4508977545478145)
  * Uptime is also monitored by [Sentry.io](https://void-industries.sentry.io/alerts/rules/uptime/platform/181314/details/?project=4508977545478145&statsPeriod=14d)

## Our CloudFormation Resources

![vpc](./vpc.jpg?raw=true)

Currently...
  * [CFN](https://us-west-2.console.aws.amazon.com/cloudformation/home?region=us-west-2#/stacks?filteringText=&filteringStatus=active&viewNested=true)
    provides IaC provisioning
  * [VPC](https://us-west-2.console.aws.amazon.com/vpcconsole/home?region=us-west-2#VpcDetails:VpcId=vpc-099384c4fec0c1614)
    provides our secure network
  * [EC2](https://us-west-2.console.aws.amazon.com/ec2/home?region=us-west-2#Instances:instanceState=running)
    provides a single `ops` EC2 instance as a bastion host
  * [ECS](https://us-west-2.console.aws.amazon.com/ecs/v2/clusters?region=us-west-2)
    provides the container runtime for our services
  * [ECR](https://us-west-2.console.aws.amazon.com/ecr/private-registry/repositories?region=us-west-2)
    provides a docker registry for each service
  * [ELB](https://us-west-2.console.aws.amazon.com/ec2/home?region=us-west-2#LoadBalancers:v=3;$case=tags:false%5C,client:false;$regex=tags:false%5C,client:false)
    provides both an application and network load balancer (see below)
  * [EFS](https://us-west-2.console.aws.amazon.com/efs/home?region=us-west-2#/file-systems)
    provides the run time file system for deployed games
  * [S3](https://us-west-2.console.aws.amazon.com/s3/buckets/void-cloud?region=us-west-2&tab=objects&bucketType=general)
    provides backup storage for deploys in the `void-cloud` bucket
  * [Elasticache](https://us-west-2.console.aws.amazon.com/elasticache/home?region=us-west-2#/redis)
    provides a Redis backed cache
  * [Secrets Manager](https://us-west-2.console.aws.amazon.com/secretsmanager/listsecrets?region=us-west-2)
    host our secrets and make them available to ECS tasks
  * [CloudWatch](https://us-west-2.console.aws.amazon.com/cloudwatch/home?region=us-west-2#dashboards/dashboard/platform)
    is used for logging, metrics, and dashboards
  * [IAM](https://us-east-1.console.aws.amazon.com/iam/home?region=us-west-2#/home)
    provides roles and policies for our ECS tasks (and GitHub deploy tasks)
  * [RDS](https://us-west-2.console.aws.amazon.com/rds/home?region=us-west-2#)
    is not used at this time, we use PlanetScale as an external mysql provider

### Security Groups

Not shown on the diagram are the 4 security groups...

  * `loadbalancer` - allows HTTP and HTTPS to reach the load balancer only
  * `ops` - allows SSH traffic into the bastion host
  * `web` - allows HTTP traffic between the load balancer and the ecs tasks
  * `data` - allows MYSQL, REDIS, and EFS traffic from the ops and web groups into our data services

### Other Notes

  * CloudFormation stacks applied/updated manually via the AWS console
  * Public subnets route to the internet via the internet gateway (one per VPC)
  * Private subnets route to the internet via a nat gateway (one per AZ)
  * A single `ops` EC2 instance provides an SSH bastion host
  * The load balancer performs SSL offloading (for wildcard *.void.dev)
  * The `ops` ec2 instance uses cloud config to install tools and mount EFS

