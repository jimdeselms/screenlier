# Publishes your code to the staging environment.
# To access it directly, go to https://screenly.clusters.storefront.vpsvc.com
# Through the Vistaprint website: https://www.vistaprint.com/cms/product-refinement

Function Get-HighestVersionNumber
{
	(aws ecr describe-images --repository-name screenly --query "imageDetails[].imageTags[]" | convertfrom-json) | ? { $_.EndsWith('') } | % { [int]($_ -replace '', '') } | sort -descending | select-object -first 1
}

Function Publish-Artifact($versionNumber)
{
	$buildPath = Join-Path $psscriptroot ..
	
	# Build docker image from pwd with BUILD_NUMBER
	
	docker build `
		-t screenly:$($versionNumber) $buildPath `
		--build-arg PUBLIC_URL=http://screenly.coalmine.staging.clusters.storefront.vpsvc.com
	
	# Tag the current image with specific BUILD_NUMBER version
	
	docker tag screenly:$($versionNumber) 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly:$($versionNumber)
	
	"Built version $versionNumber."
	
	# Login to EC2 Repo via Docker
	aws ecr get-login --no-include-email --region eu-west-1 | iex

	# Create the repository if it doesn't already exist.
	$repos = aws ecr describe-repositories --query "repositories[].repositoryName" | Out-String | ConvertFrom-Json
	if ($repos -notcontains "screenly")
	{
		"Creating ECR repository screenly"
		aws ecr create-repository --repository-name screenly
	}

	# Push the current version

	docker push 779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly:$($versionNumber)

	"Pushed version $versionNumber to the ECR."
}

Function Deploy-ByTagName($tag)
{
    $currPath = (pwd).path
    
    $kubectlName = "canary-kubectl"
    
    # Login to EC2 Repo via Docker
    aws ecr get-login --no-include-email --region eu-west-1 | iex
    $ecrLoggedIn = $true
    
    # Create the repository if it doesn't already exist.
    $repos = aws ecr describe-repositories --query "repositories[].repositoryName" | Out-String | ConvertFrom-Json
    if ($repos -notcontains "screenly")
    {
        "Creating ECR repository screenly"
        aws ecr create-repository --repository-name screenly
    }
    
    docker run --rm --entrypoint kubectl 779051441487.dkr.ecr.eu-west-1.amazonaws.com/$($kubectlName):latest `
        set image deployment screenly screenly=779051441487.dkr.ecr.eu-west-1.amazonaws.com/screenly:$tag --record
    
    "Deployed $tag"
}

#$version = (Get-HighestVersionNumber) + 1
#Publish-Artifact $version
#Deploy-ByTagName $version
Deploy-ByTagName 2

"$version is now deployed to staging"
