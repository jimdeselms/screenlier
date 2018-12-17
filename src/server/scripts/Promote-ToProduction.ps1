# This is a Powershell script, and it should probably be bashified if we want to
# create a jenkins job to do the same thing.
Param ([Parameter(Mandatory=$true)]$buildNumber)

# Login to EC2 Repo via Docker
aws ecr get-login --no-include-email --region eu-west-1 | iex

# Get the staging version
docker pull 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-server:$buildNumber-staging

# Tag it with the version number (without staging)
docker tag 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-server:$buildNumber-staging 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-server:$buildNumber-prod

# And tag it with latest
docker tag 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-server:$buildNumber-staging 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-server:latest

# And push them up
docker push 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-server:$buildNumber-prod
docker push 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-server:latest

"Promoted $buildNumber-staging to $buildNubmer-prod and staging"

# Then deploy the latest.
& $psscriptroot\Deploy-ByTagName.ps1 $buildNumber-prod 
