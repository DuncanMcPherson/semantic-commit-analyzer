[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/LpXcVzoLa44A5kgTjLmBgT/Hca7ehP7xru4DLAnZRTvy1/tree/master.svg?style=shield)](https://dl.circleci.com/status-badge/redirect/circleci/LpXcVzoLa44A5kgTjLmBgT/Hca7ehP7xru4DLAnZRTvy1/tree/master)
![NuGet Version](https://img.shields.io/nuget/v/SemanticRelease.CommitAnalysis)
![NuGet Downloads](https://img.shields.io/nuget/dt/SemanticRelease.CommitAnalysis)

# SemanticRelease.CommitAnalysis

Inspired by the Node.js [semantic-release](https://npmjs.com/package/semantic-release) tool, this was designed to make
versioning and releasing dotnet packages easy.

## Overview

This plugin serves as the tool for analyzing your commits from your most recent tag and determining which type of release 
is the most appropriate type. It will default to the Conventional Commits Standard.

## Features

- Commit analysis
- Release type detection

## Requirements

- .NET Standard 2.1
- C# 8.0 or later

## Installation

Installation should be handled by referencing the package name in your `semantic-release.json` as follows:

```json
{
  "tagFormat": "v{version}",
  "pluginConfigs": [
    "SemanticRelease.CommitAnalysis"
  ]
}
```

The base [semantic-release](https://www.nuget.org/packages/dotnet-semantic-release/) tool will handle package resolution

**NOTE:** the token "{version}" must be included in the tag format. The format can consist of any string so long as the expected token is present

## License

This project is licensed under the MIT License—see the [LICENSE](LICENSE) file for details.