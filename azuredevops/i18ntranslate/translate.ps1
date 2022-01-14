$myBranch = $env:SYSTEM_PULLREQUEST_SOURCEBRANCH;

if (!$myBranch) {
    Write-Error "Task could not determine value for the SYSTEM_PULLREQUEST_SOURCEBRANCH environment variable"
	Write-Host "##vso[task.complete result=Failed;]Task should only run in PR builds"
	exit 1
}

$myBranch = $myBranch.substring(11);

[string]$cwd = Get-VstsInput -Name cwd
if ($cwd) {
    Assert-VstsPath -LiteralPath $cwd -PathType Container
    
    Write-Verbose "Setting working directory to '$cwd'."
    Set-Location $cwd
}

[string]$translateVersion = Get-VstsInput -Name translateVersion

git fetch origin $myBranch
git checkout -f $myBranch
git reset --hard origin/$myBranch

dotnet new tool-manifest --force

if ($translateVersion) {
    dotnet tool install --local dotnet-i18n-translate --version $translateVersion
} else {
    dotnet tool install --local dotnet-i18n-translate
}

[string]$defaultLanguage = Get-VstsInput -Name defaultLanguage
[bool]$validateOnly = Get-VstsInput -Name validate -AsBool

if ($defaultLanguage) {
    if ($validateOnly -eq $true) {
        dotnet i18n-translate -l $defaultLanguage --validate
    } else {
        [string]$authkey = Get-VstsInput -Name authkey -Require
        dotnet i18n-translate -l $defaultLanguage -a $authkey
    }
} else {
    if ($validateOnly -eq $true) {
        dotnet i18n-translate --validate
    } else {
        [string]$authkey = Get-VstsInput -Name authkey -Require
        dotnet i18n-translate -a $authkey
    }
}

if ($? -ne $true) {
    Write-Host "##vso[task.complete result=Failed;]Errors found while running translations"
    exit 1 # fail current build. Running i18n-translate failed
}

if ($validateOnly -ne $true) {
    git add '*.json'
    git reset './.config'

    if(git status --porcelain |Where-Object {$_ -notmatch '^\?\?'}) {
        git config --global user.name "Steven Thuriot"
        git config --global user.email i18n@thuriot.be

        git commit -m 'i18n-translate'
        git push
		
		Write-Host "##vso[task.complete result=Failed;]Pushed new translations"
        exit 1 # fail current build. Push will trigger a new build
    }
}