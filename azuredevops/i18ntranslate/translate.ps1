$myBranch = $env:SYSTEM_PULLREQUEST_SOURCEBRANCH;

if (!$myBranch) {
    Write-Error "Task could not determine value for the SYSTEM_PULLREQUEST_SOURCEBRANCH environment variable"
    exit 1 #fail, not a PR
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
[string]$installCommand = 'dotnet tool install --local dotnet-i18n-translate'

if ($translateVersion) {
    $installCommand = "$installCommand --version $translateVersion"
}

iex $installCommand

[string]$defaultLanguage = Get-VstsInput -Name defaultLanguage
[bool]$validateOnly = Get-VstsInput -Name validate

[string]$translateCommand = 'dotnet i18n-translate'

if ($defaultLanguage) {
    $translateCommand = "$translateCommand -l $defaultLanguage"
}

if ($validateOnly -eq $true) {
    $translateCommand = "$translateCommand --validate"
} else {
    [string]$authkey = Get-VstsInput -Name authkey -Require
    $translateCommand = "$translateCommand -a $authkey"
}

[bool]$success = iex "$translateCommand;$?"

if ($success -ne $true) {
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
        exit 1 # fail current build. Push will trigger a new build
    }
}