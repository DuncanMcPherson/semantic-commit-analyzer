using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;
using SemanticRelease.Abstractions;

namespace SemanticRelease.CommitAnalyzer
{
    public class CommitAnalyzer : ISemanticPlugin
    {
        private readonly List<string> _patchInitiators = new List<string>
        {
            "fix",
            "perf",
            "refactor",
            "revert"
        };
        
        public string Name => "CommitAnalyzer";
        
        public void Register(SemanticLifecycle lifecycle)
        {
            lifecycle.OnAnalyzeCommits(async (context) =>
            {
                Console.WriteLine($"Beginning step 'AnalyzeCommits' for plugin '{Name}'...");
                var repo = new Repository(context.WorkingDirectory);
                var lastTag = GetLastTag(context, repo);
                if (lastTag != null)
                {
                    Console.WriteLine($"Last tag is {lastTag.FriendlyName} at {lastTag.Target.Sha}");
                }
                var commits = await GetCommitsSinceLastTagAsync(context, repo, lastTag);
                var releaseType = DetermineReleaseType(commits);
                var nextVersion = CalculateVersionFromTagAndReleaseType(lastTag, releaseType, context.Config.TagFormat);
                context.PluginData["releaseType"] = releaseType.ToString();
                context.PluginData["commits"] = commits;
                context.PluginData["nextVersion"] = nextVersion;
                Console.WriteLine($"Step 'AnalyzeCommits' for plugin '{Name}' completed successfully.");
            });
        }

        private static string CalculateVersionFromTagAndReleaseType(Tag? lastTag, ReleaseType releaseType, string tagFormat)
        {
            if (lastTag == null)
            {
                Console.WriteLine("No tags found matching the specified format. Next version is '1.0.0'...");
                return "1.0.0";
            }
            
            var (prefix, suffix) = ExtractTagPattern(tagFormat);
            var version = lastTag.FriendlyName[(prefix.Length)..^suffix.Length];
            var versionParts = version.Split('.');
            var major = int.Parse(versionParts[0]);
            var minor = int.Parse(versionParts[1]);
            var patch = int.Parse(versionParts[2]);
            switch (releaseType)
            {
                case ReleaseType.Major:
                    major++;
                    break;
                case ReleaseType.Minor:
                    minor++;
                    break;
                case ReleaseType.Patch:
                    patch++;
                    break;
                case ReleaseType.None:
                default:
                    // Do nothing
                    break;
            }
            return $"{major}.{minor}.{patch}";
        }

        private static Tag? GetLastTag(ReleaseContext context, Repository repo)
        {
            var (prefix, suffix) = ExtractTagPattern(context.Config.TagFormat);
            var matchingTags = repo.Tags
                .Where(tag => tag.FriendlyName.StartsWith(prefix) && tag.FriendlyName.EndsWith(suffix))
                .Select(tag => new
                {
                    Tag = tag,
                    Commit = repo.Lookup<Commit>(tag.Target.Id)
                })
                .Where(x => x.Commit != null)
                .OrderByDescending(x => x.Commit.Committer.When)
                .FirstOrDefault();
            
            return matchingTags?.Tag;
        }

        private static (string prefix, string suffix) ExtractTagPattern(string tagFormat)
        {
            const string token = "{version}";
            var index = tagFormat.IndexOf(token, StringComparison.Ordinal);

            if (index == -1)
            {
                throw new ArgumentException("Tag format must contain '{version}'", nameof(tagFormat));
            }
            
            var prefix = tagFormat[..index];
            var suffix = tagFormat[(index + token.Length)..];
            return (prefix, suffix);
        }

        private async Task<List<Commit>> GetCommitsSinceLastTagAsync(ReleaseContext context, Repository repo, Tag? latestTag)
        {
            return await Task.Run(() =>
            {
                Commit? taggedCommit = null;

                if (latestTag != null)
                {
                    taggedCommit = repo.Lookup<Commit>(latestTag.Target.Sha);
                }

                var filter = new CommitFilter
                {
                    IncludeReachableFrom = repo.Head,
                    SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
                };

                return repo.Commits.QueryBy(filter).TakeWhile(commit => taggedCommit == null || commit.Sha != taggedCommit.Sha).ToList();
            });
        }

        private ReleaseType DetermineReleaseType(List<Commit> commits)
        {
            var result = ReleaseType.None;

            Console.WriteLine($"Found {commits.Count} commits since last tag");
            foreach (var commit in commits)
            {
                var localReleaseType = ReleaseType.None;
                Console.WriteLine($"Processing commit {commit.Sha}: {commit.Message}");
                if (commit.Message.StartsWith("feat:"))
                    localReleaseType = ReleaseType.Minor;
                else if (CommitIsPatchCommit(commit))
                    localReleaseType = ReleaseType.Patch;
                else if (CommitIsMajorCommit(commit))
                    localReleaseType = ReleaseType.Major;
                Console.WriteLine($"Commit release type is: {localReleaseType.ToString()}");
                result = MaxReleaseType(result, localReleaseType);
            }
            Console.WriteLine($"Final release type is: {result.ToString()}");
            return result;
        }
        
        private bool CommitIsPatchCommit(Commit commit)
        {
            return _patchInitiators.Any(patchInitiator => commit.Message.StartsWith($"{patchInitiator}:"));
        }

        private bool CommitIsMajorCommit(Commit commit)
        {
            return commit.Message.Contains("BREAKING CHANGE") || Regex.IsMatch(commit.Message, @"^(\w+)!:");
        }
        
        private ReleaseType MaxReleaseType(ReleaseType a, ReleaseType b)
        {
            return (ReleaseType)Math.Max((int)a, (int)b);
        }
    }
}