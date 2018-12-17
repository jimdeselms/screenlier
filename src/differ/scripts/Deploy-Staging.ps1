# This is a Powershell script, and it should probably be bashified if we want to
# create a jenkins job to do the same thing.
Param ([Parameter(Mandatory=$true)]$buildNumber)

& $psscriptroot\Deploy-ByTagName.ps1 $buildNumber-staging

"Deployed $buildNumber-staging to staging environment."
"To promote to production, run Promote-ToProduction $buildNumber"
