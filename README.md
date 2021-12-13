# dotnet-i18n-translate
Support for ngx-translate auto translations using DeepL

## Installation

> dotnet tool install --global dotnet-i18n-translate

## Usage

First, navigate to the folder containing the resource files that need to be translated. Then run: 

> i18n-translate -a <DEEPL_AUTHKEY>

If you don't want to pass the `authkey` every time you run this tool, or for instance when running on build agents, you can set an environment variable with name `DeepLAuthKey` instead. Then you can just run

> i18n-translate

For more options, check out:

> i18n-translate --help

## Azure

This tool is also available as an [Azure DevOps Build Step](https://marketplace.visualstudio.com/items?itemName=StevenThuriot.i18ntranslate).
