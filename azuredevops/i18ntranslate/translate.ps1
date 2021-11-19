$myBranch = $env:SYSTEM_PULLREQUEST_SOURCEBRANCH;

if (!$myBranch) {
    Write-Error "Task could not determine value for the SYSTEM_PULLREQUEST_SOURCEBRANCH environment variable"
    exit 1 #fail, not a PR
}

$myBranch = $myBranch.substring(11);

[string]$authkey = Get-VstsInput -Name authkey -Require

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
    dotnet  tool install --local dotnet-i18n-translate --version $translateVersion
} else {
    dotnet  tool install --local dotnet-i18n-translate
}

[string]$defaultLanguage = Get-VstsInput -Name defaultLanguage

if ($defaultLanguage) {
    dotnet i18n-translate -a $authkey -l $defaultLanguage
} else {
    dotnet i18n-translate -a $authkey
}

git add '*.json'
git reset './.config'

if(git status --porcelain |Where-Object {$_ -notmatch '^\?\?'}) {
    git config --global user.name "Steven Thuriot"
    git config --global user.email i18n@thuriot.be

    git commit -m 'i18n-translate'
    git push
    exit 1 # fail current build. Push will trigger a new build
}