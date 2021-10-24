# Set the working directory.
[string]$cwd = Get-VstsInput -Name cwd

if ($cwd) {
    Assert-VstsPath -LiteralPath $cwd -PathType Container
    
    Write-Verbose "Setting working directory to '$cwd'."
    Set-Location $cwd
}

[string]$translateVersion = Get-VstsInput -Name translateVersion
[string]$authkey = Get-VstsInput -Name authkey -Require
[bool]$free = Get-VstsInput -Name free -Require

$myBranch = ($env:SYSTEM_PULLREQUEST_SOURCEBRANCH).substring(11);

git fetch origin $myBranch
git checkout -f $myBranch
git reset --hard origin/$myBranch

dotnet new tool-manifest --force

if ($translateVersion) {
    dotnet  tool install --local dotnet-i18n-translate --version $translateVersion
} else {
    dotnet  tool install --local dotnet-i18n-translate
}

if ($free -eq $true) {
    dotnet i18n-translate --free -a $authkey
} else {
    dotnet i18n-translate -a $authkey
}

git add '*.json'
git reset './.config'

if(git status --porcelain |Where {$_ -notmatch '^\?\?'}) {
    git config --global user.name "Steven Thuriot"
    git config --global user.email i18n@thuriot.be

    git commit -m 'i18n-translate'
    git push
    exit 1 # fail current build. Push will trigger a new build
}