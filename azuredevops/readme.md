# i18n-translate

A build step to simplify i18n translations, using the DeepL API.

Should be run conditionally inside PR builds. When any changes have been made, these will be committed and pushed to the PR branch. At this point, the current build will be terminated and a new build will trigger due to the push.

This build step wraps around the dotnet [i18n-translate tool](https://www.nuget.org/packages/dotnet-i18n-translate), which supports .NET Core 3.1, .NET 5.0 and .NET 6.0. Don't forget to use the `Ensure dotnet x.x` task if needed.