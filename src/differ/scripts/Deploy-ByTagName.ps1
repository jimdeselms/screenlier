# This is a Powershell script, and it should probably be bashified if we want to
# create a jenkins job to do the same thing.
Param ([Parameter(Mandatory=$true)]$tag)

$currPath = (pwd).path
$kubectlName = if ($tag.EndsWith("-staging")) { "canary-kubectl-staging" } else { "canary-kubectl" }

# Login to EC2 Repo via Docker
aws ecr get-login --no-include-email --region eu-west-1 | iex
$ecrLoggedIn = $true

# Create the repository if it doesn't already exist.
$repos = aws ecr describe-repositories --query "repositories[].repositoryName" | Out-String | ConvertFrom-Json
if ($repos -notcontains "screenly-differ")
{
	"Creating ECR repository screenly-differ"
	aws ecr create-repository --repository-name screenly-differ
}

# Push the current version

docker push 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-differ:$tag

# Pull the latest kubectl image (in case it's been updated)

docker pull 779051441487.dkr.ecr.eu-west-1.amazonaws.com/$($kubectlName):latest

# Deploy envelopes deployment via kubectl (after the very first run, this deployment will always exist)

#docker run --rm --entrypoint kubectl --mount type=bind,source=$currPath,destination=/workspace -w /workspace 779051441487.dkr.ecr.eu-west-1.amazonaws.com/$($kubectlName):latest create -f deployments/deployment-screenly-differ.yaml

# In case we made updates to the deployment file, run apply

docker run --rm --entrypoint kubectl --mount type=bind,source=$currPath,destination=/workspace -w /workspace 779051441487.dkr.ecr.eu-west-1.amazonaws.com/$($kubectlName):latest apply -f deployments/deployment-screenly-differ.yaml

# Now perform rolling-update to latest version (modify the version tag on the docker image spec)

docker run --rm --entrypoint kubectl --mount type=bind,source=$currPath,destination=/workspace -w /workspace 779051441487.dkr.ecr.eu-west-1.amazonaws.com/$($kubectlName):latest set image deployment screenly-differ screenly-differ=779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly-differ:$tag --record

"Deployed $tag"
